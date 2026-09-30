using Godot;
using System.Collections.Generic;

// Selects weighted attacks and runs each ship's volleys through its hardpoints.
public partial class EnemyAttackController : Node
{
	#region Runtime

	private EnemyShip _ship;
	private EnemyAttackControllerDefinition _definition;

	private readonly List<AttackState> _attacks = new();
	private readonly List<AttackState> _eligible = new();
	private readonly RandomNumberGenerator _random = new();

	private float _selectionTimer;

	// Stores mutable state here so shared attack resources remain unchanged.
	private sealed class AttackState
	{
		public EnemyAttackDefinition Definition;
		public readonly List<EnemyHardpoint> Mounts = new();

		public Node3D Target;

		public float Cooldown;
		public float ShotTimer;
		public float WaitTimer;

		public int Round;
		public int MountIndex;

		public bool Active;
	}

	#endregion

	#region Setup

	// Caches each attack's hardpoints and staggers this ship's selection checks.
	public void Initialize(
		EnemyShip ship,
		EnemyAttackControllerDefinition definition
	)
	{
		_ship = ship;
		_definition = definition;

		_random.Randomize();

		// Fire after movement and hardpoint aiming have updated.
		ProcessPhysicsPriority = 20;

		_selectionTimer =
			(ship.GetInstanceId() % 10) / 10.0f
			* Mathf.Max(0.02f, definition.SelectionIntervalSeconds);

		foreach (EnemyAttackDefinition attack in definition.Attacks)
		{
			if (attack == null)
			{
				continue;
			}

			if (attack.Weapon?.ProjectileScene == null)
			{
				GD.PushError(
					$"{ship.Name}: attack '{attack.Key}' needs a weapon "
					+ "with a ProjectileScene."
				);

				continue;
			}

			AttackState state = new()
			{
				Definition = attack
			};

			foreach (EnemyHardpoint mount in ship.Hardpoints)
			{
				if (mount.Group == attack.HardpointGroup)
				{
					state.Mounts.Add(mount);
				}
			}

			if (state.Mounts.Count == 0)
			{
				GD.PushError(
					$"{ship.Name}: attack '{attack.Key}' could not find "
					+ $"hardpoint group '{attack.HardpointGroup}'."
				);

				continue;
			}

			_attacks.Add(state);
		}
	}

	#endregion

	#region Scheduling

	// Advances active volleys and periodically selects another eligible attack.
	public override void _PhysicsProcess(double delta)
	{
		if (
			!GodotObject.IsInstanceValid(_ship)
			|| !_ship.IsCombatTargetable
			|| _definition == null
		)
		{
			return;
		}

		float seconds = (float)delta;

		foreach (AttackState state in _attacks)
		{
			state.Cooldown = Mathf.Max(
				0.0f,
				state.Cooldown - seconds
			);

			if (state.Active)
			{
				UpdateVolley(state, seconds);
			}
		}

		_selectionTimer -= seconds;

		if (_selectionTimer > 0.0f)
		{
			return;
		}

		_selectionTimer = Mathf.Max(
			0.02f,
			_definition.SelectionIntervalSeconds
		);

		SelectAttack();
	}

	// Chooses an eligible attack by weight while respecting occupied channels.
	private void SelectAttack()
	{
		Node3D target = _ship.Targeting?.Target;

		if (!IsValidTarget(target))
		{
			return;
		}

		int activeCount = 0;

		foreach (AttackState state in _attacks)
		{
			if (state.Active)
			{
				activeCount++;
			}
		}

		if (activeCount >= Mathf.Max(1, _definition.MaxActiveChannels))
		{
			return;
		}

		_eligible.Clear();
		float totalWeight = 0.0f;

		foreach (AttackState state in _attacks)
		{
			EnemyAttackDefinition attack = state.Definition;

			if (
				state.Active
				|| state.Cooldown > 0.0f
				|| attack.Weight <= 0.0f
				|| IsChannelBusy(attack.Channel)
				|| !IsInRange(attack, target)
			)
			{
				continue;
			}

			bool canFire = false;

			foreach (EnemyHardpoint mount in state.Mounts)
			{
				if (CanFire(mount, attack, target))
				{
					canFire = true;
					break;
				}
			}

			if (!canFire)
			{
				continue;
			}

			_eligible.Add(state);
			totalWeight += attack.Weight;
		}

		if (_eligible.Count == 0)
		{
			return;
		}

		float roll = _random.Randf() * totalWeight;
		AttackState selected = _eligible[_eligible.Count - 1];

		foreach (AttackState state in _eligible)
		{
			roll -= state.Definition.Weight;

			if (roll <= 0.0f)
			{
				selected = state;
				break;
			}
		}

		selected.Active = true;
		selected.Target = target;
		selected.Round = 0;
		selected.WaitTimer = 0.0f;
		selected.ShotTimer = 0.0f;

		ChooseMount(selected);
	}

	// Prevents two attacks from occupying the same named channel.
	private bool IsChannelBusy(string channel)
	{
		foreach (AttackState state in _attacks)
		{
			if (state.Active && state.Definition.Channel == channel)
			{
				return true;
			}
		}

		return false;
	}

	#endregion

	#region Volleys

	// Fires a round when ready, briefly waiting for blocked or unaligned mounts.
	private void UpdateVolley(AttackState state, float seconds)
	{
		EnemyAttackDefinition attack = state.Definition;
		Node3D target = state.Target;

		if (
			!IsValidTarget(target)
			|| target != _ship.Targeting?.Target
			|| !IsInRange(attack, target)
		)
		{
			FinishAttack(state);
			return;
		}

		state.ShotTimer -= seconds;

		if (state.ShotTimer > 0.0f)
		{
			return;
		}

		bool fired = false;

		if (attack.Order == HardpointFireOrder.All)
		{
			foreach (EnemyHardpoint mount in state.Mounts)
			{
				if (CanFire(mount, attack, target))
				{
					Fire(mount, attack.Weapon);
					fired = true;
				}
			}
		}
		else
		{
			EnemyHardpoint mount = state.Mounts[state.MountIndex];

			if (CanFire(mount, attack, target))
			{
				Fire(mount, attack.Weapon);
				fired = true;
			}
		}

		if (!fired)
		{
			state.WaitTimer += seconds;

			if (
				state.WaitTimer
				< Mathf.Max(0.0f, attack.MaxRoundWaitSeconds)
			)
			{
				return;
			}
		}

		// A fired or skipped round advances the volley.
		state.WaitTimer = 0.0f;
		state.Round++;

		if (state.Round >= Mathf.Max(1, attack.VolleyRounds))
		{
			FinishAttack(state);
			return;
		}

		state.ShotTimer = Mathf.Max(
			0.01f,
			attack.IntervalSeconds
		);

		ChooseMount(state);
	}

	// Chooses the next mount using the attack's configured firing order.
	private void ChooseMount(AttackState state)
	{
		if (state.Definition.Order == HardpointFireOrder.Random)
		{
			state.MountIndex = _random.RandiRange(
				0,
				state.Mounts.Count - 1
			);

			return;
		}

		state.MountIndex = state.Round % state.Mounts.Count;
	}

	// Releases the channel and starts the attack's cooldown.
	private void FinishAttack(AttackState state)
	{
		state.Active = false;
		state.Target = null;

		state.Cooldown = Mathf.Max(
			0.0f,
			state.Definition.CooldownSeconds
		);
	}

	#endregion

	#region Firing Conditions

	// Rejects removed, dying, or otherwise untargetable combat objects.
	private bool IsValidTarget(Node3D target)
	{
		return
			GodotObject.IsInstanceValid(target)
			&& !target.IsQueuedForDeletion()
			&& target is ICombatTarget combatTarget
			&& combatTarget.IsCombatTargetable;
	}

	// Measures attack range from the ship without calculating a square root.
	private bool IsInRange(
		EnemyAttackDefinition attack,
		Node3D target
	)
	{
		float minimum = Mathf.Max(0.0f, attack.RangeMin);
		float maximum = Mathf.Max(minimum, attack.RangeMax);

		float distanceSquared = _ship.GlobalPosition.DistanceSquaredTo(
			target.GlobalPosition
		);

		return
			distanceSquared >= minimum * minimum
			&& distanceSquared <= maximum * maximum;
	}

	// Checks the actual muzzle alignment and optional line of sight.
	private bool CanFire(
		EnemyHardpoint mount,
		EnemyAttackDefinition attack,
		Node3D target
	)
	{
		if (
			!GodotObject.IsInstanceValid(mount)
			|| mount.IsQueuedForDeletion()
			|| !GodotObject.IsInstanceValid(mount.Muzzle)
		)
		{
			return false;
		}

		Vector3 offset = target.GlobalPosition - mount.MuzzlePosition;

		if (offset.LengthSquared() < 0.0001f)
		{
			return false;
		}

		float tolerance = Mathf.DegToRad(
			Mathf.Clamp(attack.FireToleranceDegrees, 0.0f, 180.0f)
		);

		float alignment = mount.MuzzleDirection.Dot(
			offset.Normalized()
		);

		if (alignment < Mathf.Cos(tolerance))
		{
			return false;
		}

		if (!attack.RequireLineOfSight)
		{
			return true;
		}

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(
				mount.MuzzlePosition,
				target.GlobalPosition
			);

		query.HitFromInside = true;

		query.Exclude = new Godot.Collections.Array<Rid>
		{
			_ship.GetRid()
		};

		var result = _ship.GetWorld3D().DirectSpaceState.IntersectRay(
			query
		);

		return
			result.Count == 0
			|| result["collider"].AsGodotObject() == target;
	}

	#endregion

	#region Projectile Creation

	// Spawns a projectile along the muzzle direction and emits its muzzle flash.
	private void Fire(
		EnemyHardpoint mount,
		WeaponDefinition weapon
	)
	{
		Projectile projectile = weapon.ProjectileScene.Instantiate<Projectile>();

		GetTree().CurrentScene.AddChild(projectile);

		Vector3 direction = mount.MuzzleDirection;

		Vector3 up = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.99f
			? Vector3.Right
			: Vector3.Up;

		projectile.LookAtFromPosition(
			mount.MuzzlePosition,
			mount.MuzzlePosition + direction,
			up
		);

		projectile.Configure(
			weapon,
			_ship,
			_ship.CombatFaction,
			_ship.Velocity
		);

		WeaponEffects.Muzzle(
			mount.Muzzle,
			weapon.Effects,
			_ship.CombatFaction
		);
	}

	#endregion
}

using System.Collections.Generic;
using Godot;

// Fires projectiles or maintains sustained beams from this mount's muzzle markers.
public partial class WeaponMount : Node3D
{
	#region Settings

	[Export] public WeaponDefinition Weapon;
	[Export] public Faction ShooterFaction = Faction.Player;

	#endregion

	#region Runtime

	private readonly List<Marker3D> _muzzles = new();
	private readonly List<SustainedBeam> _beams = new();

	private CharacterBody3D _ship;

	private int _muzzleIndex;
	private float _cooldown;

	#endregion

	#region Setup

	// =========================================================
	// Finds the owning ship and caches this mount's authored muzzle markers.
	public override void _Ready()
	{
		Node ancestor = GetParent();

		while (ancestor != null)
		{
			if (ancestor is CharacterBody3D ship)
			{
				_ship = ship;
				break;
			}

			ancestor = ancestor.GetParent();
		}

		foreach (Node child in GetChildren())
		{
			if (child is Marker3D muzzle)
			{
				_muzzles.Add(muzzle);
			}
		}

		if (_ship == null || _muzzles.Count == 0)
		{
			GD.PushError(
				$"{Name}: WeaponMount needs a ship ancestor and a muzzle marker."
			);
		}
	}

	// =========================================================
	// Changes the equipped weapon and clears the previous weapon's firing state.
	public void EquipWeapon(WeaponDefinition weapon)
	{
		StopFiring();

		Weapon = weapon;
		_cooldown = 0.0f;
		_muzzleIndex = 0;
	}

	#endregion

	#region Firing

	// =========================================================
	// Updates the projectile weapon's shot cooldown.
	public override void _PhysicsProcess(double delta)
	{
		_cooldown = Mathf.Max(
			0.0f,
			_cooldown - (float)delta
		);
	}

	// =========================================================
	// Fires repeatedly or maintains one beam while the fire button is held.
	public void SetTriggerHeld(bool held)
	{
		if (!held)
		{
			StopFiring();
			return;
		}

		if (Weapon == null)
		{
			StopFiring();
			return;
		}

		if (Weapon.Delivery == WeaponDefinition.DeliveryType.Beam)
		{
			StartBeams();
		}
		else
		{
			StopFiring();
			TryFire();
		}
	}

	// =========================================================
	// Attempts one projectile shot when the weapon's cooldown permits.
	public bool TryFire()
	{
		if (Weapon == null
			|| Weapon.Delivery != WeaponDefinition.DeliveryType.Projectile
			|| Weapon.ProjectileScene == null
			|| !GodotObject.IsInstanceValid(_ship)
			|| _muzzles.Count == 0
			|| _cooldown > 0.0f)
		{
			return false;
		}

		FireShot();

		_cooldown = Mathf.Max(
			0.01f,
			Weapon.SecondsBetweenShots
		);

		return true;
	}

	// =========================================================
	// Launches a projectile from the next muzzle marker.
	private void FireShot()
	{
		Marker3D muzzle = _muzzles[_muzzleIndex];

		_muzzleIndex = (_muzzleIndex + 1) % _muzzles.Count;

		Projectile projectile =
			Weapon.ProjectileScene.Instantiate<Projectile>();

		GetTree().CurrentScene.AddChild(projectile);

		projectile.GlobalTransform = new Transform3D(
			muzzle.GlobalBasis.Orthonormalized(),
			muzzle.GlobalPosition
		);

		projectile.Configure(
			Weapon,
			_ship,
			ShooterFaction,
			_ship.Velocity
		);

		WeaponEffects.Muzzle(
			muzzle,
			Weapon.MuzzleEffects,
			ShooterFaction
		);
	}

	// =========================================================
	// Creates beams once per trigger hold rather than once per frame.
	private void StartBeams()
	{
		if (_beams.Count > 0
			|| Weapon.Beam == null
			|| !GodotObject.IsInstanceValid(_ship)
			|| _muzzles.Count == 0)
		{
			return;
		}

		foreach (Marker3D muzzle in _muzzles)
		{
			SustainedBeam beam = new SustainedBeam();

			AddChild(beam);

			beam.Configure(
				Weapon,
				muzzle,
				_ship,
				ShooterFaction
			);

			_beams.Add(beam);
		}
	}

	// =========================================================
	// Releases all sustained beams owned by this mount.
	public void StopFiring()
	{
		foreach (SustainedBeam beam in _beams)
		{
			if (GodotObject.IsInstanceValid(beam)
				&& !beam.IsQueuedForDeletion())
			{
				beam.Stop();
			}
		}

		_beams.Clear();
	}

	#endregion
}

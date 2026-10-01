using System.Collections.Generic;
using Godot;

// Fires projectiles or maintains sustained beams from this mount's muzzle markers.
public partial class WeaponMount : Node3D
{
	#region Settings

	[Export] public WeaponDefinition Weapon;
	[Export] public Faction ShooterFaction = Faction.Player;
	[Export] public bool AimAtCameraCenter = false;
	[Export] public float CameraAimDistance = 1000.0f;
	[Export] public WeaponMount ProjectileMuzzleProvider;

	#endregion

	#region Runtime

	private readonly List<Marker3D> _muzzles = new();
	private readonly List<SustainedBeam> _beams = new();

	private CharacterBody3D _ship;

	private int _muzzleIndex;
	private float _cooldown;

		private readonly RandomNumberGenerator _random = new();

	public float CurrentSpreadDegrees { get; private set; }
		private PhysicsRayQueryParameters3D _aimQuery;

	#endregion

		#region Camera Aiming

	// =========================================================
	// Finds the crosshair's world aim point within reach of the firing muzzle.
	public Vector3 GetCameraAimPoint(
		Vector3 muzzlePosition,
		float maximumDistance
	)
	{
		float distance = Mathf.Max(0.01f, maximumDistance);

		Camera3D camera = GetViewport().GetCamera3D();

		if (!GodotObject.IsInstanceValid(camera))
		{
			return muzzlePosition
				- GlobalBasis.Z.Normalized() * distance;
		}

		Vector2 screenCenter =
			GetViewport().GetVisibleRect().Size * 0.5f;

		Vector3 origin = camera.ProjectRayOrigin(screenCenter);
		Vector3 direction = camera.ProjectRayNormal(screenCenter);

		// Intersect the camera ray with the muzzle's maximum-range sphere.
		// This puts the empty-space aiming point at the weapon's actual reach.
		Vector3 offset = origin - muzzlePosition;

		float projection = offset.Dot(direction);

		float discriminant =
			projection * projection
			- (offset.LengthSquared() - distance * distance);

		if (discriminant < 0.0f)
		{
			return muzzlePosition + direction * distance;
		}

		float rayDistance =
			-projection + Mathf.Sqrt(discriminant);

		if (rayDistance <= 0.0f)
		{
			return muzzlePosition + direction * distance;
		}

		Vector3 aimPoint = origin + direction * rayDistance;

		if (_aimQuery == null)
		{
			_aimQuery = new PhysicsRayQueryParameters3D
			{
				CollisionMask = 1u,
				CollideWithBodies = true,
				CollideWithAreas = false,
				HitFromInside = true
			};

			ShipShield.ConfigureWeaponQuery(_aimQuery, _ship);
		}

		_aimQuery.From = origin;
		_aimQuery.To = aimPoint;

		var result = GetWorld3D().DirectSpaceState.IntersectRay(
			_aimQuery
		);

		if (result.Count > 0)
		{
			aimPoint = result["position"].AsVector3();
		}

		return aimPoint;
	}

	#endregion

	#region Setup

		// =========================================================
	// Finds the owning ship and caches the authored muzzle markers.
	public override void _Ready()
	{
		_random.Randomize();

		// Update accuracy after movement and before player firing input.
		ProcessPhysicsPriority = 10;

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

		CurrentSpreadDegrees = Mathf.Max(
			0.0f,
			Weapon?.Accuracy?.BaseSpreadDegrees ?? 0.0f
		);

		if (_ship == null || _muzzles.Count == 0)
		{
			GD.PushError(
				$"{Name}: WeaponMount needs a ship ancestor and a muzzle marker."
			);
		}
	}

		// =========================================================
	// Changes the equipped weapon and resets its firing and accuracy state.
	public void EquipWeapon(WeaponDefinition weapon)
	{
		StopFiring();

		Weapon = weapon;
		_cooldown = 0.0f;
		_muzzleIndex = 0;

		CurrentSpreadDegrees = Mathf.Max(
			0.0f,
			weapon?.Accuracy?.BaseSpreadDegrees ?? 0.0f
		);
	}

	#endregion

	#region Firing

		// =========================================================
	// Updates shot cooldown and accuracy from actual ship movement.
	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		_cooldown = Mathf.Max(
			0.0f,
			_cooldown - seconds
		);

		WeaponAccuracySettings accuracy = Weapon?.Accuracy;

		if (accuracy == null
			|| Weapon.Delivery != WeaponDefinition.DeliveryType.Projectile
			|| !GodotObject.IsInstanceValid(_ship))
		{
			CurrentSpreadDegrees = 0.0f;
			return;
		}

		float movementRatio = Mathf.Clamp(
			_ship.GetRealVelocity().Length()
				/ Mathf.Max(0.01f, accuracy.FullSpreadSpeed),
			0.0f,
			1.0f
		);

		float desiredSpread =
			Mathf.Max(0.0f, accuracy.BaseSpreadDegrees)
			+ Mathf.Max(0.0f, accuracy.MovementSpreadDegrees)
				* movementRatio;

		float blend = 1.0f - Mathf.Exp(
			-Mathf.Max(0.0f, accuracy.Response) * seconds
		);

		CurrentSpreadDegrees = Mathf.Lerp(
			CurrentSpreadDegrees,
			desiredSpread,
			blend
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
	// Alternates projectile launches through local or borrowed muzzle markers.
	private void FireShot()
	{
		List<Marker3D> muzzles = _muzzles;

		if (GodotObject.IsInstanceValid(ProjectileMuzzleProvider)
			&& ProjectileMuzzleProvider._muzzles.Count > 0)
		{
			muzzles = ProjectileMuzzleProvider._muzzles;
		}

		if (muzzles.Count == 0)
		{
			return;
		}

		_muzzleIndex %= muzzles.Count;

		Marker3D muzzle = muzzles[_muzzleIndex];

		_muzzleIndex = (_muzzleIndex + 1) % muzzles.Count;

		Vector3 direction = -muzzle.GlobalBasis.Z.Normalized();

		if (AimAtCameraCenter)
		{
			Vector3 aimPoint = GetCameraAimPoint(
				muzzle.GlobalPosition,
				CameraAimDistance
			);

			Vector3 offset = aimPoint - muzzle.GlobalPosition;

			if (offset.LengthSquared() > 0.0001f)
			{
				direction = offset.Normalized();
			}
		}

		direction = ApplySpread(direction);

		Vector3 up =
			Mathf.Abs(direction.Dot(Vector3.Up)) > 0.99f
				? Vector3.Right
				: Vector3.Up;

		Projectile projectile =
			Weapon.ProjectileScene.Instantiate<Projectile>();

		GetTree().CurrentScene.AddChild(projectile);

		projectile.LookAtFromPosition(
			muzzle.GlobalPosition,
			muzzle.GlobalPosition + direction,
			up
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
	// Samples a random direction uniformly inside the weapon's spread cone.
	private Vector3 ApplySpread(Vector3 forward)
	{
		float angle = Mathf.DegToRad(
			Mathf.Clamp(CurrentSpreadDegrees, 0.0f, 89.0f)
		);

		if (angle <= 0.0001f)
		{
			return forward;
		}

		Vector3 reference =
			Mathf.Abs(forward.Dot(Vector3.Up)) > 0.99f
				? Vector3.Right
				: Vector3.Up;

		Vector3 right = forward.Cross(reference).Normalized();
		Vector3 up = right.Cross(forward).Normalized();

		float cosine = Mathf.Lerp(
			1.0f,
			Mathf.Cos(angle),
			_random.Randf()
		);

		float sine = Mathf.Sqrt(
			Mathf.Max(0.0f, 1.0f - cosine * cosine)
		);

		float rotation = _random.Randf() * Mathf.Tau;

		return (
			forward * cosine
			+ right * Mathf.Cos(rotation) * sine
			+ up * Mathf.Sin(rotation) * sine
		).Normalized();
	}

		// =========================================================
	// Creates sustained beams and connects their optional camera aiming provider.
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
			SustainedBeam beam = new();

			AddChild(beam);

			beam.SetAimMount(this);

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

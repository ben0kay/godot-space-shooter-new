using Godot;

// Resolves launch settings, moves one projectile, and applies its damage and effects.
public partial class Projectile : Area3D
{
	#region Runtime

	private ProjectileDefinition _definition;
	private CollisionObject3D _source;
	private Faction _sourceFaction;

	private Vector3 _velocity;
	private float _lifeRemaining;
	private float _damage;
	private float _sizeScale;
	private float _collisionRadius;

	private PhysicsRayQueryParameters3D _flightQuery;
	private GpuParticles3D _trail;
	private ProjectileGuidance _guidance;

	private bool _hit;

	#endregion

	#region Setup

		// =========================================================
	// Resolves launch settings, guidance, collision exclusions, and effects.
	public void Configure(
		WeaponDefinition weapon,
		CollisionObject3D source,
		Faction sourceFaction,
		Vector3 sourceVelocity
	)
	{
		_definition = weapon.Projectile;
		_source = source;
		_sourceFaction = sourceFaction;

		if (_definition == null)
		{
			GD.PushError(
				"Projectile needs a weapon with a ProjectileDefinition."
			);

			QueueFree();
			return;
		}

		ProjectileLaunchOverrides launch = weapon.LaunchOverrides;

		_damage = Mathf.Max(
			0.0f,
			launch != null && launch.OverrideDamage
				? launch.Damage
				: _definition.Damage
		);

		float speed = Mathf.Max(
			0.0f,
			launch != null && launch.OverrideSpeed
				? launch.Speed
				: _definition.Speed
		);

		_lifeRemaining = Mathf.Max(
			0.01f,
			launch != null && launch.OverrideLifetime
				? launch.Lifetime
				: _definition.Lifetime
		);

		_sizeScale = Mathf.Max(0.01f, launch?.SizeScale ?? 1.0f);

		_collisionRadius = Mathf.Max(
			0.005f,
			_definition.CollisionRadius * _sizeScale
		);

		_velocity =
			-GlobalBasis.Z.Normalized() * speed + sourceVelocity;

		ProjectileGuidanceSettings guidanceSettings =
			launch != null && launch.OverrideGuidance
				? launch.Guidance
				: _definition.Guidance;

		_guidance = guidanceSettings != null
			? new ProjectileGuidance(
				this,
				source,
				sourceFaction,
				guidanceSettings
			)
			: null;

		_flightQuery = PhysicsRayQueryParameters3D.Create(
			GlobalPosition,
			GlobalPosition
		);

		_flightQuery.HitFromInside = true;

		_flightQuery.Exclude = new Godot.Collections.Array<Rid>
		{
			GetRid()
		};

		ShipShield.ConfigureWeaponQuery(_flightQuery, source);

		CollisionMask |= ShipShield.ShieldLayer;

		CreateVisual();
		CreateCollision();

		_trail = WeaponEffects.CreateTrail(
			this,
			_definition.Effects,
			_sourceFaction
		);
	}

		// =========================================================
	// Gives a guided projectile the target selected by its firing controller.
	public void SetGuidanceTarget(Node3D target)
	{
		_guidance?.SetTarget(target);
	}

	// Connects overlap detection for bodies touching the projectile.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	#endregion

	#region Flight And Impact

		// =========================================================
	// Steers the projectile, checks its travelled path, and expires old shots.
	public override void _PhysicsProcess(double delta)
	{
		if (_hit || _definition == null)
		{
			return;
		}

		float seconds = (float)delta;

		if (_guidance != null)
		{
			_velocity = _guidance.Update(_velocity, seconds);

			if (_velocity.LengthSquared() > 0.0001f)
			{
				Vector3 direction = _velocity.Normalized();

				Vector3 up =
					Mathf.Abs(direction.Dot(Vector3.Up)) > 0.99f
						? Vector3.Right
						: Vector3.Up;

				LookAt(GlobalPosition + direction, up);
			}
		}

		Vector3 destination =
			GlobalPosition + _velocity * seconds;

		_flightQuery.From = GlobalPosition;
		_flightQuery.To = destination;

		var result = GetWorld3D().DirectSpaceState.IntersectRay(
			_flightQuery
		);

		if (result.Count > 0)
		{
			GlobalPosition = result["position"].AsVector3();

			ResolveHit(
				result["collider"].AsGodotObject() as Node3D,
				result["normal"].AsVector3()
			);

			return;
		}

		GlobalPosition = destination;
		_lifeRemaining -= seconds;

		if (_lifeRemaining <= 0.0f)
		{
			Expire();
		}
	}

	// Uses the reverse flight direction when an overlap has no surface normal.
	private void OnBodyEntered(Node3D body)
	{
		Vector3 normal = _velocity.LengthSquared() > 0.001f
			? -_velocity.Normalized()
			: Vector3.Up;

		ResolveHit(body, normal);
	}

	// =========================================================
// Applies a projectile impact while ignoring the firing ship's shield.
private void ResolveHit(Node3D body, Vector3 normal)
{
	if (_hit
		|| _definition == null
		|| body == null
		|| body == _source)
	{
		return;
	}

	if (body is ShipShield ownShield && ownShield.Ship == _source)
	{
		return;
	}

	_hit = true;

	CollisionObject3D source = GodotObject.IsInstanceValid(_source)
		? _source
		: null;

	if (body is IDamageable target)
	{
		target.ApplyDamage(new DamageInfo(
			_damage,
			source,
			_sourceFaction,
			GlobalPosition,
			normal
		));
	}

	WeaponEffects.Impact(
		this,
		GlobalPosition,
		normal,
		_definition.Effects,
		_sourceFaction
	);

	WeaponEffects.FinishTrail(_trail);
	QueueFree();
}

	// Removes an expired shot while its remaining trail particles finish.
	private void Expire()
	{
		if (_hit)
		{
			return;
		}

		_hit = true;

		WeaponEffects.FinishTrail(_trail);
		QueueFree();
	}

	#endregion

	#region Appearance And Collision

	// Creates the projectile's sphere or tracer using its resolved size scale.
	private void CreateVisual()
	{
		Color color = _definition.Effects != null
			? WeaponEffects.GetColor(_definition.Effects, _sourceFaction)
			: _definition.Color;

		StandardMaterial3D material = new()
		{
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = _definition.EmissionEnergy,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};

		Mesh mesh;
		Vector3 position = Vector3.Zero;

		if (_definition.VisualLength > 0.0f)
		{
			float length = Mathf.Max(
				0.01f,
				_definition.VisualLength * _sizeScale
			);

			float width = Mathf.Max(
				0.005f,
				_definition.VisualWidth * _sizeScale
			);

			mesh = new BoxMesh
			{
				Size = new Vector3(width, width, length),
				Material = material
			};

			// The collision point is the front of the tracer.
			position = Vector3.Back * length * 0.5f;
		}
		else
		{
			mesh = new SphereMesh
			{
				Radius = _collisionRadius,
				Height = _collisionRadius * 2.0f,
				RadialSegments = 8,
				Rings = 4,
				Material = material
			};
		}

		AddChild(new MeshInstance3D
		{
			Name = "Visual",
			Mesh = mesh,
			Position = position,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		});
	}

	// Creates the projectile's scaled collision sphere.
	private void CreateCollision()
	{
		AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = new SphereShape3D
			{
				Radius = _collisionRadius
			}
		});
	}

	#endregion
}

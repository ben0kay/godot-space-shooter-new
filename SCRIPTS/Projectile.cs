using Godot;

// Moves a weapon projectile, applies impact damage, and manages its visual effects.
public partial class Projectile : Area3D
{
	#region Runtime

	private Vector3 _velocity;
	private float _lifeRemaining;

	private CollisionObject3D _source;
	private Faction _sourceFaction;
	private WeaponDefinition _definition;

	private PhysicsRayQueryParameters3D _flightQuery;
	private GpuParticles3D _trail;

	private bool _hit;

	#endregion

	#region Setup

	// Receives the weapon settings, firing ship, faction, and inherited velocity.
	public void Configure(
		WeaponDefinition definition,
		CollisionObject3D source,
		Faction sourceFaction,
		Vector3 sourceVelocity
	)
	{
		_definition = definition;
		_source = source;
		_sourceFaction = sourceFaction;

		_lifeRemaining = Mathf.Max(
			0.01f,
			definition.ProjectileLifetime
		);

		_velocity =
			-GlobalBasis.Z.Normalized() * definition.ProjectileSpeed
			+ sourceVelocity;

		_flightQuery = PhysicsRayQueryParameters3D.Create(
			GlobalPosition,
			GlobalPosition
		);

		_flightQuery.HitFromInside = true;

		Godot.Collections.Array<Rid> exclusions = new()
		{
			GetRid()
		};

		if (GodotObject.IsInstanceValid(source))
		{
			exclusions.Add(source.GetRid());
		}

		_flightQuery.Exclude = exclusions;

		CreateVisual();
		CreateCollision();

		_trail = WeaponEffects.CreateTrail(
			this,
			definition.Effects,
			sourceFaction
		);
	}

	// Connects overlap detection for bodies touching the projectile.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	#endregion

	#region Flight And Impact

	// Checks the travelled path, advances the projectile, and expires old shots.
	public override void _PhysicsProcess(double delta)
	{
		if (_hit || _definition == null)
		{
			return;
		}

		float seconds = (float)delta;
		Vector3 destination = GlobalPosition + _velocity * seconds;

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

	// Applies damage, emits impact particles, and allows the trail to finish.
	private void ResolveHit(Node3D body, Vector3 normal)
	{
		if (
			_hit
			|| _definition == null
			|| body == null
			|| body == _source
		)
		{
			return;
		}

		_hit = true;

		CollisionObject3D source = GodotObject.IsInstanceValid(_source)
			? _source
			: null;

		if (body is IDamageable target)
		{
			DamageInfo damage = new DamageInfo(
				_definition.Damage,
				source,
				_sourceFaction
			);

			target.ApplyDamage(damage);
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

	// Removes an expired projectile while its remaining trail particles finish.
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

	#region Appearance

	// Creates the projectile using its effect colour or existing weapon colour.
	private void CreateVisual()
	{
		float radius = Mathf.Max(
			0.005f,
			_definition.ProjectileRadius
		);

		Color color = _definition.Effects != null
			? WeaponEffects.GetColor(
				_definition.Effects,
				_sourceFaction
			)
			: _definition.ProjectileColor;

		StandardMaterial3D material = new()
		{
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier =
				_definition.Effects?.EmissionEnergy ?? 1.0f,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};

		SphereMesh mesh = new()
		{
			Radius = radius,
			Height = radius * 2.0f,
			RadialSegments = 8,
			Rings = 4,
			Material = material
		};

		MeshInstance3D visual = new()
		{
			Name = "Visual",
			Mesh = mesh
		};

		AddChild(visual);
	}

	// Creates a collision sphere matching the visible projectile radius.
	private void CreateCollision()
	{
		SphereShape3D shape = new()
		{
			Radius = Mathf.Max(
				0.005f,
				_definition.ProjectileRadius
			)
		};

		CollisionShape3D collision = new()
		{
			Name = "Collision",
			Shape = shape
		};

		AddChild(collision);
	}

	#endregion
}

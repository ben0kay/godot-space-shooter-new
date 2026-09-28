using Godot;

public partial class Projectile : Area3D
{
	#region Runtime

	private Vector3 _velocity;
	private float _lifeRemaining;
	private CollisionObject3D _source;
	private Faction _sourceFaction;
	private WeaponDefinition _definition;
	private bool _hit;

	#endregion

	#region Setup

	// Receives the weapon, source, faction, and inherited ship velocity.
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
		_lifeRemaining = definition.ProjectileLifetime;

		_velocity =
			-GlobalBasis.Z * definition.ProjectileSpeed
			+ sourceVelocity;

		CreateVisual();
		CreateCollision();
	}

	// Connects overlap detection for bodies touching the projectile.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	#endregion

	#region Flight And Impact

	// Checks the flight path, moves the projectile, and expires old shots.
	public override void _PhysicsProcess(double delta)
	{
		if (_hit)
		{
			return;
		}

		float seconds = (float)delta;
		Vector3 destination = GlobalPosition + _velocity * seconds;

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(GlobalPosition, destination);

		query.Exclude = new Godot.Collections.Array<Rid>
		{
			_source.GetRid()
		};

		var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			GlobalPosition = result["position"].AsVector3();
			ResolveHit(result["collider"].AsGodotObject() as Node3D);
			return;
		}

		GlobalPosition = destination;
		_lifeRemaining -= seconds;

		if (_lifeRemaining <= 0.0f)
		{
			QueueFree();
		}
	}

	// Handles an overlap found by the Area3D collision shape.
	private void OnBodyEntered(Node3D body)
	{
		ResolveHit(body);
	}

	// Sends damage to damageable targets and removes the projectile.
	private void ResolveHit(Node3D body)
	{
		if (_hit || body == null || body == _source)
		{
			return;
		}

		_hit = true;

		if (body is IDamageable target)
		{
			DamageInfo damage = new DamageInfo(
				_definition.Damage,
				_source,
				_sourceFaction
			);

			target.ApplyDamage(damage);
		}

		QueueFree();
	}

	#endregion

	#region Appearance

	// Creates the visible bullet using the weapon definition.
	private void CreateVisual()
	{
		SphereMesh mesh = new SphereMesh();
		mesh.Radius = _definition.ProjectileRadius;
		mesh.Height = _definition.ProjectileRadius * 2.0f;
		mesh.RadialSegments = 8;
		mesh.Rings = 4;

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = _definition.ProjectileColor;
		material.EmissionEnabled = true;
		material.Emission = _definition.ProjectileColor;
		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = "Visual";
		visual.Mesh = mesh;
		AddChild(visual);
	}

	// Creates the bullet's matching collision sphere.
	private void CreateCollision()
	{
		SphereShape3D shape = new SphereShape3D();
		shape.Radius = _definition.ProjectileRadius;

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = shape;
		AddChild(collision);
	}

	#endregion
}

using Godot;

public partial class Projectile : Area3D
{
	#region Runtime

	private Vector3 _velocity;
	private float _lifeRemaining;
	private CollisionObject3D _source;
	private WeaponDefinition _definition;

	#endregion

	#region Setup

	// Receives weapon data and ship velocity after the projectile is positioned.
	public void Configure(
		WeaponDefinition definition,
		CollisionObject3D source,
		Vector3 sourceVelocity
	)
	{
		_definition = definition;
		_source = source;
		_lifeRemaining = definition.ProjectileLifetime;

		_velocity =
			-GlobalBasis.Z * definition.ProjectileSpeed
			+ sourceVelocity;

		CreateVisual();
		CreateCollision();
	}

	// Connects the collision event for this projectile.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	#endregion

	#region Flight

	// Moves the projectile and removes it when its lifetime expires.
	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		GlobalPosition += _velocity * seconds;
		_lifeRemaining -= seconds;

		if (_lifeRemaining <= 0.0f)
		{
			QueueFree();
		}
	}

	// Removes the projectile when it hits a body other than the firing ship.
	private void OnBodyEntered(Node3D body)
	{
		if (body == _source)
		{
			return;
		}

		QueueFree();
	}

	#endregion

	#region Appearance

	// Creates the small visible bullet from the weapon's projectile settings.
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

	// Creates a collision sphere matching the visible bullet.
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

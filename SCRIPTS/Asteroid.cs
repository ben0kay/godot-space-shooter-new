using Godot;

public partial class Asteroid : StaticBody3D, IDamageable
{
	#region Shape Settings

	[Export] public float HealthPerRadius = 3.0f;

	private float _radius = 3.0f;
	private ulong _shapeSeed = 1;
	private float _health;

	#endregion

	#region Setup

	// Receives the asteroid's size and shape seed, then sets health based on size.
	public void Configure(float radius, ulong shapeSeed)
	{
		_radius = radius;
		_shapeSeed = shapeSeed;
		_health = radius * HealthPerRadius;
	}

	// Builds this asteroid's visual mesh and matching collision when it spawns.
	public override void _Ready()
	{
		ArrayMesh mesh = CreateShape();

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = "Visual";
		visual.Mesh = mesh;
		AddChild(visual);

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = mesh.CreateConvexShape();
		AddChild(collision);
	}

	#endregion

	#region Damage

	// Reduces health when hit and removes the asteroid when health reaches zero.
	public void ApplyDamage(DamageInfo damage)
	{
		if (damage.Amount <= 0.0f || IsQueuedForDeletion())
		{
			return;
		}

		_health -= damage.Amount;

		if (_health <= 0.0f)
		{
			QueueFree();
		}
	}

	#endregion

	#region Shape Generation

	// Deforms a low-poly sphere into a repeatable irregular asteroid shape.
	private ArrayMesh CreateShape()
	{
		RandomNumberGenerator random = new RandomNumberGenerator();
		random.Seed = _shapeSeed;

		float phaseA = random.RandfRange(0, Mathf.Tau);
		float phaseB = random.RandfRange(0, Mathf.Tau);

		Vector3 proportions = new Vector3(
			random.RandfRange(0.75f, 1.25f),
			random.RandfRange(0.75f, 1.25f),
			random.RandfRange(0.75f, 1.25f)
		);

		SphereMesh source = new SphereMesh();
		source.Radius = 1.0f;
		source.Height = 2.0f;
		source.RadialSegments = 10;
		source.Rings = 6;

		ArrayMesh sourceMesh = new ArrayMesh();
		sourceMesh.AddSurfaceFromArrays(
			Mesh.PrimitiveType.Triangles,
			source.SurfaceGetArrays(0)
		);

		MeshDataTool data = new MeshDataTool();
		data.CreateFromSurface(sourceMesh, 0);

		for (int index = 0; index < data.GetVertexCount(); index++)
		{
			Vector3 point = data.GetVertex(index);
			Vector3 direction = point.Normalized();

			float variation =
				1.0f
				+ 0.16f * Mathf.Sin(direction.X * 7.0f + phaseA)
				+ 0.12f * Mathf.Cos(direction.Y * 6.0f + phaseB)
				+ 0.10f * Mathf.Sin(direction.Z * 9.0f + phaseA);

			Vector3 shapedPoint = new Vector3(
				point.X * proportions.X,
				point.Y * proportions.Y,
				point.Z * proportions.Z
			) * _radius * variation;

			data.SetVertex(index, shapedPoint);
		}

		ArrayMesh result = new ArrayMesh();
		data.CommitToSurface(result);

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = new Color(
			random.RandfRange(0.33f, 0.48f),
			random.RandfRange(0.33f, 0.44f),
			random.RandfRange(0.35f, 0.47f)
		);
		material.Roughness = 1.0f;

		result.SurfaceSetMaterial(0, material);

		return result;
	}

	#endregion
}

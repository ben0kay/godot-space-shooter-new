using Godot;

// Creates a distant decorative asteroid ring using shared meshes and spatial batches.
// Background rocks have no collision, deposits, damage or per-frame updates.
public partial class AsteroidBackdrop : Node3D
{
	#region Configuration

	[ExportGroup("Population")]

	[Export] public int AsteroidCount = 2400;
	[Export] public int Seed = 184;
	[Export] public Vector2 RadiusRange = new(5.0f, 28.0f);

	[ExportGroup("Belt Shape")]

	[Export] public Vector2 OuterRadii = new(7500.0f, 10000.0f);

	[Export(PropertyHint.Range, "0.1,0.95,0.01")]
	public float InnerRadiusFraction = 0.65f;

	[Export] public float HalfHeight = 450.0f;

	#endregion

	#region Setup

	// =========================================================
	// Builds reusable rock variants and distributes them among ring chunks.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		const int chunks = 24;
		const int variants = 4;

		int count = Mathf.Clamp(AsteroidCount, 0, 20000);
		if (count == 0) return;

		RandomNumberGenerator random = new() { Seed = (ulong)(uint)Seed };

		ArrayMesh[] meshes = new ArrayMesh[variants];
		for (int variant = 0; variant < variants; variant++)
			meshes[variant] = CreateRockMesh(random);

		int batchCount = chunks * variants;
		int each = count / batchCount;
		int remainder = count % batchCount;

		float inner = Mathf.Clamp(InnerRadiusFraction, 0.1f, 0.95f);
		float minimum = Mathf.Max(0.1f, Mathf.Min(RadiusRange.X, RadiusRange.Y));
		float maximum = Mathf.Max(minimum, Mathf.Max(RadiusRange.X, RadiusRange.Y));

		for (int chunk = 0; chunk < chunks; chunk++)
		{
			float middleAngle = (chunk + 0.5f) * Mathf.Tau / chunks;
			float middleRadius = (1.0f + inner) * 0.5f;

			Vector3 centre = new(
				Mathf.Cos(middleAngle) * OuterRadii.X * middleRadius,
				0,
				Mathf.Sin(middleAngle) * OuterRadii.Y * middleRadius
			);

			for (int variant = 0; variant < variants; variant++)
			{
				int batchIndex = chunk * variants + variant;
				int population = each + (batchIndex < remainder ? 1 : 0);
				if (population == 0) continue;

				MultiMesh multimesh = new()
				{
					TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
					Mesh = meshes[variant],
					InstanceCount = population
				};

				for (int index = 0; index < population; index++)
				{
					float angle = (chunk + random.Randf()) * Mathf.Tau / chunks;
					float radial = Mathf.Sqrt(
						Mathf.Lerp(inner * inner, 1.0f, random.Randf())
					);

					Vector3 position = new(
						Mathf.Cos(angle) * OuterRadii.X * radial,
						random.RandfRange(-HalfHeight, HalfHeight),
						Mathf.Sin(angle) * OuterRadii.Y * radial
					);

					float radius = random.RandfRange(minimum, maximum);

					Basis basis = Basis.FromEuler(new Vector3(
						random.RandfRange(0, Mathf.Tau),
						random.RandfRange(0, Mathf.Tau),
						random.RandfRange(0, Mathf.Tau)
					));

					basis = basis.Scaled(new Vector3(
						radius * random.RandfRange(0.75f, 1.25f),
						radius * random.RandfRange(0.75f, 1.25f),
						radius * random.RandfRange(0.75f, 1.25f)
					));

					multimesh.SetInstanceTransform(
						index, new Transform3D(basis, position - centre)
					);
				}

				AddChild(new MultiMeshInstance3D
				{
					Name = $"Chunk_{chunk:D2}_Variant_{variant}",
					Position = centre,
					Multimesh = multimesh,
					CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
				});
			}
		}
	}

	#endregion

	#region Mesh Generation

	// =========================================================
	// Builds a low-detail rock shared by many distant instances.
	private static ArrayMesh CreateRockMesh(RandomNumberGenerator random)
	{
		SphereMesh sphere = new()
		{
			Radius = 1.0f,
			Height = 2.0f,
			RadialSegments = 8,
			Rings = 4
		};

		ArrayMesh source = new();
		source.AddSurfaceFromArrays(
			Mesh.PrimitiveType.Triangles, sphere.SurfaceGetArrays(0)
		);

		MeshDataTool data = new();
		data.CreateFromSurface(source, 0);

		float phase = random.RandfRange(0, Mathf.Tau);

		for (int index = 0; index < data.GetVertexCount(); index++)
		{
			Vector3 point = data.GetVertex(index);
			float variation = 1.0f
				+ Mathf.Sin(point.X * 5.0f + phase) * 0.16f
				+ Mathf.Cos(point.Y * 7.0f + phase) * 0.12f
				+ Mathf.Sin(point.Z * 6.0f + phase) * 0.10f;

			data.SetVertex(index, point * variation);
		}

		ArrayMesh deformed = new();
		data.CommitToSurface(deformed);
		data.Dispose();

		SurfaceTool surface = new();
		surface.CreateFrom(deformed, 0);
		surface.SetSmoothGroup(0);
		surface.GenerateNormals();

		ArrayMesh mesh = surface.Commit();
		surface.Dispose();

		mesh.SurfaceSetMaterial(0, new StandardMaterial3D
		{
			AlbedoColor = new Color(0.25f, 0.26f, 0.28f).Lerp(
				new Color(0.33f, 0.28f, 0.23f), random.Randf()
			),
			Roughness = 1.0f,
			MetallicSpecular = 0.15f
		});

		return mesh;
	}

	#endregion
}

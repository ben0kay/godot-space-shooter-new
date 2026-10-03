using Godot;

// Applies sector-specific distance haze and generates a small static dust population.
public partial class SectorAtmosphere : Node3D
{
	#region Distance Haze

	[ExportGroup("Distance Haze")]

	[Export] public NodePath WorldEnvironmentPath =
		new("../SpaceEnvironment/WorldEnvironment");

	[Export] public bool HazeEnabled = true;
	[Export] public Color HazeColor = new(0.10f, 0.17f, 0.26f);

	// Higher values keep distant objects clearer.
	[Export] public float HazeHalfDistance = 18000.0f;

	#endregion

	#region Dust Clouds

	[ExportGroup("Dust Clouds")]

	[Export] public bool CloudsEnabled = true;
	[Export] public int CloudCount = 12;
	[Export] public int Seed = 287;

	[Export] public Vector3 CloudHalfExtents = new(1600, 220, 2300);
	[Export] public Vector2 CloudSizeRange = new(900, 1800);
	[Export] public Color CloudColor = new(0.30f, 0.45f, 0.62f);

	[Export(PropertyHint.Range, "0,0.3,0.005")]
	public float CloudOpacity = 0.07f;

	[Export] public float CloudNearFadeStart = 200.0f;
	[Export] public float CloudNearFadeEnd = 550.0f;

	#endregion

	#region Setup

	// =========================================================
	// Waits until SpaceEnvironment has finished creating its runtime environment.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		CallDeferred(nameof(BuildAtmosphere));
	}

	// =========================================================
	// Applies the haze and builds this sector's dust clouds once.
	private void BuildAtmosphere()
	{
		ApplyHaze();

		if (CloudsEnabled)
			CreateClouds();
	}

	#endregion

	#region Haze

	// =========================================================
	// Softens distant geometry while preserving the existing sky and nebula.
	private void ApplyHaze()
	{
		WorldEnvironment world =
			GetNodeOrNull<WorldEnvironment>(WorldEnvironmentPath);

		if (world?.Environment == null)
		{
			GD.PushError(
				$"{Name}: WorldEnvironmentPath does not point to an initialized environment."
			);
			return;
		}

		Godot.Environment environment =
			(Godot.Environment)world.Environment.Duplicate();

		environment.FogEnabled = HazeEnabled;
		environment.FogDensity =
			0.693147f / Mathf.Max(100.0f, HazeHalfDistance);

		environment.FogLightColor = HazeColor;
		environment.FogLightEnergy = 1.0f;
		environment.FogSunScatter = 0.0f;
		environment.FogAerialPerspective = 0.0f;
		environment.FogSkyAffect = 0.0f;
		environment.FogHeightDensity = 0.0f;

		// This pass uses ordinary fog, without volumetric fog.
		environment.VolumetricFogEnabled = false;

		world.Environment = environment;
	}

	#endregion

	#region Cloud Generation

	// =========================================================
	// Generates varied soft cloud cards with no collision or per-frame scripts.
	private void CreateClouds()
	{
		Shader shader = GD.Load<Shader>(
			"res://ENVIRONMENT/SectorDust.gdshader"
		);

		if (shader == null)
		{
			GD.PushError("Could not load ENVIRONMENT/SectorDust.gdshader.");
			return;
		}

		Node3D generated = new() { Name = "DustClouds" };
		AddChild(generated);

		RandomNumberGenerator random = new()
		{
			Seed = (ulong)(uint)Seed
		};

		int count = Mathf.Clamp(CloudCount, 0, 32);

		float minimumSize = Mathf.Max(
			10.0f, Mathf.Min(CloudSizeRange.X, CloudSizeRange.Y)
		);

		float maximumSize = Mathf.Max(
			minimumSize, Mathf.Max(CloudSizeRange.X, CloudSizeRange.Y)
		);

		Vector3 extents = new(
			Mathf.Abs(CloudHalfExtents.X),
			Mathf.Abs(CloudHalfExtents.Y),
			Mathf.Abs(CloudHalfExtents.Z)
		);

		for (int index = 0; index < count; index++)
		{
			float size = random.RandfRange(minimumSize, maximumSize);

			ShaderMaterial material = new() { Shader = shader };
			material.SetShaderParameter("cloud_color", CloudColor);
			material.SetShaderParameter(
				"opacity",
				Mathf.Clamp(CloudOpacity, 0, 0.3f)
					* random.RandfRange(0.65f, 1.0f)
			);

			material.SetShaderParameter(
				"noise_offset",
				new Vector2(
					random.RandfRange(0, 100),
					random.RandfRange(0, 100)
				)
			);

			material.SetShaderParameter(
				"pattern_rotation", random.RandfRange(0, Mathf.Tau)
			);

			material.SetShaderParameter(
				"near_fade_start", Mathf.Max(0, CloudNearFadeStart)
			);

			material.SetShaderParameter(
				"near_fade_end",
				Mathf.Max(CloudNearFadeStart + 1, CloudNearFadeEnd)
			);

			generated.AddChild(new MeshInstance3D
			{
				Name = $"Cloud_{index:D2}",
				Position = new Vector3(
					random.RandfRange(-extents.X, extents.X),
					random.RandfRange(-extents.Y, extents.Y),
					random.RandfRange(-extents.Z, extents.Z)
				),
				Mesh = new QuadMesh
				{
					Size = new Vector2(
						size,
						size * random.RandfRange(0.55f, 0.9f)
					)
				},
				MaterialOverride = material,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,

				// Keeps billboard bounds conservative as the camera turns.
				ExtraCullMargin = size * 0.5f
			});
		}
	}

	#endregion
}
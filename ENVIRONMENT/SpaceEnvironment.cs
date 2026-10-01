using Godot;

// Builds reusable sky and sunlight settings for any space sector.
public partial class SpaceEnvironment : Node3D
{
	#region Settings

	[ExportGroup("Space Appearance")]

	[Export] public float StarBrightness = 0.9f;
	[Export] public float NebulaStrength = 0.12f;
	[Export] public float SunEnergy = 1.4f;
	[Export] public float AmbientEnergy = 0.12f;

	#endregion

	#region Setup

	// =========================================================
	// Builds this environment instance from its authored settings.
	// =========================================================
	public override void _Ready()
	{
		ConfigureSky(this, StarBrightness, NebulaStrength, AmbientEnergy);
		AddSun(this, SunEnergy);

		SetProcess(false);
		SetPhysicsProcess(false);
	}

	// =========================================================
	// Reuses a local WorldEnvironment and preserves its saved effects.
	// =========================================================
	public static void ConfigureSky(
		Node3D parent,
		float stars,
		float nebula,
		float ambient
	)
	{
		Shader shader = GD.Load<Shader>(
            "res://ENVIRONMENT/SpaceSky.gdshader"
		);

		if (shader == null)
		{
			GD.PushError("Could not load ENVIRONMENT/SpaceSky.gdshader.");
			return;
		}

		ShaderMaterial material = new()
		{
			Shader = shader
		};

		material.SetShaderParameter(
			"star_brightness",
			Mathf.Max(0.0f, stars)
		);

		material.SetShaderParameter(
			"nebula_strength",
			Mathf.Max(0.0f, nebula)
		);

		WorldEnvironment world =
			parent.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");

		if (world == null)
		{
			world = new WorldEnvironment
			{
				Name = "WorldEnvironment"
			};

			parent.AddChild(world);
		}

		Godot.Environment environment =
			world.Environment != null
				? (Godot.Environment)world.Environment.Duplicate()
				: new Godot.Environment();

		environment.BackgroundMode = Godot.Environment.BGMode.Sky;

		environment.Sky = new Sky
		{
			SkyMaterial = material
		};

		environment.AmbientLightSource =
			Godot.Environment.AmbientSource.Color;

		environment.AmbientLightColor =
			new Color(0.32f, 0.4f, 0.55f);

		environment.AmbientLightEnergy = Mathf.Max(0.0f, ambient);

		environment.ReflectedLightSource =
			Godot.Environment.ReflectionSource.Disabled;

		environment.FogEnabled = false;
		environment.VolumetricFogEnabled = false;

		world.Environment = environment;
	}

	// =========================================================
	// Adds one directional sun to the supplied sector or environment.
	// =========================================================
	public static void AddSun(Node3D parent, float energy)
	{
		parent.AddChild(new DirectionalLight3D
		{
			Name = "DistantSun",
			RotationDegrees = new Vector3(-35.0f, -30.0f, 0.0f),
			LightColor = new Color(1.0f, 0.95f, 0.88f),
			LightEnergy = Mathf.Max(0.0f, energy),
			ShadowEnabled = true,
			DirectionalShadowMaxDistance = 350.0f
		});
	}

	#endregion
}

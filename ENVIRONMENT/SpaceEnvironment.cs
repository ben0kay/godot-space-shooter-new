using Godot;

// Builds a shared space sky and aligns its visible sun with directional lighting.
public partial class SpaceEnvironment : Node3D
{
	#region Appearance

	[ExportGroup("Space Appearance")]

	[Export] public float StarBrightness = 0.9f;
	[Export] public float NebulaStrength = 0.06f;
	[Export] public float SunEnergy = 1.8f;
	[Export] public float AmbientEnergy = 0.45f;

	[ExportGroup("Sun")]

	// Direction from the sector towards its distant sun.
	[Export] public Vector3 SunDirection = new(-0.65f, 0.35f, -0.67f);
	[Export] public Color SunColor = new(1.0f, 0.91f, 0.76f);
	[Export] public float SunAngularSizeDegrees = 0.65f;

	#endregion

	#region Setup

	// =========================================================
	// Creates the sky and light from one shared sun direction.
	public override void _Ready()
	{
		ConfigureSky(
			this, StarBrightness, NebulaStrength,
			AmbientEnergy, SunDirection
		);

		WorldEnvironment world =
			GetNodeOrNull<WorldEnvironment>("WorldEnvironment");

		if (world?.Environment?.Sky?.SkyMaterial is ShaderMaterial material)
		{
			material.SetShaderParameter("sun_color", SunColor);
			material.SetShaderParameter(
				"sun_radius",
				Mathf.DegToRad(
					Mathf.Clamp(SunAngularSizeDegrees, 0.1f, 3.0f)
				)
			);
		}

		AddSun(this, SunEnergy, SunDirection, SunColor);

		SetProcess(false);
		SetPhysicsProcess(false);
	}

	// =========================================================
	// Reuses the local environment while preserving authored glow settings.
	public static void ConfigureSky(
		Node3D parent,
		float stars,
		float nebula,
		float ambient,
		Vector3? sunDirection = null
	)
	{
		Shader shader = GD.Load<Shader>("res://ENVIRONMENT/SpaceSky.gdshader");

		if (shader == null)
		{
			GD.PushError("Could not load ENVIRONMENT/SpaceSky.gdshader.");
			return;
		}

		ShaderMaterial material = new() { Shader = shader };
		material.SetShaderParameter("star_brightness", Mathf.Max(0, stars));
		material.SetShaderParameter("nebula_strength", Mathf.Max(0, nebula));
		material.SetShaderParameter(
			"sun_direction",
			ResolveSunDirection(sunDirection)
		);

		WorldEnvironment world =
			parent.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");

		if (world == null)
		{
			world = new WorldEnvironment { Name = "WorldEnvironment" };
			parent.AddChild(world);
		}

		Godot.Environment environment = world.Environment != null
			? (Godot.Environment)world.Environment.Duplicate()
			: new Godot.Environment();

		environment.BackgroundMode = Godot.Environment.BGMode.Sky;
		environment.Sky = new Sky { SkyMaterial = material };

		environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
		environment.AmbientLightColor = new Color(0.32f, 0.40f, 0.55f);
		environment.AmbientLightEnergy = Mathf.Max(0, ambient);

		environment.ReflectedLightSource =
			Godot.Environment.ReflectionSource.Disabled;

		environment.FogEnabled = false;
		environment.VolumetricFogEnabled = false;

		world.Environment = environment;
	}

	// =========================================================
	// Reuses or creates the sunlight and aims its rays away from the visible sun.
	public static void AddSun(
		Node3D parent,
		float energy,
		Vector3? direction = null,
		Color? color = null
	)
	{
		Vector3 towardsSun = ResolveSunDirection(direction);
		Vector3 up = Mathf.Abs(towardsSun.Dot(Vector3.Up)) > 0.98f
			? Vector3.Forward : Vector3.Up;

		DirectionalLight3D light =
			parent.GetNodeOrNull<DirectionalLight3D>("DistantSun");

		if (light == null)
		{
			light = new DirectionalLight3D { Name = "DistantSun" };
			parent.AddChild(light);
		}

		light.Basis = Basis.LookingAt(-towardsSun, up);
		light.LightColor = color ?? new Color(1.0f, 0.91f, 0.76f);
		light.LightEnergy = Mathf.Max(0, energy);
		light.ShadowEnabled = true;
		light.DirectionalShadowMaxDistance = 450.0f;
	}

	// =========================================================
	// Ensures sunlight always has a usable normalized direction.
	private static Vector3 ResolveSunDirection(Vector3? direction)
	{
		Vector3 value = direction ?? new Vector3(-0.65f, 0.35f, -0.67f);

		return value.LengthSquared() > 0.0001f
			? value.Normalized()
			: new Vector3(-0.65f, 0.35f, -0.67f).Normalized();
	}

	#endregion
}

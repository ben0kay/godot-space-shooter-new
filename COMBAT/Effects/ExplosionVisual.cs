using Godot;

// Plays a visual-only explosion flash and expanding shell, then removes itself.
public partial class ExplosionVisual : Node3D
{
	#region Shared Assets

	private static SphereMesh _sphere;
	private static Shader _shader;

	#endregion

	#region Runtime

	private ExplosionVisualSettings _settings;

	private MeshInstance3D _shell;
	private MeshInstance3D _flash;

	private ShaderMaterial _shellMaterial;
	private ShaderMaterial _flashMaterial;

	private float _radius;
	private float _elapsed;
	private float _duration;
	private float _flashDuration;

	#endregion

	#region Creation

	// =========================================================
	// Creates an independent effect that survives the projectile's removal.
	public static void Spawn(
		Node3D context,
		Vector3 position,
		float damageRadius,
		ExplosionVisualSettings settings,
		Faction faction
	)
	{
		if (settings == null
			|| !GodotObject.IsInstanceValid(context))
		{
			return;
		}

		Node scene = WorldSector.GetContentParent(context);

		if (!GodotObject.IsInstanceValid(scene))
		{
			return;
		}

		if (_shader == null)
		{
			_shader = GD.Load<Shader>(
				"res://COMBAT/Effects/ExplosionShockwave.gdshader"
			);
		}

		if (_shader == null)
		{
			GD.PushError(
				"Could not load ExplosionShockwave.gdshader."
			);

			return;
		}

		if (_sphere == null)
		{
			_sphere = new SphereMesh
			{
				Radius = 1.0f,
				Height = 2.0f,
				RadialSegments = 32,
				Rings = 16
			};
		}

		ExplosionVisual effect = new()
		{
			Name = "ExplosionVisual"
		};

		scene.AddChild(effect);

		// Keep the effect in world space even if the scene root is transformed.
		effect.TopLevel = true;
		effect.GlobalTransform = new Transform3D(
			Basis.Identity,
			position
		);

		effect.Initialize(damageRadius, settings, faction);
	}

	// =========================================================
	// Builds the shell and flash using independent material parameters.
	private void Initialize(
		float damageRadius,
		ExplosionVisualSettings settings,
		Faction faction
	)
	{
		_settings = settings;

		_radius = Mathf.Max(
			0.05f,
			damageRadius * Mathf.Max(0.01f, settings.RadiusScale)
		);

		_duration = Mathf.Max(0.01f, settings.Duration);
		_flashDuration = Mathf.Max(0.01f, settings.FlashDuration);

		Color color = settings.UseFactionPalette
			? FactionPalettes.Get(faction).Energy
			: settings.Color;

		_shellMaterial = CreateMaterial(
			color,
			settings.Brightness,
			1.0f,
			settings.Distortion
		);

		_flashMaterial = CreateMaterial(
			color.Lerp(Colors.White, 0.75f),
			settings.FlashBrightness,
			0.0f,
			0.0f
		);

		_shell = CreateSphere("Shockwave", _shellMaterial);
		_flash = CreateSphere("Flash", _flashMaterial);

		UpdateAppearance();
	}

	// =========================================================
	// Creates a material for either a rim shell or a filled central flash.
	private ShaderMaterial CreateMaterial(
		Color color,
		float brightness,
		float rimMix,
		float distortion
	)
	{
		ShaderMaterial material = new()
		{
			Shader = _shader
		};

		material.SetShaderParameter("energy_color", color);
		material.SetShaderParameter(
			"brightness",
			Mathf.Max(0.0f, brightness)
		);

		material.SetShaderParameter(
			"rim_power",
			Mathf.Max(0.01f, _settings.RimPower)
		);

		material.SetShaderParameter("rim_mix", rimMix);
		material.SetShaderParameter("distortion", distortion);

		return material;
	}

	// =========================================================
	// Creates one shadow-free visual mesh from the shared unit sphere.
	private MeshInstance3D CreateSphere(
		string name,
		ShaderMaterial material
	)
	{
		MeshInstance3D mesh = new()
		{
			Name = name,
			Mesh = _sphere,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};

		AddChild(mesh);
		return mesh;
	}

	#endregion

	#region Animation

	// =========================================================
	// Animates the visual lifetime independently of explosion damage.
	public override void _Process(double delta)
	{
		if (_settings == null)
		{
			return;
		}

		_elapsed += (float)delta;

		UpdateAppearance();

		if (_elapsed >= Mathf.Max(_duration, _flashDuration))
		{
			QueueFree();
		}
	}

	// =========================================================
	// Expands the shell rapidly and fades both visual layers.
	private void UpdateAppearance()
	{
		float progress = Mathf.Clamp(
			_elapsed / _duration,
			0.0f,
			1.0f
		);

		// Fast initial expansion that slows near the final radius.
		float expansion = 1.0f - Mathf.Pow(1.0f - progress, 2.0f);

		float start = Mathf.Clamp(
			_settings.StartRadiusFraction,
			0.001f,
			1.0f
		);

		float radius = _radius * Mathf.Lerp(
			start,
			1.0f,
			expansion
		);

		_shell.Scale = Vector3.One * radius;
		_shell.Visible = progress < 1.0f;

		_shellMaterial.SetShaderParameter("age", _elapsed);
		_shellMaterial.SetShaderParameter(
			"opacity",
			Mathf.Clamp(_settings.Opacity, 0.0f, 1.0f)
				* Mathf.Pow(1.0f - progress, 1.4f)
		);

		float flashProgress = Mathf.Clamp(
			_elapsed / _flashDuration,
			0.0f,
			1.0f
		);

		float flashRadius = _radius * Mathf.Max(
			0.001f,
			_settings.FlashRadiusFraction
		);

		_flash.Scale = Vector3.One * flashRadius
			* Mathf.Lerp(0.65f, 1.0f, flashProgress);

		_flash.Visible = flashProgress < 1.0f;

		_flashMaterial.SetShaderParameter(
			"opacity",
			Mathf.Pow(1.0f - flashProgress, 2.0f)
		);
	}

	#endregion
}
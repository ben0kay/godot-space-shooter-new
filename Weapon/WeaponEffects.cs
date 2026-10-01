using Godot;

public static class WeaponEffects
{

		private static Shader _glowTrailShader;

	#region Colour

	// Resolves the effect colour without changing a shared resource.
	public static Color GetColor(
		WeaponEffectsProfile profile,
		Faction faction
	)
	{
		return profile.UseFactionPalette
			? FactionPalettes.Get(faction).Energy
			: profile.Color;
	}

	#endregion

	#region Muzzle And Impact

	// Emits a short flash when a projectile is actually created.
	public static void Muzzle(
		Node3D emitter,
		WeaponEffectsProfile profile,
		Faction faction
	)
	{
		if (profile == null || profile.MuzzleParticles <= 0)
		{
			return;
		}

		Burst(
			emitter.GetTree().CurrentScene,
			emitter.GlobalTransform,
			profile,
			faction,
			profile.MuzzleParticles,
			profile.MuzzleLifetime,
			profile.MuzzleSpeed,
			Vector3.Forward,
			25.0f
		);
	}

	// Emits particles away from the struck surface.
	public static void Impact(
		Node3D projectile,
		Vector3 position,
		Vector3 normal,
		WeaponEffectsProfile profile,
		Faction faction
	)
	{
		if (profile == null || profile.ImpactParticles <= 0)
		{
			return;
		}

		if (normal.LengthSquared() < 0.001f)
		{
			normal = Vector3.Up;
		}

		Burst(
			projectile.GetTree().CurrentScene,
			new Transform3D(Basis.Identity, position),
			profile,
			faction,
			profile.ImpactParticles,
			profile.ImpactLifetime,
			profile.ImpactSpeed,
			normal.Normalized(),
			65.0f
		);
	}

	#endregion

	#region Trail

		// =========================================================
	// Creates world-space trail particles with optional soft shader glow.
	public static GpuParticles3D CreateTrail(
		Node3D projectile,
		WeaponEffectsProfile profile,
		Faction faction
	)
	{
		if (profile == null || profile.TrailParticles <= 0)
		{
			return null;
		}

		GpuParticles3D particles = Create(
			profile,
			faction,
			profile.TrailParticles,
			profile.TrailLifetime,
			0.0f,
			Vector3.Back,
			0.0f
		);

		particles.Name = "Trail";
		particles.OneShot = false;
		particles.Position = profile.TrailOffset;
		particles.CastShadow =
			GeometryInstance3D.ShadowCastingSetting.Off;

		float bounds = Mathf.Max(
			1.0f,
			profile.TrailBoundsRadius
		);

		particles.VisibilityAabb = new Aabb(
			-Vector3.One * bounds,
			Vector3.One * bounds * 2.0f
		);

		if (profile.UseGlowTrail)
		{
			if (_glowTrailShader == null)
			{
				_glowTrailShader = GD.Load<Shader>(
					"res://Weapon/ProjectileGlowTrail.gdshader"
				);
			}

			if (_glowTrailShader != null)
			{
				ShaderMaterial material = new()
				{
					Shader = _glowTrailShader
				};

				material.SetShaderParameter(
					"energy_color",
					GetColor(profile, faction)
				);

				material.SetShaderParameter(
					"brightness",
					Mathf.Max(0.0f, profile.EmissionEnergy)
				);

				float width = Mathf.Max(
					0.01f,
					profile.GlowTrailWidth
				);

				particles.DrawPass1 = new QuadMesh
				{
					Size = Vector2.One * width,
					Material = material
				};

				if (particles.ProcessMaterial
					is ParticleProcessMaterial process)
				{
					// Slight variation makes the wake less perfectly uniform.
					process.EmissionShape =
						ParticleProcessMaterial.EmissionShapeEnum.Sphere;

					process.EmissionSphereRadius = width * 0.06f;
					process.ScaleMin = 0.75f;
					process.ScaleMax = 1.0f;
				}
			}
			else
			{
				GD.PushError(
					"Could not load ProjectileGlowTrail.gdshader."
				);
			}
		}

		projectile.AddChild(particles);
		particles.Emitting = true;

		return particles;
	}
	// Detaches the stopped trail so its existing particles can finish.
	public static void FinishTrail(GpuParticles3D trail)
	{
		if (!GodotObject.IsInstanceValid(trail))
		{
			return;
		}

		trail.Emitting = false;
		trail.Reparent(trail.GetTree().CurrentScene, true);

		trail.GetTree().CreateTimer(trail.Lifetime + 0.1).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(trail))
			{
				trail.QueueFree();
			}
		};
	}

	#endregion

	#region Particle Construction

	// Creates a burst that survives removal of the firing or hit node.
	private static void Burst(
		Node parent,
		Transform3D transform,
		WeaponEffectsProfile profile,
		Faction faction,
		int amount,
		float lifetime,
		float speed,
		Vector3 direction,
		float spread
	)
	{
		GpuParticles3D particles = Create(
			profile,
			faction,
			amount,
			lifetime,
			speed,
			direction,
			spread
		);

		particles.OneShot = true;
		particles.Explosiveness = 1.0f;

		parent.AddChild(particles);
		particles.GlobalTransform = transform;
		particles.Restart();

		parent.GetTree().CreateTimer(particles.Lifetime + 0.2).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(particles))
			{
				particles.QueueFree();
			}
		};
	}

	// Builds the initial particle appearance and emission settings.
	private static GpuParticles3D Create(
		WeaponEffectsProfile profile,
		Faction faction,
		int amount,
		float lifetime,
		float speed,
		Vector3 direction,
		float spread
	)
	{
		Color color = GetColor(profile, faction);
		float radius = Mathf.Max(0.005f, profile.ParticleRadius);

		StandardMaterial3D material = new()
		{
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = profile.EmissionEnergy,
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

		ParticleProcessMaterial process = new()
		{
			Direction = direction,
			Spread = spread,
			Gravity = Vector3.Zero,
			InitialVelocityMin = speed * 0.5f,
			InitialVelocityMax = speed,
			ScaleMin = 0.5f,
			ScaleMax = 1.0f
		};

		return new GpuParticles3D
		{
			Amount = Mathf.Max(1, amount),
			Lifetime = Mathf.Max(0.02f, lifetime),
			LocalCoords = false,
			Emitting = false,
			ProcessMaterial = process,
			DrawPass1 = mesh,
			VisibilityAabb = new Aabb(
				new Vector3(-8, -8, -8),
				new Vector3(16, 16, 16)
			)
		};
	}

	#endregion
}

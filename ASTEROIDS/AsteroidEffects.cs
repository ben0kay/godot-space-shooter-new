using Godot;

// Creates bounded cosmetic rock-fragment bursts for asteroid impacts and destruction.
public static class AsteroidEffects
{
	#region Shared Assets

	private static BoxMesh _fragmentMesh;
	private static int _activeBursts;

	private const int MaximumActiveBursts = 48;

	#endregion

	#region Effects

	// =========================================================
	// Emits a small burst of rock fragments away from a damaged surface.
	public static void Impact(
		Node3D context,
		Vector3 position,
		Vector3 normal,
		float radius
	)
	{
		Vector3 direction = normal.LengthSquared() > 0.001f
			? normal.Normalized()
			: Vector3.Up;

		float size = Mathf.Clamp(radius * 0.015f, 0.06f, 0.3f);

		CreateBurst(
			context,
			position + direction * 0.03f,
			direction,
			7,
			0.55f,
			size,
			3.0f,
			9.0f,
			65.0f,
			0.0f
		);
	}

	// =========================================================
	// Emits outward-flying fragments when an asteroid breaks apart.
	public static void Destruction(
		Node3D context,
		Vector3 position,
		float radius
	)
	{
		float size = Mathf.Clamp(radius * 0.055f, 0.15f, 1.5f);

		float speed = Mathf.Clamp(
			radius * 0.7f,
			5.0f,
			22.0f
		);

		CreateBurst(
			context,
			position,
			Vector3.Up,
			36,
			1.3f,
			size,
			speed * 0.4f,
			speed,
			180.0f,
			radius * 0.55f
		);
	}

	#endregion

	#region Burst Creation

	// =========================================================
	// Creates one reusable GPU emitter and removes it after its particles finish.
	private static void CreateBurst(
		Node3D context,
		Vector3 position,
		Vector3 direction,
		int amount,
		float lifetime,
		float size,
		float speedMin,
		float speedMax,
		float spread,
		float emissionRadius
	)
	{
		if (_activeBursts >= MaximumActiveBursts
			|| !GodotObject.IsInstanceValid(context))
		{
			return;
		}

		Node scene = context.GetTree().CurrentScene;

		if (scene == null)
		{
			return;
		}

		EnsureMesh();

		Gradient fade = new Gradient
		{
			Offsets = new float[] { 0.0f, 0.7f, 1.0f },
			Colors = new Color[]
			{
				Colors.White,
				Colors.White,
				new Color(1.0f, 1.0f, 1.0f, 0.0f)
			}
		};

		ParticleProcessMaterial process = new ParticleProcessMaterial
		{
			Direction = direction,
			Spread = spread,
			Gravity = Vector3.Zero,

			InitialVelocityMin = speedMin,
			InitialVelocityMax = speedMax,

			ScaleMin = size * 0.5f,
			ScaleMax = size,

			AngularVelocityMin = -180.0f,
			AngularVelocityMax = 180.0f,

			ColorRamp = new GradientTexture1D
			{
				Gradient = fade
			}
		};

		if (emissionRadius > 0.0f)
		{
			process.EmissionShape =
				ParticleProcessMaterial.EmissionShapeEnum.Sphere;

			process.EmissionSphereRadius = emissionRadius;
		}

		float bounds = emissionRadius + speedMax * lifetime + size + 1.0f;

		GpuParticles3D particles = new GpuParticles3D
		{
			Name = "AsteroidDebris",
			Amount = amount,
			Lifetime = lifetime,
			OneShot = true,
			Explosiveness = 1.0f,
			LocalCoords = false,
			Emitting = false,

			ProcessMaterial = process,
			DrawPass1 = _fragmentMesh,

			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,

			VisibilityAabb = new Aabb(
				Vector3.One * -bounds,
				Vector3.One * bounds * 2.0f
			)
		};

		scene.AddChild(particles);

		particles.GlobalPosition = position;

		_activeBursts++;

		particles.TreeExiting += () =>
		{
			_activeBursts = Mathf.Max(0, _activeBursts - 1);
		};

		particles.Emitting = true;
		particles.Restart();

		context.GetTree().CreateTimer(lifetime + 0.3f).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(particles))
			{
				particles.QueueFree();
			}
		};
	}

	// =========================================================
	// Creates the shared fragment mesh and material only once.
	private static void EnsureMesh()
	{
		if (_fragmentMesh != null)
		{
			return;
		}

		StandardMaterial3D material = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.48f, 0.44f, 0.4f),
			Roughness = 1.0f,
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha
		};

		_fragmentMesh = new BoxMesh
		{
			Size = new Vector3(1.0f, 0.65f, 0.8f),
			Material = material
		};
	}

	#endregion
}

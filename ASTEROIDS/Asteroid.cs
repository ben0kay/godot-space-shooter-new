using Godot;

// Builds seeded asteroids with size-based health, optional rotation, and cosmetic destruction.
public partial class Asteroid : StaticBody3D, IDamageable
{
	#region Health Settings

	[Export] public float HealthPerRadius = 3.0f;

	public float Health => _health;
	public float MaximumHealth => _maximumHealth;

	public float HealthFraction => _maximumHealth > 0.0f
		? Mathf.Clamp(_health / _maximumHealth, 0.0f, 1.0f)
		: 0.0f;

	#endregion

	#region Rotation Settings

	[Export] public bool RotationEnabled = true;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float RotationChance = 0.45f;

	[Export] public float RotationSpeedMinDegrees = 0.5f;
	[Export] public float RotationSpeedMaxDegrees = 2.5f;

	#endregion

	#region Effects Settings

	[Export] public bool ImpactDebrisEnabled = true;
	[Export] public bool DestructionDebrisEnabled = true;

	// Limits debris emission during sustained beam damage and rapid gunfire.
	[Export] public float ImpactDebrisInterval = 0.1f;

	#endregion

	#region Runtime

	private float _radius = 3.0f;
	private ulong _shapeSeed = 1;

	private float _health;
	private float _maximumHealth;

	private Vector3 _rotationAxis;
	private float _rotationSpeed;

	private ulong _nextImpactTime;
	private bool _destroyed;

	#endregion

	#region Setup

	// =========================================================
	// Receives the procedural size and seed before the asteroid enters the scene.
	public void Configure(float radius, ulong shapeSeed)
	{
		_radius = Mathf.Max(0.1f, radius);
		_shapeSeed = shapeSeed;
	}

	// =========================================================
	// Builds the asteroid, initializes its health, and chooses its rotation.
	public override void _Ready()
	{
		_maximumHealth = Mathf.Max(
			1.0f,
			_radius * HealthPerRadius
		);

		_health = _maximumHealth;

		ArrayMesh mesh = CreateShape();

		AddChild(new MeshInstance3D
		{
			Name = "Visual",
			Mesh = mesh
		});

		AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = mesh.CreateConvexShape()
		});

		ConfigureRotation();
	}

	// =========================================================
	// Chooses a repeatable rotation axis and speed for selected asteroids.
	private void ConfigureRotation()
	{
		RandomNumberGenerator random = new RandomNumberGenerator
		{
			Seed = _shapeSeed ^ 0x9E3779B9UL
		};

		bool rotates = RotationEnabled
			&& random.Randf() < Mathf.Clamp(RotationChance, 0.0f, 1.0f);

		if (!rotates)
		{
			SetPhysicsProcess(false);
			return;
		}

		_rotationAxis = new Vector3(
			random.RandfRange(-1.0f, 1.0f),
			random.RandfRange(-1.0f, 1.0f),
			random.RandfRange(-1.0f, 1.0f)
		);

		_rotationAxis = _rotationAxis.LengthSquared() > 0.001f
			? _rotationAxis.Normalized()
			: Vector3.Up;

		float minimum = Mathf.Max(
			0.0f,
			Mathf.Min(RotationSpeedMinDegrees, RotationSpeedMaxDegrees)
		);

		float maximum = Mathf.Max(
			minimum,
			Mathf.Max(RotationSpeedMinDegrees, RotationSpeedMaxDegrees)
		);

		_rotationSpeed = Mathf.DegToRad(
			random.RandfRange(minimum, maximum)
		);

		SetPhysicsProcess(_rotationSpeed > 0.0f);
	}

	#endregion

	#region Rotation

	// =========================================================
	// Slowly rotates the visible rock and its matching collision together.
	public override void _PhysicsProcess(double delta)
	{
		if (!_destroyed)
		{
			RotateObjectLocal(
				_rotationAxis,
				_rotationSpeed * (float)delta
			);
		}
	}

	#endregion

	#region Damage

	// =========================================================
	// Applies damage, emits throttled contact debris, and destroys depleted asteroids.
	public void ApplyDamage(DamageInfo damage)
	{
		if (_destroyed
			|| IsQueuedForDeletion()
			|| damage.Amount <= 0.0f)
		{
			return;
		}

		_health = Mathf.Max(0.0f, _health - damage.Amount);

		if (_health <= 0.0f)
		{
			Destroy();
			return;
		}

		if (!ImpactDebrisEnabled || !damage.HasImpact)
		{
			return;
		}

		ulong now = Time.GetTicksMsec();

		if (now < _nextImpactTime)
		{
			return;
		}

		_nextImpactTime = now + (ulong)(
			Mathf.Max(0.02f, ImpactDebrisInterval) * 1000.0f
		);

		AsteroidEffects.Impact(
			this,
			damage.ImpactPosition,
			damage.ImpactNormal,
			_radius
		);
	}

	// =========================================================
	// Emits independent debris and removes this asteroid safely.
	private void Destroy()
	{
		_destroyed = true;

		SetPhysicsProcess(false);
		Hide();

		if (DestructionDebrisEnabled)
		{
			AsteroidEffects.Destruction(
				this,
				GlobalPosition,
				_radius
			);
		}

		QueueFree();
	}

	#endregion

	#region Shape Generation

	// =========================================================
	// Deforms a low-poly sphere into a repeatable irregular asteroid shape.
	private ArrayMesh CreateShape()
	{
		RandomNumberGenerator random = new RandomNumberGenerator
		{
			Seed = _shapeSeed
		};

		float phaseA = random.RandfRange(0.0f, Mathf.Tau);
		float phaseB = random.RandfRange(0.0f, Mathf.Tau);

		Vector3 proportions = new Vector3(
			random.RandfRange(0.75f, 1.25f),
			random.RandfRange(0.75f, 1.25f),
			random.RandfRange(0.75f, 1.25f)
		);

		SphereMesh source = new SphereMesh
		{
			Radius = 1.0f,
			Height = 2.0f,
			RadialSegments = 10,
			Rings = 6
		};

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

		StandardMaterial3D material = new StandardMaterial3D
		{
			AlbedoColor = new Color(
				random.RandfRange(0.33f, 0.48f),
				random.RandfRange(0.33f, 0.44f),
				random.RandfRange(0.35f, 0.47f)
			),
			Roughness = 1.0f
		};

		result.SurfaceSetMaterial(0, material);

		return result;
	}

	#endregion
}

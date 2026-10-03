using Godot;

// Builds seeded asteroids with size-based health, optional rotation, and cosmetic destruction.
public partial class Asteroid : StaticBody3D, IDamageable
{
	#region Health Settings

	[Export] public float HealthPerRadius = 3.0f;
	[Export] public bool Destructible = true;

	public float Health => _health;
	public float MaximumHealth => _maximumHealth;

	public float HealthFraction => _maximumHealth > 0.0f
		? Mathf.Clamp(_health / _maximumHealth, 0.0f, 1.0f)
		: 0.0f;

	#endregion

	#region Identity And Size

[Export] public string PersistentId = "";

public float Radius => _radius;

// Conservative bound for the current shape generator:
// maximum deformation 1.38 × maximum proportion 1.25 = 1.725.
public float PlacementRadius => _radius * 1.8f;

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

		#region Damage Appearance

	[Export] public bool CracksEnabled = true;
	[Export] public float CrackDensity = 3.5f;
	[Export] public float CrackWidth = 0.07f;

	[Export] public float ImpactFlashDuration = 0.14f;
	[Export] public bool DestructionDustEnabled = true;

	private static Shader _damageShader;

	private ShaderMaterial _damageMaterial;
	private float _flashRemaining;

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
	// Builds the asteroid and registers its shared detail checks.
	// =========================================================
	public override void _Ready()
	{
		_maximumHealth = Mathf.Max(
			1.0f,
			_radius * HealthPerRadius
		);

		_health = _maximumHealth;

		ArrayMesh mesh = CreateShape();

		_visual = new MeshInstance3D
		{
			Name = "Visual",
			Mesh = mesh
		};

		AddChild(_visual);

		AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = mesh.CreateConvexShape()
		});

		ConfigureDamageVisuals(_visual, mesh);

		SetProcess(false);

		ConfigureRotation();

		if (!DistanceOptimizationEnabled)
		{
			return;
		}

		_detailManager = DistanceDetailManager.Find(this);

		if (!GodotObject.IsInstanceValid(_detailManager))
		{
			GD.PushWarning(
				$"{Name}: WorldDetail Autoload is missing. "
				+ "Keeping full asteroid detail."
			);

			return;
		}

		_detailManager.Register(
			this,
			ApplyDistanceDetail,
			PlacementRadius,
			FullDetailDistance,
			SimpleDetailDistance
		);
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
// Applies damage only to destructible rocks while retaining surface hit feedback.
public void ApplyDamage(DamageInfo damage)
{
	if (_destroyed
		|| IsQueuedForDeletion()
		|| damage.Amount <= 0.0f)
	{
		return;
	}

	if (Destructible)
	{
		_health = Mathf.Max(0.0f, _health - damage.Amount);

		if (_health <= 0.0f)
		{
			Destroy();
			return;
		}
	}

	UpdateDamageVisuals(damage);

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
// Removes a destructible asteroid after emitting sector-owned debris and dust.
private void Destroy()
{
	if (!Destructible || _destroyed)
	{
		return;
	}

	_destroyed = true;

	SetPhysicsProcess(false);
	SetProcess(false);
	Hide();

	if (DestructionDebrisEnabled)
	{
		AsteroidEffects.Destruction(
			this,
			GlobalPosition,
			_radius
		);
	}

	if (DestructionDustEnabled)
	{
		AsteroidEffects.Dust(
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
// Builds a seeded irregular rock and recalculates its surface normals.
private ArrayMesh CreateShape()
{
	RandomNumberGenerator random = new() { Seed = _shapeSeed };

	FastNoiseLite noise = new()
	{
		Seed = unchecked((int)_shapeSeed),
		NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
		Frequency = 2.4f,
		FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
		FractalOctaves = 3
	};

	Vector3 proportions = new(
		random.RandfRange(0.78f, 1.22f),
		random.RandfRange(0.78f, 1.22f),
		random.RandfRange(0.78f, 1.22f)
	);

	Vector3 offset = new(
		random.RandfRange(-100, 100),
		random.RandfRange(-100, 100),
		random.RandfRange(-100, 100)
	);

	SphereMesh source = new()
	{
		Radius = 1.0f,
		Height = 2.0f,
		RadialSegments = 10,
		Rings = 6
	};

	ArrayMesh sourceMesh = new();
	sourceMesh.AddSurfaceFromArrays(
		Mesh.PrimitiveType.Triangles,
		source.SurfaceGetArrays(0)
	);

	MeshDataTool data = new();
	data.CreateFromSurface(sourceMesh, 0);

	for (int index = 0; index < data.GetVertexCount(); index++)
	{
		Vector3 direction = data.GetVertex(index).Normalized();
		Vector3 sample = direction + offset;

		float broad = noise.GetNoise3D(sample.X, sample.Y, sample.Z);
		float ridges = noise.GetNoise3D(
			sample.X * 2.1f,
			sample.Y * 2.1f,
			sample.Z * 2.1f
		);

		// Stays within the existing conservative placement radius.
		float variation = Mathf.Clamp(
			1.0f + broad * 0.32f
				+ (Mathf.Abs(ridges) - 0.35f) * 0.15f,
			0.65f,
			1.40f
		);

		data.SetVertex(
			index,
			direction * proportions * _radius * variation
		);
	}

	ArrayMesh deformed = new();
	data.CommitToSurface(deformed);
	data.Dispose();

	// Original sphere normals no longer match the deformed rock.
	SurfaceTool surface = new();
	surface.CreateFrom(deformed, 0);
	surface.SetSmoothGroup(0);
	surface.GenerateNormals();

	ArrayMesh result = surface.Commit();
	surface.Dispose();

	Color rockColor = new Color(0.22f, 0.235f, 0.25f).Lerp(
		new Color(0.34f, 0.29f, 0.235f),
		random.Randf()
	);

	result.SurfaceSetMaterial(0, new StandardMaterial3D
	{
		AlbedoColor = rockColor,
		Roughness = 0.95f,
		MetallicSpecular = 0.18f
	});

	return result;
}

	#endregion

	#region Damage Visuals

// =========================================================
// Assigns a seeded crack material while preserving this asteroid's rock colour.
private void ConfigureDamageVisuals(
	MeshInstance3D visual,
	ArrayMesh mesh
)
{
	if (_damageShader == null)
	{
		_damageShader = GD.Load<Shader>(
			"res://ASTEROIDS/AsteroidDamage.gdshader"
		);
	}

	if (_damageShader == null)
	{
		GD.PushError("Could not load ASTEROIDS/AsteroidDamage.gdshader.");
		return;
	}

	Color rockColor = new Color(0.4f, 0.4f, 0.42f);

	if (mesh.SurfaceGetMaterial(0) is StandardMaterial3D original)
	{
		rockColor = original.AlbedoColor;
	}

	RandomNumberGenerator random = new RandomNumberGenerator
	{
		Seed = _shapeSeed ^ 0x85EBCA6BUL
	};

	_damageMaterial = new ShaderMaterial
	{
		Shader = _damageShader
	};

	_damageMaterial.SetShaderParameter("rock_color", rockColor);
	_damageMaterial.SetShaderParameter("asteroid_radius", _radius);

	_damageMaterial.SetShaderParameter(
		"crack_density",
		Mathf.Max(0.1f, CrackDensity)
	);

	_damageMaterial.SetShaderParameter(
		"crack_width",
		Mathf.Max(0.001f, CrackWidth)
	);

	_damageMaterial.SetShaderParameter(
		"pattern_offset",
		new Vector3(
			random.RandfRange(0.0f, 100.0f),
			random.RandfRange(0.0f, 100.0f),
			random.RandfRange(0.0f, 100.0f)
		)
	);

	visual.MaterialOverride = _damageMaterial;
}

	// =========================================================
	// Keeps crack damage current, but only animates hit flashes
	// when the detailed asteroid is within the camera view.
	// =========================================================
	private void UpdateDamageVisuals(DamageInfo damage)
	{
		if (_damageMaterial == null)
		{
			return;
		}

		_damageMaterial.SetShaderParameter(
			"damage_amount",
			Destructible && CracksEnabled
				? 1.0f - HealthFraction
				: 0.0f
		);

		if (!_detailState.InCameraView
			|| _detailState.Tier
				== DistanceDetailManager.DetailTier.Distant
			|| !damage.HasImpact
			|| ImpactFlashDuration <= 0.0f)
		{
			return;
		}

		_damageMaterial.SetShaderParameter(
			"hit_position",
			ToLocal(damage.ImpactPosition)
		);

		_damageMaterial.SetShaderParameter(
			"hit_radius",
			Mathf.Clamp(_radius * 0.08f, 0.65f, 6.0f)
		);

		_damageMaterial.SetShaderParameter(
			"hit_strength",
			1.0f
		);

		_flashRemaining = ImpactFlashDuration;
		SetProcess(true);
	}

// =========================================================
// Fades the local hit flash and disables frame updates when it finishes.
public override void _Process(double delta)
{
	if (_destroyed || _damageMaterial == null)
	{
		SetProcess(false);
		return;
	}

	_flashRemaining = Mathf.Max(
		0.0f,
		_flashRemaining - (float)delta
	);

	float strength = _flashRemaining
		/ Mathf.Max(0.001f, ImpactFlashDuration);

	_damageMaterial.SetShaderParameter("hit_strength", strength);

	if (_flashRemaining <= 0.0f)
	{
		SetProcess(false);
	}
}

#endregion

	#region Distance Detail

	[ExportGroup("Distance Detail")]

	[Export] public bool DistanceOptimizationEnabled = true;

	[Export] public float FullDetailDistance =
		DistanceDetailConfig.NearDistance;

	[Export] public float SimpleDetailDistance =
		DistanceDetailConfig.DistantDistance;

	private MeshInstance3D _visual;
	private DistanceDetailManager _detailManager;

	private DistanceDetailManager.DetailState _detailState =
		new(
			DistanceDetailManager.DetailTier.Near,
			true
		);

			// =========================================================
	// Stops irrelevant rotation and simplifies distant rendering.
	// Collision, damage and deposits continue operating normally.
	// =========================================================
	private void ApplyDistanceDetail(
		DistanceDetailManager.DetailState state
	)
	{
		if (_destroyed || IsQueuedForDeletion())
		{
			return;
		}

		_detailState = state;

		bool rotate =
			state.InCameraView
			&& state.Tier == DistanceDetailManager.DetailTier.Near
			&& _rotationSpeed > 0.0f;

		SetPhysicsProcess(rotate);

		bool distant =
			state.Tier == DistanceDetailManager.DetailTier.Distant;

		// Clearing the override restores the generated mesh's
		// original StandardMaterial3D.
		_visual.MaterialOverride = distant
			? null
			: _damageMaterial;

		_visual.CastShadow = distant
			? GeometryInstance3D.ShadowCastingSetting.Off
			: GeometryInstance3D.ShadowCastingSetting.On;

		if (!state.InCameraView || distant)
		{
			_flashRemaining = 0.0f;

			_damageMaterial?.SetShaderParameter(
				"hit_strength",
				0.0f
			);

			SetProcess(false);
		}
	}

	// =========================================================
	// Releases the registration during destruction or sector unloading.
	// =========================================================
	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_detailManager))
		{
			_detailManager.Unregister(this);
		}
	}

	#endregion
}

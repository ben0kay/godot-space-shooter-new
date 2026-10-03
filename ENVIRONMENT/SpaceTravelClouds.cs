using Godot;

// Maintains sparse ambient cloud wisps around the camera.
// Each recycled patch receives a random colour, size and noise pattern.
public partial class SpaceTravelClouds : Node3D
{
	#region Settings

	[ExportGroup("Cloud Field")]

	[Export] public int PatchCount = 6;
	[Export] public float FieldRadius = 100.0f;
	[Export] public Vector2 PatchSizeRange = new(10.0f, 45.0f);

	// Higher values make large patches less common.
	[Export] public float SizeBias = 2.0f;

	[ExportGroup("Appearance")]

	[Export] public float MaximumOpacity = 0.12f;

	[Export] public Godot.Collections.Array<Color> CloudPalette = new()
	{
		new Color(0.26f, 0.34f, 0.42f), // Slate blue.
		new Color(0.18f, 0.38f, 0.35f), // Teal.
		new Color(0.27f, 0.38f, 0.25f), // Muted green.
		new Color(0.35f, 0.27f, 0.42f), // Violet.
		new Color(0.42f, 0.29f, 0.34f), // Dusty rose.
		new Color(0.43f, 0.35f, 0.24f), // Warm amber.
		new Color(0.35f, 0.38f, 0.42f), // Silver grey.
		new Color(0.23f, 0.29f, 0.43f)  // Deep blue.
	};

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private MultiMesh _multiMesh;

	private Vector3[] _positions;
	private float[] _sizes;
	private Color[] _colors;

	private readonly RandomNumberGenerator _random = new();

	private Camera3D _previousCamera;
	private Vector3 _previousPosition;
	private bool _hasPreviousPosition;

	private float _radius;
	private float _minimumSize;
	private float _maximumSize;

	#endregion

	#region Setup

	// =========================================================
	// Builds one shared mesh and material for the ambient patches.
	// =========================================================
	public override void _Ready()
	{
		SetPhysicsProcess(false);

		_ship = NodeHelpers.FindAncestor<PlayerShip>(this);

		Shader shader = GD.Load<Shader>(
            "res://ENVIRONMENT/SpaceCloud.gdshader"
		);

		if (_ship == null || shader == null)
		{
			GD.PushError(
                "SpaceTravelClouds needs a PlayerShip ancestor "
				+ "and ENVIRONMENT/SpaceCloud.gdshader."
			);

			SetProcess(false);
			return;
		}

		_random.Randomize();

		int count = Mathf.Clamp(PatchCount, 1, 64);
		_radius = Mathf.Max(10.0f, FieldRadius);

		_minimumSize = Mathf.Max(
			0.1f,
			Mathf.Min(PatchSizeRange.X, PatchSizeRange.Y)
		);

		_maximumSize = Mathf.Max(
			_minimumSize,
			Mathf.Max(PatchSizeRange.X, PatchSizeRange.Y)
		);

		_positions = new Vector3[count];
		_sizes = new float[count];
		_colors = new Color[count];

		_multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			UseCustomData = true,
			Mesh = new QuadMesh
			{
				Size = new Vector2(1.6f, 1.0f)
			},
			InstanceCount = count
		};

		float bounds = _radius + _maximumSize * 2.0f;

		AddChild(new MultiMeshInstance3D
		{
			Name = "CloudPatches",
			Multimesh = _multiMesh,
			MaterialOverride = new ShaderMaterial
			{
				Shader = shader
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(
				Vector3.One * -bounds,
				Vector3.One * bounds * 2.0f
			)
		});

		TopLevel = true;

		for (int index = 0; index < count; index++)
		{
			_positions[index] = RandomPosition();
			RandomizeAppearance(index);

			_multiMesh.SetInstanceColor(
				index,
				new Color(0.0f, 0.0f, 0.0f, 0.0f)
			);
		}
	}

	// =========================================================
	// Chooses a new size, palette colour and pattern when a patch spawns.
	// =========================================================
	private void RandomizeAppearance(int index)
	{
		float sizeRoll = Mathf.Pow(
			_random.Randf(),
			Mathf.Max(0.1f, SizeBias)
		);

		_sizes[index] = Mathf.Lerp(
			_minimumSize,
			_maximumSize,
			sizeRoll
		);

		Color color = new(0.35f, 0.38f, 0.42f);

		if (CloudPalette != null && CloudPalette.Count > 0)
		{
			color = CloudPalette[
				_random.RandiRange(0, CloudPalette.Count - 1)
			];
		}

		float brightness = _random.RandfRange(0.75f, 1.1f);

		_colors[index] = new Color(
			color.R * brightness,
			color.G * brightness,
			color.B * brightness,
			color.A * _random.RandfRange(0.6f, 1.0f)
		);

		_multiMesh.SetInstanceCustomData(
			index,
			new Color(_random.Randf(), _random.Randf(), 0.0f, 1.0f)
		);
	}

	#endregion

	#region Cloud Updates

	// =========================================================
	// Preserves world-space positions while recycling distant patches.
	// Cloud visibility is independent of boost and travel speed.
	// =========================================================
	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_ship))
		{
			return;
		}

		Camera3D camera = GetViewport().GetCamera3D();

		if (!GodotObject.IsInstanceValid(camera))
		{
			_hasPreviousPosition = false;
			return;
		}

		Vector3 position = camera.GlobalPosition;

		Vector3 displacement =
			_hasPreviousPosition && camera == _previousCamera
				? position - _previousPosition
				: Vector3.Zero;

		bool reset =
			!_hasPreviousPosition
			|| camera != _previousCamera
			|| displacement.LengthSquared() > _radius * _radius;

		_previousCamera = camera;
		_previousPosition = position;
		_hasPreviousPosition = true;

		GlobalTransform = new Transform3D(Basis.Identity, position);

		Basis billboard = camera.GlobalBasis.Orthonormalized();

		Vector3 direction = displacement.LengthSquared() > 0.000001f
			? displacement.Normalized()
			: Vector3.Zero;

		for (int index = 0; index < _positions.Length; index++)
		{
			Vector3 patch = _positions[index] - displacement;

			if (reset || patch.LengthSquared() > _radius * _radius)
			{
				patch = RandomPosition();
				RandomizeAppearance(index);

				if (!reset && direction != Vector3.Zero)
				{
					patch -= direction * patch.Dot(direction);

					float edge = Mathf.Sqrt(
						Mathf.Max(
							0.0f,
							_radius * _radius - patch.LengthSquared()
						)
					);

					patch += direction * edge * 0.98f;
				}
			}

			_positions[index] = patch;

			float distance = patch.Length();

			// Larger patches fade farther from the camera.
			float fadeStart = Mathf.Max(8.0f, _sizes[index] * 0.8f);
			float fadeLength = Mathf.Max(8.0f, _sizes[index] * 0.6f);

			float nearFade = Mathf.Clamp(
				(distance - fadeStart) / fadeLength,
				0.0f,
				1.0f
			);

			float edgeFade = Mathf.Clamp(
				(_radius - distance) / 20.0f,
				0.0f,
				1.0f
			);

			Color color = _colors[index];
			color.A *= Mathf.Clamp(MaximumOpacity, 0.0f, 1.0f)
				* nearFade * edgeFade;

			_multiMesh.SetInstanceTransform(
				index,
				new Transform3D(
					billboard.Scaled(Vector3.One * _sizes[index]),
					patch
				)
			);

			_multiMesh.SetInstanceColor(index, color);
		}
	}

	// =========================================================
	// Samples a position uniformly inside the local cloud volume.
	// =========================================================
	private Vector3 RandomPosition()
	{
		Vector3 point;

		do
		{
			point = new Vector3(
				_random.RandfRange(-1.0f, 1.0f),
				_random.RandfRange(-1.0f, 1.0f),
				_random.RandfRange(-1.0f, 1.0f)
			);
		}
		while (point.LengthSquared() > 1.0f);

		return point * _radius;
	}

	#endregion
}

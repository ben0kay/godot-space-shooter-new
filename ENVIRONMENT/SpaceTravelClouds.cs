using Godot;

// Maintains a small collection of faint world-space cloud patches
// around the player camera, recycling them outside the local volume.
public partial class SpaceTravelClouds : Node3D
{
	#region Settings

	[ExportGroup("Cloud Field")]

	[Export] public int PatchCount = 16;
	[Export] public float FieldRadius = 35.0f;
	[Export] public Vector2 PatchSizeRange = new(3.0f, 7.0f);

	[ExportGroup("Appearance")]

	[Export] public Color CloudColor = new(0.32f, 0.43f, 0.5f);
	[Export] public float MaximumOpacity = 0.1f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private MultiMesh _multiMesh;

	private Vector3[] _positions;
	private float[] _sizes;

	private readonly RandomNumberGenerator _random = new();

	private Camera3D _previousCamera;
	private Vector3 _previousPosition;
	private bool _hasPreviousPosition;

	private float _radius;

	#endregion

	#region Setup

	// =========================================================
	// Builds a bounded collection of patches using one shared material.
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
				"SpaceTravelClouds needs a PlayerShip parent "
				+ "and ENVIRONMENT/SpaceCloud.gdshader."
			);

			SetProcess(false);
			return;
		}

		_random.Randomize();

		int count = Mathf.Clamp(PatchCount, 1, 64);
		_radius = Mathf.Max(10.0f, FieldRadius);

		float minimumSize = Mathf.Max(
			0.1f,
			Mathf.Min(PatchSizeRange.X, PatchSizeRange.Y)
		);

		float maximumSize = Mathf.Max(
			minimumSize,
			Mathf.Max(PatchSizeRange.X, PatchSizeRange.Y)
		);

		_positions = new Vector3[count];
		_sizes = new float[count];

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

		float bounds = _radius + maximumSize * 2.0f;

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
			_sizes[index] = _random.RandfRange(minimumSize, maximumSize);

			_multiMesh.SetInstanceCustomData(
				index,
				new Color(_random.Randf(), _random.Randf(), 0.0f, 1.0f)
			);

			_multiMesh.SetInstanceColor(
				index,
				new Color(0.0f, 0.0f, 0.0f, 0.0f)
			);
		}
	}

	#endregion

	#region Cloud Updates

	// =========================================================
	// Keeps patches stationary in world space while the camera travels.
	// Resets the local field after camera switches or sector jumps.
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
			Vector3 patch = reset
				? RandomPosition()
				: _positions[index] - displacement;

			if (patch.LengthSquared() > _radius * _radius)
			{
				patch = RandomPosition();

				if (direction != Vector3.Zero)
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

			// Fade before a patch can fill the camera view.
			float nearFade = Mathf.Clamp(
				(distance - 5.0f) / 7.0f,
				0.0f,
				1.0f
			);

			float edgeFade = Mathf.Clamp(
				(_radius - distance) / 8.0f,
				0.0f,
				1.0f
			);

			Color color = CloudColor;
			color.A = Mathf.Clamp(MaximumOpacity, 0.0f, 1.0f)
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
	// Samples a position within the bounded cloud volume.
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

using Godot;

// Maintains a small visual dust field around the active player camera.
// Dust stays in world space while the camera moves through it.
public partial class SpaceTravelDust : Node3D
{
	#region Settings

	[ExportGroup("Dust Field")]

	[Export] public int SpeckCount = 160;
	[Export] public float FieldRadius = 24.0f;
	[Export] public float SpeckSize = 0.035f;

	[ExportGroup("Visibility")]

	[Export] public Color DustColor =
		new Color(0.7f, 0.82f, 0.9f);

	[Export] public float MaximumOpacity = 0.35f;
	[Export] public float FullVisibilitySpeed = 12.0f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private MultiMesh _multiMesh;
	private MultiMeshInstance3D _visual;

	private readonly RandomNumberGenerator _random = new();

	private Vector3[] _positions;
	private Vector3 _previousCameraPosition;
	private Camera3D _previousCamera;
	private bool _hasPreviousPosition;

	#endregion

	#region Setup

	// =========================================================
	// Creates one shared mesh for the complete nearby dust field.
	// =========================================================
	public override void _Ready()
	{
		_ship = NodeHelpers.FindAncestor<PlayerShip>(this);

		if (_ship == null)
		{
			GD.PushError(
				"SpaceTravelDust must be a child of PlayerShip."
			);

			SetProcess(false);
			return;
		}

		Shader shader = GD.Load<Shader>(
			"res://ENVIRONMENT/SpaceDust.gdshader"
		);

		if (shader == null)
		{
			GD.PushError("Could not load SpaceDust.gdshader.");
			SetProcess(false);
			return;
		}

		_random.Randomize();

		int count = Mathf.Max(1, SpeckCount);
		_positions = new Vector3[count];

		_multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = new QuadMesh
			{
				Size = Vector2.One
			},
			InstanceCount = count
		};

		float radius = Mathf.Max(1.0f, FieldRadius);

		_visual = new MultiMeshInstance3D
		{
			Name = "DustSpecks",
			Multimesh = _multiMesh,
			MaterialOverride = new ShaderMaterial
			{
				Shader = shader
			},
			CastShadow =
				GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(
				Vector3.One * -(radius + 1.0f),
				Vector3.One * (radius + 1.0f) * 2.0f
			)
		};

		AddChild(_visual);

		// Dust uses world-oriented coordinates rather than ship rotation.
		TopLevel = true;

		for (int index = 0; index < count; index++)
		{
			_positions[index] = RandomPosition(radius);
		}
	}

	#endregion

	#region Dust Update

	// =========================================================
	// Moves the camera through stationary dust and recycles specks
	// entering the field from its leading edge.
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

		Vector3 cameraPosition = camera.GlobalPosition;
		float radius = Mathf.Max(1.0f, FieldRadius);

		Vector3 displacement = Vector3.Zero;

		if (_hasPreviousPosition && camera == _previousCamera)
		{
			displacement =
				cameraPosition - _previousCameraPosition;
		}

		// Reset after camera switches or unusually large position jumps.
		bool reset =
			!_hasPreviousPosition
			|| camera != _previousCamera
			|| displacement.LengthSquared() > radius * radius;

		_previousCamera = camera;
		_previousCameraPosition = cameraPosition;
		_hasPreviousPosition = true;

		GlobalTransform = new Transform3D(
			Basis.Identity,
			cameraPosition
		);

		float speed = _ship.Velocity.Length();

		float opacity = Mathf.Clamp(MaximumOpacity, 0.0f, 1.0f)
			* Mathf.Clamp(
				speed / Mathf.Max(0.01f, FullVisibilitySpeed),
				0.0f,
				1.0f
			);

		_visual.Visible = opacity > 0.001f;

		Basis billboard = camera.GlobalBasis.Orthonormalized();

		billboard = billboard.Scaled(
			Vector3.One * Mathf.Max(0.001f, SpeckSize)
		);

		Vector3 travelDirection =
			displacement.LengthSquared() > 0.000001f
				? displacement.Normalized()
				: Vector3.Zero;

		for (int index = 0; index < _positions.Length; index++)
		{
			Vector3 position = reset
				? RandomPosition(radius)
				: _positions[index] - displacement;

			if (position.LengthSquared() > radius * radius)
			{
				position = RandomPosition(radius);

				// Recycled dust enters ahead of camera travel.
				if (travelDirection != Vector3.Zero)
				{
					position -= travelDirection
						* position.Dot(travelDirection);

					float edge = Mathf.Sqrt(
						Mathf.Max(
							0.0f,
							radius * radius
								- position.LengthSquared()
						)
					);

					position += travelDirection * edge * 0.98f;
				}
			}

			_positions[index] = position;

			float distance = position.Length();

			// Hide nearby specks and fade the outer edge of the field.
			float nearFade = Mathf.Clamp(
				(distance - 1.5f) / 2.0f,
				0.0f,
				1.0f
			);

			float edgeFade = Mathf.Clamp(
				(radius - distance) / 4.0f,
				0.0f,
				1.0f
			);

			Color color = DustColor;
			color.A = opacity * nearFade * edgeFade;

			_multiMesh.SetInstanceTransform(
				index,
				new Transform3D(billboard, position)
			);

			_multiMesh.SetInstanceColor(index, color);
		}
	}

	// =========================================================
	// Samples a position uniformly inside the dust sphere.
	// =========================================================
	private Vector3 RandomPosition(float radius)
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

		return point * radius;
	}

	#endregion
}

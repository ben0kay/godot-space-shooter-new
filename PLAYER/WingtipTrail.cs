using System.Collections.Generic;
using Godot;

// Leaves a short, fading world-space ribbon behind a moving ship's wingtip.
public partial class WingtipTrail : Node3D
{
	#region Settings

	[Export] public float MinimumSpeed = 5.0f;
	[Export] public float FullOpacitySpeed = 18.0f;

	[Export] public float Lifetime = 0.35f;
	[Export] public float SampleInterval = 0.025f;
	[Export] public int MaximumPoints = 32;

	[Export] public float Width = 0.045f;
	[Export] public float Opacity = 0.22f;
	[Export] public float BoostOpacityMultiplier = 1.4f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private ImmediateMesh _mesh;
	private MeshInstance3D _visual;

	private readonly List<TrailPoint> _points = new();
	private float _sampleTimer;

	// Stores each sampled position and its fading strength independently.
	private struct TrailPoint
	{
		public Vector3 Position;
		public float Age;
		public float Strength;
	}

	#endregion

	#region Setup

	// Finds the owning ship and creates a ribbon mesh using world coordinates.
	public override void _Ready()
	{
		Node ancestor = GetParent();

		while (ancestor != null && ancestor is not PlayerShip)
		{
			ancestor = ancestor.GetParent();
		}

		_ship = ancestor as PlayerShip;

		if (_ship == null)
		{
			GD.PushError("WingtipTrail needs a PlayerShip ancestor.");
			SetPhysicsProcess(false);
			SetProcess(false);
			return;
		}

		_mesh = new ImmediateMesh();

		StandardMaterial3D material = new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			VertexColorUseAsAlbedo = true,
			AlbedoColor = Colors.White,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			NoDepthTest = false
		};

		_visual = new MeshInstance3D
		{
			Name = "Ribbon",
			Mesh = _mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};

		AddChild(_visual);

		// Old trail positions must remain stationary as the ship moves.
		_visual.TopLevel = true;
		_visual.GlobalTransform = Transform3D.Identity;
	}

	#endregion

	#region Sampling

	// Ages existing points and records new wingtip positions while moving.
	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(_ship))
		{
			return;
		}

		float seconds = (float)delta;
		float lifetime = Mathf.Max(0.02f, Lifetime);

		for (int index = _points.Count - 1; index >= 0; index--)
		{
			TrailPoint point = _points[index];
			point.Age += seconds;

			if (point.Age >= lifetime)
			{
				_points.RemoveAt(index);
			}
			else
			{
				_points[index] = point;
			}
		}

		_sampleTimer -= seconds;

		if (_sampleTimer > 0.0f)
		{
			return;
		}

		_sampleTimer = Mathf.Max(0.01f, SampleInterval);

		float speed = _ship.Velocity.Length();

		if (speed <= MinimumSpeed)
		{
			return;
		}

		float strength = Mathf.Clamp(
			(speed - MinimumSpeed)
			/ Mathf.Max(0.01f, FullOpacitySpeed - MinimumSpeed),
			0.0f,
			1.0f
		);

		strength *= Mathf.Lerp(
			1.0f,
			BoostOpacityMultiplier,
			_ship.BoostAmount
		);

		_points.Insert(0, new TrailPoint
		{
			Position = GlobalPosition,
			Age = 0.0f,
			Strength = strength
		});

		int limit = Mathf.Max(2, MaximumPoints);

		if (_points.Count > limit)
		{
			_points.RemoveRange(limit, _points.Count - limit);
		}
	}

	#endregion

	#region Rendering

	// Rebuilds a thin ribbon facing the current camera.
	public override void _Process(double delta)
	{
		_mesh.ClearSurfaces();

		Camera3D camera = GetViewport().GetCamera3D();

		if (camera == null || _points.Count < 2)
		{
			return;
		}

		_mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

		for (int index = 0; index < _points.Count - 1; index++)
		{
			TrailPoint front = _points[index];
			TrailPoint rear = _points[index + 1];

			Vector3 direction = rear.Position - front.Position;

			if (direction.LengthSquared() < 0.000001f)
			{
				continue;
			}

			Vector3 midpoint = (front.Position + rear.Position) * 0.5f;
			Vector3 towardCamera = camera.GlobalPosition - midpoint;
			Vector3 side = direction.Normalized().Cross(towardCamera);

			if (side.LengthSquared() < 0.000001f)
			{
				side = camera.GlobalBasis.X;
			}

			side = side.Normalized();

			float frontFade = GetFade(front);
			float rearFade = GetFade(rear);

			Vector3 frontWidth = side * Width * 0.5f * frontFade;
			Vector3 rearWidth = side * Width * 0.5f * rearFade;

			Color frontColor = new(
				0.9f, 0.95f, 1.0f,
				Mathf.Clamp(Opacity * front.Strength * frontFade, 0.0f, 1.0f)
			);

			Color rearColor = new(
				0.9f, 0.95f, 1.0f,
				Mathf.Clamp(Opacity * rear.Strength * rearFade, 0.0f, 1.0f)
			);

			AddVertex(front.Position - frontWidth, frontColor);
			AddVertex(front.Position + frontWidth, frontColor);
			AddVertex(rear.Position + rearWidth, rearColor);

			AddVertex(front.Position - frontWidth, frontColor);
			AddVertex(rear.Position + rearWidth, rearColor);
			AddVertex(rear.Position - rearWidth, rearColor);
		}

		_mesh.SurfaceEnd();
	}

	// Fades and narrows the ribbon toward its older end.
	private float GetFade(TrailPoint point)
	{
		float remaining = Mathf.Clamp(
			1.0f - point.Age / Mathf.Max(0.02f, Lifetime),
			0.0f,
			1.0f
		);

		return remaining * remaining;
	}

	// Adds one ribbon vertex with its individual fading colour.
	private void AddVertex(Vector3 position, Color color)
	{
		_mesh.SurfaceSetColor(color);
		_mesh.SurfaceAddVertex(position);
	}

	#endregion
}

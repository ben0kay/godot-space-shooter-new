using Godot;

// Positions a procedural sun flare and periodically checks whether solid geometry blocks it.
public partial class SunLensFlare : CanvasLayer
{
	#region Settings

	[Export(PropertyHint.Range, "0,2,0.05")]
	public float Strength = 0.65f;

	[Export] public float OcclusionInterval = 0.1f;
	[Export] public float OcclusionDistance = 15000.0f;

	#endregion

	#region Runtime

	private SpaceEnvironment _environment;
	private ShaderMaterial _material;
	private ColorRect _surface;

	private Camera3D _lastCamera;
	private float _probeRemaining;
	private float _visibility;
	private bool _blocked = true;

	#endregion

	#region Setup

	// =========================================================
	// Creates the flare below the HUD and waits for the environment to initialize.
	public override void _Ready()
	{
		Layer = 4;
		_environment = GetParent() as SpaceEnvironment;

		Shader shader = GD.Load<Shader>(
			"res://ENVIRONMENT/SunLensFlare.gdshader"
		);

		if (_environment == null || shader == null)
		{
			GD.PushError("SunLensFlare requires a SpaceEnvironment parent and shader.");
			SetProcess(false);
			return;
		}

		_material = new ShaderMaterial { Shader = shader };

		_surface = new ColorRect
		{
			Name = "FlareSurface",
			Color = Colors.White,
			Material = _material,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		AddChild(_surface);
		_surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
	}

	#endregion

	#region Updates

	// =========================================================
	// Tracks sunlight on screen and fades glare when hidden or outside the viewport.
	public override void _Process(double delta)
	{
		Camera3D camera = GetViewport().GetCamera3D();

		if (!GodotObject.IsInstanceValid(camera))
		{
			_surface.Visible = false;
			return;
		}

		_surface.Visible = true;

		Vector3 direction = _environment.SunDirection;
		direction = direction.LengthSquared() > 0.0001f
			? direction.Normalized()
			: Vector3.Up;

		Vector2 size = _surface.Size;
		if (size.X <= 0 || size.Y <= 0) return;

		Vector3 projectedPoint = camera.GlobalPosition + direction * 100.0f;
		bool inFront = !camera.IsPositionBehind(projectedPoint);

		Vector2 position = inFront
			? camera.UnprojectPosition(projectedPoint) / size
			: new Vector2(-10, -10);

		bool onScreen = inFront
			&& position.X >= 0 && position.X <= 1
			&& position.Y >= 0 && position.Y <= 1;

		if (_lastCamera != camera)
		{
			_lastCamera = camera;
			_probeRemaining = 0;
			_blocked = true;
		}

		_probeRemaining -= (float)delta;

		if (onScreen && _probeRemaining <= 0)
		{
			_probeRemaining = Mathf.Max(0.05f, OcclusionInterval);
			_blocked = IsBlocked(camera, direction);
		}
		else if (!onScreen)
		{
			// Recheck immediately when the sun returns to view.
			_probeRemaining = 0;
		}

		float edgeDistance = Mathf.Min(
			Mathf.Min(position.X, 1.0f - position.X),
			Mathf.Min(position.Y, 1.0f - position.Y)
		);

		float edgeFade = Mathf.Clamp(edgeDistance / 0.08f, 0, 1);
		float target = onScreen && !_blocked
			? edgeFade * Mathf.Clamp(Strength, 0, 2)
			: 0;

		_visibility = Mathf.Lerp(
			_visibility, target,
			1.0f - Mathf.Exp(-12.0f * (float)delta)
		);

		_material.SetShaderParameter("sun_position", position);
		_material.SetShaderParameter("aspect_ratio", size.X / size.Y);
		_material.SetShaderParameter("visibility", _visibility);
		_material.SetShaderParameter("flare_color", _environment.SunColor);
	}

	// =========================================================
	// Checks the sky planet and solid bodies while excluding the player's collider.
	private bool IsBlocked(Camera3D camera, Vector3 direction)
	{
		WorldEnvironment world =
			_environment.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");

		if (world?.Environment?.Sky?.SkyMaterial is ShaderMaterial sky)
		{
			bool planetEnabled = sky.GetShaderParameter("planet_enabled").AsBool();

			if (planetEnabled)
			{
				Vector3 planetDirection =
					sky.GetShaderParameter("planet_direction").AsVector3();

				float planetRadius =
					sky.GetShaderParameter("planet_radius").AsSingle();

				if (planetDirection.LengthSquared() > 0.0001f)
				{
					float angularRadius = Mathf.Asin(
						Mathf.Clamp(planetRadius / 3.0f, 0, 0.99f)
					);

					if (direction.Dot(planetDirection.Normalized())
						> Mathf.Cos(angularRadius))
					{
						return true;
					}
				}
			}
		}

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(
				camera.GlobalPosition,
				camera.GlobalPosition
					+ direction * Mathf.Max(100.0f, OcclusionDistance)
			);

		query.CollideWithAreas = false;

		if (GetTree().GetFirstNodeInGroup("player_ship")
			is CollisionObject3D player)
		{
			query.Exclude = new Godot.Collections.Array<Rid>
			{
				player.GetRid()
			};
		}

		return camera.GetWorld3D().DirectSpaceState
			.IntersectRay(query).Count > 0;
	}

	#endregion
}
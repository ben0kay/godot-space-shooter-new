using System.Collections.Generic;
using Godot;

public partial class PlayerThrusters : Node3D
{
	#region Settings

	[Export] public float PlumeLength = 3.0f;
	[Export] public float CoreWidth = 0.18f;
	[Export] public float GlowWidth = 0.48f;
	[Export] public float IdleLength = 0.25f;
	[Export] public float FullThrustLength = 1.25f;
	[Export] public float ThrustResponse = 5.0f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private readonly List<MeshInstance3D> _plumes = new();
	private float _thrust;

	#endregion

	#region Shader

	private const string PlumeShader = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled;

uniform vec4 root_color : source_color;
uniform vec4 tip_color : source_color;
uniform float brightness = 1.0;
uniform float thrust = 0.0;

void fragment() {
	float sideways = abs(UV.x * 2.0 - 1.0);
	float soft_edge = pow(max(0.0, 1.0 - sideways), 1.7);
	float fade = pow(max(0.0, 1.0 - UV.y), 1.5);

	// Bright bands travel from the nozzle toward the tip.
	float travel = UV.y * 18.0 - TIME * (8.0 + thrust * 14.0);
	float streaks = 0.68 + 0.32 * sin(travel);

	float flicker = 0.90
		+ 0.10 * sin(TIME * 23.0 + UV.y * 11.0);

	float motion = mix(0.85, streaks, 0.35 + thrust * 0.55);

	vec4 color = mix(root_color, tip_color, UV.y);
	float alpha = color.a * soft_edge * fade * motion * flicker;

	ALBEDO = color.rgb;
	EMISSION = color.rgb * brightness * alpha;
	ALPHA = alpha;
}
";

	#endregion

	#region Godot Events

	// Builds a layered exhaust effect on each existing engine marker.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		if (_ship == null)
		{
			GD.PushError("PlayerThrusters must be a direct child of PlayerShip.");
			SetPhysicsProcess(false);
			return;
		}

		CreateThruster(GetNode<Marker3D>("LeftEngine"));
		CreateThruster(GetNode<Marker3D>("RightEngine"));

		UpdatePlumes();
	}

	// Stretches the exhaust according to forward movement.
	public override void _PhysicsProcess(double delta)
	{
		float forwardSpeed = Mathf.Max(
			0.0f,
			_ship.Velocity.Dot(-_ship.GlobalBasis.Z)
		);

		float targetThrust = Mathf.Clamp(
			forwardSpeed / Mathf.Max(_ship.ForwardSpeed, 0.01f),
			0.0f,
			1.0f
		);

		_thrust = Mathf.MoveToward(
			_thrust,
			targetThrust,
			ThrustResponse * (float)delta
		);

		UpdatePlumes();
	}

	#endregion

	#region Plumes

	// Applies current length and motion strength to both engine effects.
	private void UpdatePlumes()
	{
		float length = Mathf.Lerp(
			IdleLength,
			FullThrustLength,
			_thrust
		);

		foreach (MeshInstance3D plume in _plumes)
		{
			plume.Scale = new Vector3(1.0f, 1.0f, length);

			ShaderMaterial material = plume.MaterialOverride as ShaderMaterial;
			material?.SetShaderParameter("thrust", _thrust);
		}
	}

	// Adds a broad blue glow and a narrow white-blue centre.
	private void CreateThruster(Marker3D marker)
	{
		AddPlume(
			marker,
			"OuterGlow",
			GlowWidth,
			new Color(0.10f, 0.55f, 1.0f, 0.50f),
			new Color(0.27f, 0.12f, 0.95f, 0.30f),
			2.0f
		);

		AddPlume(
			marker,
			"BrightCore",
			CoreWidth,
			new Color(0.75f, 0.95f, 1.0f, 0.90f),
			new Color(0.12f, 0.50f, 1.0f, 0.20f),
			3.0f
		);
	}

	// Creates one soft plume layer and remembers it for thrust updates.
	private void AddPlume(
		Marker3D marker,
		string name,
		float width,
		Color rootColor,
		Color tipColor,
		float brightness
	)
	{
		Shader shader = new Shader();
		shader.Code = PlumeShader;

		ShaderMaterial material = new ShaderMaterial();
		material.Shader = shader;
		material.SetShaderParameter("root_color", rootColor);
		material.SetShaderParameter("tip_color", tipColor);
		material.SetShaderParameter("brightness", brightness);

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = name;
		visual.Mesh = CreateCrossedPlanes(width, PlumeLength);
		visual.MaterialOverride = material;

		marker.AddChild(visual);
		_plumes.Add(visual);
	}

	// Builds three intersecting planes extending behind the ship.
	private ArrayMesh CreateCrossedPlanes(float width, float length)
	{
		SurfaceTool surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		for (int plane = 0; plane < 3; plane++)
		{
			float angle = plane * Mathf.Pi / 3.0f;

			Vector3 across = new Vector3(
				Mathf.Cos(angle),
				Mathf.Sin(angle),
				0.0f
			) * width;

			Vector3 rootLeft = -across;
			Vector3 rootRight = across;
			Vector3 tipLeft = -across + Vector3.Back * length;
			Vector3 tipRight = across + Vector3.Back * length;

			AddVertex(surface, rootLeft, new Vector2(0, 0));
			AddVertex(surface, rootRight, new Vector2(1, 0));
			AddVertex(surface, tipRight, new Vector2(1, 1));

			AddVertex(surface, rootLeft, new Vector2(0, 0));
			AddVertex(surface, tipRight, new Vector2(1, 1));
			AddVertex(surface, tipLeft, new Vector2(0, 1));
		}

		return surface.Commit();
	}

	// Supplies one position and its fade coordinates to the plume mesh.
	private void AddVertex(SurfaceTool surface, Vector3 position, Vector2 uv)
	{
		surface.SetUV(uv);
		surface.AddVertex(position);
	}

	#endregion
}

using System.Collections.Generic;
using Godot;

// Builds animated engine plumes and scales their appearance with movement and boost.
public partial class PlayerThrusters : Node3D
{
#region Settings

[Export] public float PlumeLength = 3.0f;
[Export] public float CoreWidth = 0.18f;
[Export] public float GlowWidth = 0.48f;
[Export] public float IdleLength = 0.25f;
[Export] public float FullThrustLength = 1.25f;
[Export] public float ThrustResponse = 5.0f;

[Export] public float BoostLengthMultiplier = 1.8f;
[Export] public float BoostWidthMultiplier = 1.45f;
[Export] public float BoostBrightnessMultiplier = 1.3f;

#endregion

	#region Runtime

	private PlayerShip _ship;
	private readonly List<MeshInstance3D> _plumes = new();
	private float _thrust;

	#endregion

	#region Shader

private const string PlumeShader = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, fog_disabled;

uniform vec4 root_color : source_color;
uniform vec4 tip_color : source_color;
uniform float brightness = 1.0;
uniform float thrust = 0.0;

// =========================================================
// Draws a soft tapered exhaust with a bright root and faint moving variation.
void fragment()
{
	float along = clamp(UV.y, 0.0, 1.0);
	float sideways = abs(UV.x * 2.0 - 1.0);

	float taper = mix(1.0, 0.18, along);
	float profile = sideways / max(taper, 0.01);
	float soft_edge = exp(-profile * profile * 4.5);

	// Ensure every plane edge disappears completely.
	soft_edge *= 1.0 - smoothstep(0.65, 1.0, sideways);

	float tail = pow(1.0 - along, 2.2);
	float root = exp(-along * 12.0);

	float ripple = 0.95 + 0.05 * sin(
		along * 20.0 - TIME * (10.0 + thrust * 12.0)
	);

	vec4 tint = mix(
		root_color, tip_color,
		smoothstep(0.0, 0.65, along)
	);

	float alpha = tint.a * soft_edge * tail * ripple;

	ALBEDO = vec3(0.0);
	EMISSION = tint.rgb * brightness * (1.0 + root * 0.3);
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

	// Measures forward movement while the ship supplies its smoothed boost amount.
	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(_ship))
		{
			return;
		}

		float forwardSpeed = Mathf.Max(
			0.0f,
			_ship.Velocity.Dot(-_ship.GlobalBasis.Z.Normalized())
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

// =========================================================
// Preserves movement and boost scaling with softer exhaust brightness.
private void UpdatePlumes()
{
	float boost = _ship.BoostAmount;

	float length = Mathf.Lerp(
		IdleLength, FullThrustLength, _thrust
	) * Mathf.Lerp(1.0f, BoostLengthMultiplier, boost);

	float width = Mathf.Lerp(
		1.0f, BoostWidthMultiplier, boost
	);

	float brightnessScale = Mathf.Lerp(
		1.0f, BoostBrightnessMultiplier, boost
	);

	foreach (MeshInstance3D plume in _plumes)
	{
		plume.Scale = new Vector3(width, width, length);

		if (plume.MaterialOverride is not ShaderMaterial material)
			continue;

		float baseBrightness =
			plume.Name.ToString() == "BrightCore" ? 2.0f : 1.0f;

		material.SetShaderParameter("thrust", Mathf.Max(_thrust, boost));
		material.SetShaderParameter(
			"brightness", baseBrightness * brightnessScale
		);
	}
}

	/// =========================================================
// Adds a soft cyan envelope around a pale white-cyan exhaust core.
private void CreateThruster(Marker3D marker)
{
	AddPlume(
		marker, "OuterGlow", GlowWidth,
		new Color(0.30f, 0.85f, 1.0f, 0.22f),
		new Color(0.12f, 0.65f, 0.85f, 0.04f),
		1.0f
	);

	AddPlume(
		marker, "BrightCore", CoreWidth,
		new Color(0.85f, 1.0f, 1.0f, 0.65f),
		new Color(0.35f, 0.85f, 1.0f, 0.06f),
		2.0f
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

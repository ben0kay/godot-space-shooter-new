using Godot;

public partial class PlayerThrusters : Node3D
{
	#region Settings

	[Export] public float PlumeLength = 3.0f;
	[Export] public float CoreWidth = 0.18f;
	[Export] public float GlowWidth = 0.48f;

	#endregion

	#region Shader

	private const string PlumeShader = """
        shader_type spatial;
        render_mode unshaded, blend_add, depth_draw_never, cull_disabled;

        uniform vec4 root_color : source_color;
        uniform vec4 tip_color : source_color;
        uniform float brightness = 1.0;

        void fragment() {
            float sideways = abs(UV.x * 2.0 - 1.0);
            float soft_edge = pow(max(0.0, 1.0 - sideways), 1.7);
            float fade = pow(max(0.0, 1.0 - UV.y), 1.5);

            float shimmer = 0.94
                + 0.06 * sin(TIME * 18.0 + UV.y * 23.0);

            vec4 color = mix(root_color, tip_color, UV.y);
            float alpha = color.a * soft_edge * fade * shimmer;

            ALBEDO = color.rgb;
            EMISSION = color.rgb * brightness * alpha;
            ALPHA = alpha;
        }
		""";

	#endregion

	#region Setup

	// Builds the same layered exhaust effect at both placed engine markers.
	public override void _Ready()
	{
		CreateThruster(GetNode<Marker3D>("LeftEngine"));
		CreateThruster(GetNode<Marker3D>("RightEngine"));
	}

	#endregion

	#region Plumes

	// Adds a broad coloured glow and a narrow bright centre.
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

	// Creates crossed soft planes so the plume is visible from different angles.
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
	}

	// Builds three intersecting planes extending behind the ship along local +Z.
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

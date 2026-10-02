using Godot;

// Builds two curved cosmetic flow ribbons and attaches them to the
// player's banking visual pivot. Boost and dash control their intensity.
public partial class PlayerBoostFlow : Node3D
{
	#region Shape Settings

	[ExportGroup("Ribbon Shape")]

	[Export] public float NoseZ = -2.6f;
	[Export] public float TailZ = 3.4f;

	[Export] public float NoseSide = 0.22f;
	[Export] public float WingSide = 2.7f;
	[Export] public float HeightAboveHull = 0.4f;

	[Export] public float RibbonWidth = 0.22f;
	[Export] public int Segments = 24;

	#endregion

	#region Appearance Settings

	[ExportGroup("Appearance")]

	[Export] public Color FlowColor = new(0.45f, 0.9f, 1.0f);

	[Export] public float BoostOpacity = 0.35f;
	[Export] public float DashOpacity = 0.65f;

	[Export] public float FadeResponse = 12.0f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private ShaderMaterial _material;

	private MeshInstance3D _left;
	private MeshInstance3D _right;

	private float _opacity;
	private float _dashAmount;

	#endregion

	#region Setup

	// =========================================================
	// Waits until PlayerFlightVisuals has created its cosmetic pivot.
	// =========================================================
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		_ship = NodeHelpers.FindAncestor<PlayerShip>(this);

		CallDeferred(nameof(InitializeFlow));
	}

	// =========================================================
	// Creates the ribbons and reparents this effect under the visual pivot.
	// =========================================================
	private void InitializeFlow()
	{
		if (IsQueuedForDeletion())
		{
			return;
		}

		if (!GodotObject.IsInstanceValid(_ship))
		{
			GD.PushError("PlayerBoostFlow must start as a child of PlayerShip.");
			return;
		}

		Node3D pivot = _ship.GetNodeOrNull<Node3D>("FlightVisualPivot");

		Shader shader = GD.Load<Shader>(
			"res://PLAYER/Effects/BoostFlow.gdshader"
		);

		if (pivot == null || shader == null)
		{
			GD.PushError(
				"PlayerBoostFlow needs FlightVisualPivot "
				+ "and PLAYER/Effects/BoostFlow.gdshader."
			);

			return;
		}

		Reparent(pivot, false);
		Transform = Transform3D.Identity;

		_material = new ShaderMaterial
		{
			Shader = shader
		};

		_material.SetShaderParameter("flow_color", FlowColor);
		_material.SetShaderParameter("base_width", Mathf.Max(0.01f, RibbonWidth));
		_material.SetShaderParameter("nose_z", NoseZ);

		_left = BuildRibbon("LeftFlow", -1.0f);
		_right = BuildRibbon("RightFlow", 1.0f);

		SetProcess(true);
	}

	// =========================================================
	// Builds a curved strip with length and width coordinates for the shader.
	// =========================================================
	private MeshInstance3D BuildRibbon(string ribbonName, float side)
	{
		int segments = Mathf.Clamp(Segments, 4, 64);
		int vertexCount = (segments + 1) * 2;

		Vector3[] vertices = new Vector3[vertexCount];
		Vector3[] normals = new Vector3[vertexCount];
		Vector2[] uv = new Vector2[vertexCount];
		int[] indices = new int[segments * 6];

		float width = Mathf.Max(0.01f, RibbonWidth);

		// Tilt the strip across its width so chase and cockpit views
		// can see it without requiring camera-facing mesh rebuilding.
		Vector3 widthDirection =
			new Vector3(side * 0.65f, 0.76f, 0.0f).Normalized();

		for (int index = 0; index <= segments; index++)
		{
			float progress = (float)index / segments;

			float spread =
				1.0f - Mathf.Pow(1.0f - progress, 2.0f);

			Vector3 centre = new(
				side * Mathf.Lerp(NoseSide, WingSide, spread),
				HeightAboveHull + Mathf.Sin(progress * Mathf.Pi) * 0.18f,
				Mathf.Lerp(NoseZ, TailZ, progress)
			);

			int first = index * 2;

			vertices[first] = centre - widthDirection * width * 0.5f;
			vertices[first + 1] = centre + widthDirection * width * 0.5f;

			// The unshaded vertex shader uses this as a widening direction.
			normals[first] = widthDirection;
			normals[first + 1] = widthDirection;

			uv[first] = new Vector2(progress, 0.0f);
			uv[first + 1] = new Vector2(progress, 1.0f);

			if (index == segments)
			{
				continue;
			}

			int triangle = index * 6;

			indices[triangle] = first;
			indices[triangle + 1] = first + 1;
			indices[triangle + 2] = first + 2;

			indices[triangle + 3] = first + 1;
			indices[triangle + 4] = first + 3;
			indices[triangle + 5] = first + 2;
		}

		Godot.Collections.Array arrays = new();
		arrays.Resize((int)Mesh.ArrayType.Max);

		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uv;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		ArrayMesh mesh = new();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		MeshInstance3D visual = new()
		{
			Name = ribbonName,
			Mesh = mesh,
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			ExtraCullMargin = 12.0f,
			Visible = false
		};

		AddChild(visual);
		return visual;
	}

	#endregion

	#region Flow Updates

	// =========================================================
	// Smooths boost intensity and adds a stronger, longer dash burst.
	// =========================================================
	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_ship))
		{
			return;
		}

		float seconds = (float)delta;

		bool alive = _ship.IsCombatTargetable;
		bool dashing = alive && _ship.IsDashing;

		float targetOpacity = !alive
			? 0.0f
			: dashing
				? DashOpacity
				: _ship.BoostAmount * BoostOpacity;

		float blend = 1.0f - Mathf.Exp(
			-Mathf.Max(0.0f, FadeResponse) * seconds
		);

		_opacity = Mathf.Lerp(
			_opacity,
			Mathf.Clamp(targetOpacity, 0.0f, 1.0f),
			blend
		);

		_dashAmount = Mathf.Lerp(
			_dashAmount,
			dashing ? 1.0f : 0.0f,
			blend
		);

		bool visible = _opacity > 0.002f;

		_left.Visible = visible;
		_right.Visible = visible;

		if (!visible)
		{
			return;
		}

		_material.SetShaderParameter("opacity", _opacity);

		_material.SetShaderParameter(
			"flow_speed",
			Mathf.Lerp(3.0f, 7.0f, _dashAmount)
		);

		_material.SetShaderParameter(
			"width_scale",
			Mathf.Lerp(1.0f, 1.7f, _dashAmount)
		);

		_material.SetShaderParameter(
			"length_scale",
			Mathf.Lerp(1.0f, 1.45f, _dashAmount)
		);
	}

	#endregion
}

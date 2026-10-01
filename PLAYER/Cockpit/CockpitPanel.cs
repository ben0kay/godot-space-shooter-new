using Godot;

// Displays Godot UI on a physical cockpit screen and forwards mouse input.
public partial class CockpitPanel : Node3D
{
	#region Settings

	public Vector2 ScreenSize = new(0.56f, 0.30f);
	public Vector2I Resolution = new(672, 360);

	public Color AccentColor = new(0.20f, 0.90f, 1.0f);

	#endregion

	#region Runtime

	private SubViewport _viewport;
	private VBoxContainer _content;
	private Vector2 _previousMouse;
	private bool _mouseInside;

	#endregion

	#region Setup

	// =========================================================
// Creates the screen viewport, layered housing, and compact UI layout.
public override void _Ready()
{
	SetProcess(false);
	SetPhysicsProcess(false);

	_viewport = new SubViewport
	{
		Name = "ScreenViewport",
		Size = Resolution,
		Disable3D = true,
		TransparentBg = false,
		HandleInputLocally = true,
		RenderTargetUpdateMode = SubViewport.UpdateMode.Once
	};

	AddChild(_viewport);

	_viewport.AddChild(new ColorRect
	{
		Color = new Color(0.006f, 0.018f, 0.025f),
		Size = new Vector2(Resolution.X, Resolution.Y),
		MouseFilter = Control.MouseFilterEnum.Ignore
	});

	_content = new VBoxContainer
	{
		Position = new Vector2(28.0f, 22.0f),
		Size = new Vector2(
			Resolution.X - 56.0f,
			Resolution.Y - 44.0f
		),
		MouseFilter = Control.MouseFilterEnum.Ignore
	};

	_content.AddThemeConstantOverride("separation", 10);
	_viewport.AddChild(_content);

	Shader shader = GD.Load<Shader>(
		"res://PLAYER/Cockpit/CockpitScreen.gdshader"
	);

	Material screenMaterial;

	if (shader != null)
	{
		ShaderMaterial material = new()
		{
			Shader = shader
		};

		material.SetShaderParameter(
			"screen_texture",
			_viewport.GetTexture()
		);

		screenMaterial = material;
	}
	else
	{
		GD.PushError("Cannot load CockpitScreen.gdshader.");

		screenMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoTexture = _viewport.GetTexture()
		};
	}

	AddChild(new MeshInstance3D
	{
		Name = "Screen",
		Mesh = new QuadMesh { Size = ScreenSize },
		MaterialOverride = screenMaterial,
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
	});

	StandardMaterial3D darkMetal = new()
	{
		AlbedoColor = new Color(0.025f, 0.033f, 0.042f),
		Metallic = 0.65f,
		Roughness = 0.42f
	};

	StandardMaterial3D silver = new()
	{
		AlbedoColor = new Color(0.23f, 0.28f, 0.32f),
		Metallic = 0.8f,
		Roughness = 0.35f
	};

	AddHousingPart(
		"ScreenHousing",
		new Vector3(ScreenSize.X + 0.075f, ScreenSize.Y + 0.075f, 0.09f),
		new Vector3(0.0f, 0.0f, -0.052f),
		darkMetal
	);

	float horizontalEdge = ScreenSize.X * 0.5f + 0.012f;
	float verticalEdge = ScreenSize.Y * 0.5f + 0.012f;

	for (int side = -1; side <= 1; side += 2)
	{
		AddHousingPart(
			$"VerticalBezel{side}",
			new Vector3(0.016f, ScreenSize.Y + 0.04f, 0.02f),
			new Vector3(side * horizontalEdge, 0.0f, -0.002f),
			silver
		);

		AddHousingPart(
			$"HorizontalBezel{side}",
			new Vector3(ScreenSize.X + 0.04f, 0.016f, 0.02f),
			new Vector3(0.0f, side * verticalEdge, -0.002f),
			silver
		);
	}
}

	// =========================================================
	// Adds a cyan readout to the screen.
	public Label AddReadout(string text, int fontSize = 26)
	{
		Label label = new()
		{
			Text = text,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		label.AddThemeColorOverride("font_color", AccentColor);
		label.AddThemeFontSizeOverride("font_size", fontSize);

		_content.AddChild(label);
		Refresh();

		return label;
	}

	// =========================================================
	// Adds a working button with cockpit-style colours.
	public Button AddButton(string text)
	{
		Button button = new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(0.0f, 48.0f),
			FocusMode = Control.FocusModeEnum.None
		};

		button.AddThemeFontSizeOverride("font_size", 23);
		button.AddThemeColorOverride("font_color", AccentColor);
		button.AddThemeColorOverride("font_hover_color", Colors.White);

		StyleBoxFlat normal = new()
		{
			BgColor = new Color(0.015f, 0.075f, 0.09f),
			BorderColor = new Color(0.08f, 0.35f, 0.40f),
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			BorderWidthTop = 1,
			BorderWidthBottom = 1
		};

		StyleBoxFlat hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = new Color(0.025f, 0.16f, 0.19f);
		hover.BorderColor = AccentColor;

		StyleBoxFlat pressed = (StyleBoxFlat)hover.Duplicate();
		pressed.BgColor = new Color(0.04f, 0.24f, 0.28f);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);

		_content.AddChild(button);
		Refresh();

		return button;
	}

	// =========================================================
// Adds a physical housing piece behind or around the screen surface.
private void AddHousingPart(
	string name,
	Vector3 size,
	Vector3 position,
	Material material
)
{
	AddChild(new MeshInstance3D
	{
		Name = name,
		Position = position,
		Mesh = new BoxMesh { Size = size },
		MaterialOverride = material,
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
	});
}

// =========================================================
// Adds a vector instrument using the same UI layout as labels and buttons.
public CockpitInstrument AddInstrument(
	PlayerShip ship,
	bool showDefence
)
{
	CockpitInstrument instrument = new()
	{
		Ship = ship,
		ShowDefence = showDefence,
		AccentColor = AccentColor,
		CustomMinimumSize = new Vector2(0.0f, 230.0f),
		SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		MouseFilter = Control.MouseFilterEnum.Ignore
	};

	_content.AddChild(instrument);
	Refresh();

	return instrument;
}

	#endregion

	#region Display

	// =========================================================
	// Schedules one texture refresh without rendering continuously.
	public void Refresh()
	{
		if (_viewport != null)
		{
			_viewport.RenderTargetUpdateMode =
				SubViewport.UpdateMode.Once;
		}
	}

	// =========================================================
	// Enables or stops screen rendering when changing camera view.
	public void SetScreenVisible(bool visible)
	{
		Visible = visible;

		_viewport.RenderTargetUpdateMode = visible
			? SubViewport.UpdateMode.Once
			: SubViewport.UpdateMode.Disabled;

		if (!visible)
		{
			ClearPointer();
		}
	}

	#endregion

	#region Interaction

	// =========================================================
	// Intersects a camera ray with this screen and returns texture coordinates.
	private bool GetScreenPoint(
		Camera3D camera,
		Vector2 mousePosition,
		out Vector2 screenPoint
	)
	{
		screenPoint = Vector2.Zero;

		Transform3D inverse = GlobalTransform.AffineInverse();

		Vector3 origin =
			inverse * camera.ProjectRayOrigin(mousePosition);

		Vector3 direction =
			inverse.Basis * camera.ProjectRayNormal(mousePosition);

		// The visible face points along local positive Z.
		if (direction.Z >= -0.00001f)
		{
			return false;
		}

		float distance = -origin.Z / direction.Z;

		if (distance < 0.0f)
		{
			return false;
		}

		Vector3 hit = origin + direction * distance;

		if (Mathf.Abs(hit.X) > ScreenSize.X * 0.5f
			|| Mathf.Abs(hit.Y) > ScreenSize.Y * 0.5f)
		{
			return false;
		}

		screenPoint = new Vector2(
			(hit.X / ScreenSize.X + 0.5f) * Resolution.X,
			(0.5f - hit.Y / ScreenSize.Y) * Resolution.Y
		);

		return true;
	}

	// =========================================================
	// Sends translated mouse motion or button events to the screen UI.
	public void ForwardMouse(
		InputEvent inputEvent,
		Camera3D camera,
		Vector2 mousePosition
	)
	{
		bool inside = GetScreenPoint(
			camera,
			mousePosition,
			out Vector2 point
		);

		if (inputEvent is InputEventMouseMotion motion)
		{
			if (!inside)
			{
				ClearPointer();
				return;
			}

			InputEventMouseMotion translated =
				(InputEventMouseMotion)motion.Duplicate();

			translated.Position = point;
			translated.GlobalPosition = point;
			translated.Relative = _mouseInside
				? point - _previousMouse
				: Vector2.Zero;

			_mouseInside = true;
			_previousMouse = point;

			_viewport.PushInput(translated, true);
			Refresh();
		}
		else if (inputEvent is InputEventMouseButton button)
		{
			// Send releases outside too, so buttons cannot remain held.
			if (!inside && button.Pressed)
			{
				return;
			}

			InputEventMouseButton translated =
				(InputEventMouseButton)button.Duplicate();

			translated.Position = inside
				? point
				: new Vector2(-100.0f, -100.0f);

			translated.GlobalPosition = translated.Position;

			_viewport.PushInput(translated, true);
			Refresh();
		}
	}

	// =========================================================
	// Removes hover and releases a held left mouse button.
	public void ClearPointer()
	{
		if (_viewport == null)
		{
			return;
		}

		Vector2 outside = new(-100.0f, -100.0f);

		_viewport.PushInput(new InputEventMouseMotion
		{
			Position = outside,
			GlobalPosition = outside
		}, true);

		_viewport.PushInput(new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Pressed = false,
			Position = outside,
			GlobalPosition = outside
		}, true);

		_mouseInside = false;
		Refresh();
	}

	#endregion
}

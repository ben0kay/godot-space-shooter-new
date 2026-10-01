using System.Collections.Generic;
using Godot;

// Builds a replaceable cockpit frame and independent interactive ship screens.
public partial class PlayerCockpit : Node
{
	#region Settings

	[ExportGroup("Interaction")]

	[Export] public Key InteractionKey = Key.Tab;

	[ExportGroup("Placeholder Frame")]

	[Export] public bool BuildPlaceholderFrame = true;

	[ExportGroup("Screen Layout")]

	[Export] public Vector3 LeftScreenPosition =
		new(-0.61f, -0.33f, -0.88f);

	[Export] public Vector3 CentreScreenPosition =
		new(0.0f, -0.36f, -0.90f);

	[Export] public Vector3 RightScreenPosition =
		new(0.61f, -0.33f, -0.88f);

	[Export] public Vector3 LeftScreenRotationDegrees =
		new(-12.0f, 22.0f, 0.0f);

	[Export] public Vector3 CentreScreenRotationDegrees =
		new(-12.0f, 0.0f, 0.0f);

	[Export] public Vector3 RightScreenRotationDegrees =
		new(-12.0f, -22.0f, 0.0f);

	[Export] public Vector2 ScreenSize =
		new(0.56f, 0.30f);

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private PlayerFlightVisuals _flight;
	private PlayerWeaponInput _weapons;

	private Node3D _root;
	private readonly List<CockpitPanel> _panels = new();

private CockpitInstrument _flightInstrument;
private CockpitInstrument _defenceInstrument;
	private Label _weaponReadout;

	private bool _initialized;
	private bool _visible;
	private float _refreshRemaining;

	#endregion

	#region Setup

	// =========================================================
	// Finds existing player systems and waits for the visual pivot.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		if (_ship == null)
		{
			GD.PushError("PlayerCockpit must be a child of PlayerShip.");
			SetProcess(false);
			SetProcessInput(false);
			return;
		}

		_flight = _ship.GetNodeOrNull<PlayerFlightVisuals>(
			"FlightVisuals"
		);

		_weapons = _ship.GetNodeOrNull<PlayerWeaponInput>(
			"PlayerWeaponMount"
		);
	}

	// =========================================================
// Builds the cockpit frame and connects the three screens to player systems.
private bool InitializeCockpit()
{
	Node3D pivot = _ship.GetNodeOrNull<Node3D>(
		"FlightVisualPivot"
	);

	Marker3D eye = _ship.GetNodeOrNull<Marker3D>(
		"CockpitView"
	);

	if (pivot == null || eye == null || _flight == null)
	{
		return false;
	}

	_root = new Node3D
	{
		Name = "CockpitSystems",
		Visible = false
	};

	pivot.AddChild(_root);

	_root.Transform =
		_ship.GlobalTransform.AffineInverse()
		* eye.GlobalTransform;

	if (BuildPlaceholderFrame)
	{
		BuildFrame();
	}

	CockpitPanel left = CreatePanel(
		"FlightScreen",
		LeftScreenPosition,
		LeftScreenRotationDegrees
	);

	left.AddReadout("01  /  FLIGHT SYSTEMS", 25);
	_flightInstrument = left.AddInstrument(_ship, false);
	left.AddReadout("TAB  INTERACT     /     V  CAMERA", 17);

	CockpitPanel centre = CreatePanel(
		"DefenceScreen",
		CentreScreenPosition,
		CentreScreenRotationDegrees
	);

	centre.AddReadout("02  /  SHIP INTEGRITY", 25);
	_defenceInstrument = centre.AddInstrument(_ship, true);
	centre.AddReadout("SHIELD  /  ARMOUR  /  HULL", 17);

	CockpitPanel right = CreatePanel(
		"WeaponScreen",
		RightScreenPosition,
		RightScreenRotationDegrees
	);

	right.AddReadout("03  /  WEAPON SYSTEMS", 25);

	right.AddReadout(
		"PRIMARY  /  " + GetWeaponTitle(
			_weapons?.PrimaryWeapon?.Weapon
		),
		19
	);

	_weaponReadout = right.AddReadout("", 19);
	right.AddReadout("SECONDARY SELECTION", 17);

	if (_weapons != null)
	{
		for (int index = 0;
			index < _weapons.SecondaryOptions.Count;
			index++)
		{
			WeaponDefinition definition =
				_weapons.SecondaryOptions[index];

			if (definition == null)
			{
				continue;
			}

			int optionIndex = index;

			Button button = right.AddButton(
				GetWeaponTitle(definition)
			);

			button.Pressed += () =>
			{
				if (!_ship.CockpitInteractionActive)
				{
					return;
				}

				_weapons.SelectSecondary(optionIndex);
				RefreshReadouts();
			};
		}
	}

	RefreshReadouts();
	return true;
}

	// =========================================================
	// Adds a reusable panel with its own authored position and angle.
	private CockpitPanel CreatePanel(
		string name,
		Vector3 position,
		Vector3 rotationDegrees
	)
	{
		CockpitPanel panel = new()
		{
			Name = name,
			ScreenSize = ScreenSize,
			Position = position,
			RotationDegrees = rotationDegrees
		};

		_root.AddChild(panel);
		_panels.Add(panel);

		return panel;
	}

	// =========================================================
// Builds layered consoles, a central hood, and illuminated canopy supports.
private void BuildFrame()
{
	Node3D frame = new()
	{
		Name = "PlaceholderFrame"
	};

	_root.AddChild(frame);

	StandardMaterial3D metal = new()
	{
		AlbedoColor = new Color(0.035f, 0.045f, 0.055f),
		Metallic = 0.65f,
		Roughness = 0.45f
	};

	StandardMaterial3D silver = new()
	{
		AlbedoColor = new Color(0.24f, 0.28f, 0.32f),
		Metallic = 0.8f,
		Roughness = 0.35f
	};

	StandardMaterial3D light = new()
	{
		AlbedoColor = new Color(0.1f, 0.8f, 0.95f),
		ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		EmissionEnabled = true,
		Emission = new Color(0.1f, 0.8f, 0.95f),
		EmissionEnergyMultiplier = 1.2f
	};

	Vector3[] positions =
	{
		LeftScreenPosition,
		CentreScreenPosition,
		RightScreenPosition
	};

	Vector3[] rotations =
	{
		LeftScreenRotationDegrees,
		CentreScreenRotationDegrees,
		RightScreenRotationDegrees
	};

	for (int index = 0; index < positions.Length; index++)
	{
		Node3D console = new()
		{
			Name = $"Console{index}",
			Position = positions[index],
			RotationDegrees = rotations[index]
		};

		frame.AddChild(console);

		AddFramePart(
			console, "ConsoleBody",
			new Vector3(
				ScreenSize.X + 0.12f,
				ScreenSize.Y + 0.12f,
				0.20f
			),
			new Vector3(0.0f, -0.015f, -0.16f),
			Vector3.Zero, metal
		);

		AddFramePart(
			console, "LowerSilverTrim",
			new Vector3(ScreenSize.X + 0.09f, 0.025f, 0.055f),
			new Vector3(0.0f, -ScreenSize.Y * 0.5f - 0.045f, -0.06f),
			Vector3.Zero, silver
		);

		AddFramePart(
			console, "LowerLight",
			new Vector3(ScreenSize.X * 0.75f, 0.006f, 0.012f),
			new Vector3(0.0f, -ScreenSize.Y * 0.5f - 0.023f, -0.025f),
			Vector3.Zero, light
		);
	}

	AddFramePart(
		frame, "DashboardBase",
		new Vector3(2.02f, 0.12f, 0.35f),
		new Vector3(0.0f, -0.62f, -1.0f),
		Vector3.Zero, metal
	);

	AddFramePart(
		frame, "CentralHood",
		new Vector3(0.32f, 0.10f, 0.20f),
		new Vector3(0.0f, -0.15f, -1.04f),
		new Vector3(-12.0f, 0.0f, 0.0f),
		metal
	);

	for (int strip = 0; strip < 3; strip++)
	{
		AddFramePart(
			frame, $"CentralVent{strip}",
			new Vector3(0.21f, 0.006f, 0.012f),
			new Vector3(0.0f, -0.175f + strip * 0.022f, -0.925f),
			Vector3.Zero, light
		);
	}

	for (int side = -1; side <= 1; side += 2)
	{
		Vector3 pillarRotation = new(0.0f, 0.0f, side * 24.0f);

		AddFramePart(
			frame, $"CanopyPillar{side}",
			new Vector3(0.065f, 1.05f, 0.075f),
			new Vector3(side * 0.80f, 0.13f, -0.96f),
			pillarRotation, metal
		);

		AddFramePart(
			frame, $"CanopyTrim{side}",
			new Vector3(0.018f, 0.96f, 0.018f),
			new Vector3(side * 0.80f, 0.13f, -0.913f),
			pillarRotation, silver
		);

		AddFramePart(
			frame, $"CanopyLight{side}",
			new Vector3(0.007f, 0.72f, 0.009f),
			new Vector3(side * 0.80f, 0.13f, -0.900f),
			pillarRotation, light
		);
	}
}

	// =========================================================
	// Creates one visual frame piece without gameplay collision.
	private void AddFramePart(
		Node3D parent,
		string name,
		Vector3 size,
		Vector3 position,
		Vector3 rotationDegrees,
		Material material
	)
	{
		parent.AddChild(new MeshInstance3D
		{
			Name = name,
			Position = position,
			RotationDegrees = rotationDegrees,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		});
	}

	#endregion

	#region Display

	// =========================================================
	// Tracks camera mode and refreshes visible readouts ten times per second.
	public override void _Process(double delta)
	{
		if (!_initialized)
		{
			_initialized = InitializeCockpit();

			if (!_initialized)
			{
				return;
			}
		}

		bool visible =
			_flight.IsFirstPerson && _ship.IsCombatTargetable;

		if (visible != _visible)
		{
			_visible = visible;
			_root.Visible = visible;

			foreach (CockpitPanel panel in _panels)
			{
				panel.SetScreenVisible(visible);
			}
		}

		if (!visible)
		{
			if (_ship.CockpitInteractionActive)
			{
				SetInteraction(false);
			}

			return;
		}

		_refreshRemaining -= (float)delta;

		if (_refreshRemaining <= 0.0f)
		{
			_refreshRemaining = 0.1f;
			RefreshReadouts();
		}
	}

	// =========================================================
// Refreshes live instruments and highlights the equipped secondary weapon.
private void RefreshReadouts()
{
	_flightInstrument?.QueueRedraw();
	_defenceInstrument?.QueueRedraw();

	int selected = _weapons?.SecondaryIndex ?? -1;

	_weaponReadout.Text =
		_weapons != null
		&& selected >= 0
		&& selected < _weapons.SecondaryOptions.Count
			? "SECONDARY  /  " + GetWeaponTitle(
				_weapons.SecondaryOptions[selected]
			)
			: "SECONDARY  /  UNASSIGNED";

	foreach (CockpitPanel panel in _panels)
	{
		if (_visible)
		{
			panel.Refresh();
		}
	}
}

	// =========================================================
	// Uses resource names or filenames as temporary equipment labels.
	private string GetWeaponTitle(WeaponDefinition definition)
	{
		if (definition == null)
		{
			return "UNASSIGNED";
		}

		string title = definition.ResourceName;

		if (string.IsNullOrWhiteSpace(title))
		{
			title = definition.ResourcePath.GetFile().GetBaseName();
		}

		return string.IsNullOrWhiteSpace(title)
			? definition.Delivery.ToString().ToUpperInvariant()
			: title.ToUpperInvariant();
	}

	#endregion

	#region Interaction

	// =========================================================
	// Toggles cockpit control and forwards pointer events to the visible screens.
	public override void _Input(InputEvent inputEvent)
	{
		if (!_initialized || !_ship.IsCombatTargetable)
		{
			return;
		}

		if (inputEvent is InputEventKey key
			&& key.Pressed
			&& !key.Echo)
		{
			if (key.PhysicalKeycode == InteractionKey
				&& _flight.IsFirstPerson)
			{
				SetInteraction(!_ship.CockpitInteractionActive);
				GetViewport().SetInputAsHandled();
				return;
			}

			if (key.PhysicalKeycode == Key.Escape
				&& _ship.CockpitInteractionActive)
			{
				SetInteraction(false);
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (!_ship.CockpitInteractionActive
			|| inputEvent is not InputEventMouse)
		{
			return;
		}

		Camera3D camera = GetViewport().GetCamera3D();

		if (camera == null)
		{
			return;
		}

		Vector2 mousePosition = GetViewport().GetMousePosition();

		foreach (CockpitPanel panel in _panels)
		{
			panel.ForwardMouse(inputEvent, camera, mousePosition);
		}

		GetViewport().SetInputAsHandled();
	}

	// =========================================================
	// Changes input ownership and immediately stops held weapons.
	private void SetInteraction(bool active)
	{
		_ship.SetCockpitInteraction(active);
		_weapons?.PrimaryWeapon?.SetTriggerHeld(false);
		_weapons?.SecondaryWeapon?.SetTriggerHeld(false);

		foreach (CockpitPanel panel in _panels)
		{
			panel.ClearPointer();
		}

		RefreshReadouts();
	}

	// =========================================================
	// Leaves interaction safely without capturing the mouse after losing focus.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut
			&& GodotObject.IsInstanceValid(_ship)
			&& _ship.CockpitInteractionActive)
		{
			SetInteraction(false);
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	// =========================================================
	// Removes generated cockpit nodes if this component is removed.
	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_root))
		{
			_root.QueueFree();
		}
	}

	#endregion
}
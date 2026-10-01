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

	private Label _flightReadout;
	private Label _defenceReadout;
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
	// Creates independent frame and screen roots at the cockpit eye position.
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

		left.AddReadout("FLIGHT SYSTEMS", 30);
		_flightReadout = left.AddReadout("");
		left.AddReadout("TAB  /  INTERACT", 22);
		left.AddReadout("V    /  CAMERA", 22);

		CockpitPanel centre = CreatePanel(
			"DefenceScreen",
			CentreScreenPosition,
			CentreScreenRotationDegrees
		);

		centre.AddReadout("SHIP INTEGRITY", 30);
		_defenceReadout = centre.AddReadout("");

		CockpitPanel right = CreatePanel(
			"WeaponScreen",
			RightScreenPosition,
			RightScreenRotationDegrees
		);

		right.AddReadout("SECONDARY WEAPON", 30);
		_weaponReadout = right.AddReadout("", 22);

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
	// Builds only the temporary cockpit housing, separate from the panels.
	private void BuildFrame()
	{
		Node3D frame = new()
		{
			Name = "PlaceholderFrame"
		};

		_root.AddChild(frame);

		StandardMaterial3D metal = new()
		{
			AlbedoColor = new Color(0.055f, 0.065f, 0.075f),
			Metallic = 0.75f,
			Roughness = 0.40f
		};

		StandardMaterial3D light = new()
		{
			AlbedoColor = new Color(0.1f, 0.8f, 0.95f),
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			EmissionEnabled = true,
			Emission = new Color(0.1f, 0.8f, 0.95f),
			EmissionEnergyMultiplier = 1.5f
		};

		AddFramePart(
			frame, "Dashboard",
			new Vector3(1.95f, 0.12f, 0.22f),
			new Vector3(0.0f, -0.56f, -0.94f),
			Vector3.Zero, metal
		);

		AddFramePart(
			frame, "DashboardLight",
			new Vector3(1.80f, 0.012f, 0.012f),
			new Vector3(0.0f, -0.49f, -0.81f),
			Vector3.Zero, light
		);

		for (int side = -1; side <= 1; side += 2)
		{
			AddFramePart(
				frame, $"CanopyPillar{side}",
				new Vector3(0.035f, 0.95f, 0.045f),
				new Vector3(side * 0.77f, 0.12f, -0.95f),
				new Vector3(0.0f, 0.0f, side * 24.0f),
				metal
			);

			AddFramePart(
				frame, $"CanopyLight{side}",
				new Vector3(0.009f, 0.70f, 0.012f),
				new Vector3(side * 0.77f, 0.12f, -0.92f),
				new Vector3(0.0f, 0.0f, side * 24.0f),
				light
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
	// Reads existing ship state without owning or duplicating gameplay stats.
	private void RefreshReadouts()
	{
		_flightReadout.Text =
			$"SPEED   {_ship.Velocity.Length():0.0}\n"
			+ $"BOOST   {_ship.BoostAmount * 100.0f:0}%\n"
			+ (_ship.CockpitInteractionActive
				? "MODE    COCKPIT"
				: "MODE    FLIGHT");

		ShipDefence defence = _ship.Defence;

		_defenceReadout.Text = defence == null
			? "DEFENCE OFFLINE"
			: $"SHIELD   {defence.Shield:0} / {defence.MaxShield:0}\n"
			+ $"ARMOUR   {defence.Armour:0} / {defence.MaxArmour:0}\n"
			+ $"HULL     {defence.Hull:0} / {defence.MaxHull:0}";

		int selected = _weapons?.SecondaryIndex ?? -1;

		_weaponReadout.Text =
			_weapons != null
			&& selected >= 0
			&& selected < _weapons.SecondaryOptions.Count
				? "ACTIVE: " + GetWeaponTitle(
					_weapons.SecondaryOptions[selected]
				)
				: "NO SECONDARY EQUIPPED";

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
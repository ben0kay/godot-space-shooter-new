using System.Collections.Generic;
using Godot;

// Transfers input and camera ownership between the pilot and independent interior walker.
public partial class PlayerInteriorSession : Node
{
	#region Settings

	[ExportGroup("Interior")]
	[Export] public PackedScene InteriorScene;

	[ExportGroup("Interaction")]
	[Export] public Key LeaveSeatKey = Key.F;
	[Export] public Key EnterSeatKey = Key.F;
	[Export] public float SeatUseDistance = 1.2f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private PlayerFlightInput _pilotInput;
	private PlayerFlightVisuals _flight;
	private PlayerDash _dash;
	private PlayerWeaponInput _weapons;

	private Camera3D _flightCamera;
	private Node3D _flightPivot;
	private ShipInteriorRoom _room;
	private InteriorWalker _walker;
	private PlayerCrosshair _crosshair;
	private Label _prompt;

	private bool _walking;
	private bool _pivotWasVisible;
	private bool _previousFirstPerson;
	private bool _previousPilotEnabled;
	private bool _crosshairWasVisible;

	private readonly Dictionary<Node, (bool Input, bool Physics, bool Process)>
		_controllerStates = new();

	#endregion

	#region Setup

	// =========================================================
	// Waits for the existing player and flight camera setup to finish.
	public override void _Ready()
	{
		SetProcess(false);
		SetProcessUnhandledInput(false);
		CallDeferred(nameof(InitializeInterior));
	}

	// =========================================================
	// Creates the interior independently and positions it at the cockpit station.
	private void InitializeInterior()
	{
		_ship = NodeHelpers.FindAncestor<PlayerShip>(this);
		_pilotInput = _ship?.FlightInput;
		_flight = _ship?.GetNodeOrNull<PlayerFlightVisuals>("FlightVisuals");
		_flightCamera = _ship?.GetNodeOrNull<Camera3D>("Camera3D");
		_flightPivot = _ship?.GetNodeOrNull<Node3D>("FlightVisualPivot");
		Marker3D eye = _ship?.GetNodeOrNull<Marker3D>("CockpitView");

		if (_ship == null || _pilotInput == null || _flight == null
			|| _flightCamera == null || _flightPivot == null
			|| eye == null || InteriorScene == null)
		{
			GD.PushError(
				"PlayerInteriorSession needs flight input, cameras, visual pivot and InteriorScene."
			);
			return;
		}

		Node instance = InteriorScene.Instantiate();

		if (instance is not ShipInteriorRoom room)
		{
			instance.Free();
			GD.PushError("InteriorScene requires ShipInteriorRoom on its root.");
			return;
		}

		_room = room;
		_ship.AddChild(_room);
		_walker = _room.GetNodeOrNull<InteriorWalker>("Walker");

		if (_walker == null || _walker.Camera == null)
		{
			GD.PushError("ShipInterior needs an InteriorWalker named Walker.");
			_room.QueueFree();
			return;
		}

		Transform3D eyeLocal =
			_ship.GlobalTransform.AffineInverse() * eye.GlobalTransform;

		_room.Transform = new Transform3D(
			eyeLocal.Basis.Orthonormalized(), eyeLocal.Origin
		);
		_room.Position -= _room.Basis * (
			_room.PilotPosition + Vector3.Up * _walker.EyeHeight
		);
		_room.Visible = false;

		_dash = _ship.GetNodeOrNull<PlayerDash>("Dash");
		_weapons = _ship.GetNodeOrNull<PlayerWeaponInput>("Weapons/PlayerWeaponMount");
		_crosshair = _ship.GetNodeOrNull<PlayerCrosshair>("PlayerHud/Crosshair");

		BuildPrompt();
		SetProcess(true);
		SetProcessUnhandledInput(true);
	}

	// =========================================================
	// Builds the walking interaction prompt without intercepting controls.
	private void BuildPrompt()
	{
		CanvasLayer overlay = new() { Name = "InteriorPrompt", Layer = 45 };
		AddChild(overlay);

		_prompt = new Label
		{
			Visible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		overlay.AddChild(_prompt);

		_prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
		_prompt.OffsetLeft = -350;
		_prompt.OffsetRight = 350;
		_prompt.OffsetTop = -100;
		_prompt.OffsetBottom = -65;

		_prompt.AddThemeFontSizeOverride("font_size", 18);
		_prompt.AddThemeColorOverride("font_color", new Color(0.6f, 0.95f, 1));
	}

	#endregion

	#region Interaction

	// =========================================================
	// Transfers control when leaving the seat or using the nearby pilot station.
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey key
			|| !key.Pressed || key.Echo
			|| !_ship.IsCombatTargetable
			|| Input.MouseMode != Input.MouseModeEnum.Captured)
			return;

		if (!_walking && key.PhysicalKeycode == LeaveSeatKey)
		{
			if (!_pilotInput.Enabled || _ship.CockpitInteractionActive || IsTravelling())
				return;

			BeginWalking();
			GetViewport().SetInputAsHandled();
		}
		else if (_walking && key.PhysicalKeycode == EnterSeatKey && CanUseSeat())
		{
			ReturnToSeat();
			GetViewport().SetInputAsHandled();
		}
	}

	// =========================================================
	// Prevents a control transfer during an existing sector transition.
	private bool IsTravelling()
	{
		SectorManager manager =
			GetTree().GetFirstNodeInGroup("sector_manager") as SectorManager;

		return manager?.IsTravelling == true;
	}

	// =========================================================
	// Checks distance to the pilot station in the room's local coordinates.
	private bool CanUseSeat()
	{
		return _walker.Position.DistanceTo(_room.PilotPosition)
			<= Mathf.Max(0.1f, SeatUseDistance);
	}

	// =========================================================
	// Routes input to the walker while leaving ship simulation active.
	private void BeginWalking()
	{
		_previousFirstPerson = _flight.IsFirstPerson;
		_previousPilotEnabled = _pilotInput.Enabled;
		_pivotWasVisible = _flightPivot.Visible;
		_crosshairWasVisible = _crosshair?.Visible == true;

		_dash?.ClearInputRequests();
		_weapons?.PrimaryWeapon?.SetTriggerHeld(false);
		_weapons?.SecondaryWeapon?.SetTriggerHeld(false);

		_pilotInput.SetEnabled(false);

		// These components read pilot inputs; their underlying simulations stay alive.
		SuspendController(_dash);
		SuspendController(_flight);
		SuspendController(_ship.GetNodeOrNull<PlayerCockpit>("Cockpit"));

		// This physics callback only routes fire-button requests to weapon mounts.
		SuspendController(_weapons, stopPhysics: true);
		SuspendController(_crosshair, stopProcess: true);

		if (_crosshair != null)
			_crosshair.Visible = false;

		// Hide the exterior shell for this oversized placeholder interior.
		_flightPivot.Visible = false;
		_room.Visible = true;

		_walking = true;
		_walker.SetActive(true);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// =========================================================
	// Returns control to the pilot and restores the previous flight camera.
	private void ReturnToSeat()
	{
		_walker.SetActive(false);
		_room.Visible = false;
		_flightPivot.Visible = _pivotWasVisible;

		RestoreControllers();
		_pilotInput.SetEnabled(_previousPilotEnabled);

		if (_crosshair != null)
			_crosshair.Visible = _crosshairWasVisible;

		_walking = false;
		_flight.SetCameraMode(_previousFirstPerson);
		_flightCamera.MakeCurrent();

		_prompt.Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	#endregion

	#region Controller Routing

	// =========================================================
	// Saves input-controller callback settings without disabling the node hierarchy.
	private void SuspendController(
		Node controller, bool stopPhysics = false, bool stopProcess = false
	)
	{
		if (controller == null || _controllerStates.ContainsKey(controller))
			return;

		_controllerStates.Add(controller, (
			controller.IsProcessingInput(),
			controller.IsPhysicsProcessing(),
			controller.IsProcessing()
		));

		controller.SetProcessInput(false);

		if (stopPhysics)
			controller.SetPhysicsProcess(false);
		if (stopProcess)
			controller.SetProcess(false);
	}

	// =========================================================
	// Restores the precise callback settings saved before leaving the seat.
	private void RestoreControllers()
	{
		foreach (var entry in _controllerStates)
		{
			if (!GodotObject.IsInstanceValid(entry.Key))
				continue;

			entry.Key.SetProcessInput(entry.Value.Input);
			entry.Key.SetPhysicsProcess(entry.Value.Physics);
			entry.Key.SetProcess(entry.Value.Process);
		}

		_controllerStates.Clear();
	}

	#endregion

	#region Presentation

	// =========================================================
	// Displays the station prompt while the independent walker is active.
	public override void _Process(double delta)
	{
		if (!_walking)
			return;

		if (!_ship.IsCombatTargetable)
		{
			_walker.SetActive(false);
			_prompt.Visible = true;
			_prompt.Text = "SHIP LOST";
			Input.MouseMode = Input.MouseModeEnum.Visible;
			return;
		}

		_prompt.Visible = Input.MouseMode == Input.MouseModeEnum.Captured;
		_prompt.Text = CanUseSeat()
			? $"{EnterSeatKey}  /  RETURN TO PILOT SEAT"
			: "ARROW KEYS  /  WALK     MOUSE  /  LOOK";
	}

	#endregion
}
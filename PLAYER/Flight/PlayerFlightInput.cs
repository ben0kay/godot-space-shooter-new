using Godot;

// Reads pilot controls independently of ship physics and interior walking.
public partial class PlayerFlightInput : Node
{
	#region Controls

	[ExportGroup("Movement")]
	[Export] public Key ForwardKey = Key.Up;
	[Export] public Key ReverseKey = Key.Down;
	[Export] public Key StrafeLeftKey = Key.Left;
	[Export] public Key StrafeRightKey = Key.Right;
	[Export] public Key RollLeftKey = Key.X;
	[Export] public Key RollRightKey = Key.Z;
	[Export] public Key BoostKey = Key.Shift;
	[Export] public MouseButton AscendButton = MouseButton.Right;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private Vector2 _mouseMovement;
	private bool _rightCtrlHeld;
	private bool _hasFocus = true;

	public bool Enabled { get; private set; } = true;

	public bool CanReadControls =>
		Enabled
		&& _hasFocus
		&& GodotObject.IsInstanceValid(_ship)
		&& _ship.IsCombatTargetable
		&& _ship.CanProcess()
		&& !_ship.CockpitInteractionActive
		&& Input.MouseMode == Input.MouseModeEnum.Captured;

	#endregion

	#region Setup

	// =========================================================
	// Finds the ship without taking responsibility for its simulation.
	public override void _Ready()
	{
		_ship = NodeHelpers.FindAncestor<PlayerShip>(this);

		if (_ship == null)
		{
			GD.PushError("PlayerFlightInput needs a PlayerShip ancestor.");
			SetEnabled(false);
		}
	}

	// =========================================================
	// Transfers pilot input ownership and discards stale held controls.
	public void SetEnabled(bool enabled)
	{
		Enabled = enabled;
		ClearControls();
		SetProcessUnhandledInput(enabled);
	}

	// =========================================================
	// Clears accumulated mouse movement and tracked key state.
	public void ClearControls()
	{
		_mouseMovement = Vector2.Zero;
		_rightCtrlHeld = false;
	}

	#endregion

	#region Input Events

	// =========================================================
	// Receives flight mouse input after interface controls have handled events.
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!Enabled || !_hasFocus
			|| !GodotObject.IsInstanceValid(_ship)
			|| !_ship.IsCombatTargetable
			|| _ship.CockpitInteractionActive)
			return;

		if (inputEvent is InputEventKey key)
		{
			if (key.PhysicalKeycode == Key.Escape && key.Pressed && !key.Echo)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				ClearControls();
				return;
			}

			if (key.PhysicalKeycode == Key.Ctrl
				&& key.Location == KeyLocation.Right && !key.Echo)
				_rightCtrlHeld = key.Pressed;
		}

		if (inputEvent is InputEventMouseButton button
			&& button.Pressed
			&& Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			ClearControls();
			return;
		}

		if (inputEvent is InputEventMouseMotion motion && CanReadControls)
			_mouseMovement += motion.ScreenRelative;
	}

	// =========================================================
	// Prevents controls from sticking when the application loses focus.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			_hasFocus = false;
			ClearControls();
		}
		else if (what == NotificationApplicationFocusIn)
			_hasFocus = true;
	}

	#endregion

	#region Commands

	// =========================================================
	// Returns movement requests without consuming buffered mouse movement.
	public ShipFlightCommands ReadMovementCommands()
	{
		if (!CanReadControls)
			return default;

		ShipFlightCommands commands = new() { Active = true };

		if (Input.IsPhysicalKeyPressed(ForwardKey)) commands.Thrust += 1;
		if (Input.IsPhysicalKeyPressed(ReverseKey)) commands.Thrust -= 1;
		if (Input.IsPhysicalKeyPressed(StrafeLeftKey)) commands.Strafe -= 1;
		if (Input.IsPhysicalKeyPressed(StrafeRightKey)) commands.Strafe += 1;
		if (Input.IsPhysicalKeyPressed(RollLeftKey)) commands.Roll -= 1;
		if (Input.IsPhysicalKeyPressed(RollRightKey)) commands.Roll += 1;
		if (Input.IsMouseButtonPressed(AscendButton)) commands.Rise += 1;
		if (_rightCtrlHeld) commands.Rise -= 1;

		commands.Boost = Input.IsPhysicalKeyPressed(BoostKey);
		return commands;
	}

	// =========================================================
	// Produces one physics tick of commands and consumes its mouse movement.
	public ShipFlightCommands ReadCommands()
	{
		ShipFlightCommands commands = ReadMovementCommands();
		commands.MouseMotion = commands.Active ? _mouseMovement : Vector2.Zero;
		_mouseMovement = Vector2.Zero;
		return commands;
	}

	#endregion
}
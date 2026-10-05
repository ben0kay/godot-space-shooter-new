using Godot;

// Provides ship-local walking and mouse look for the single-room interior prototype.
public partial class InteriorWalker : Node3D
{
	#region Settings

	[ExportGroup("Walking")]
	[Export] public float WalkSpeed = 2.0f;
	[Export] public float WalkResponse = 12.0f;

	[ExportGroup("View")]
	[Export] public float EyeHeight = 1.65f;
	[Export] public float MouseSensitivity = 0.002f;
	[Export] public float MaximumPitchDegrees = 80.0f;
	[Export] public float FieldOfView = 80.0f;

	public Camera3D Camera { get; private set; }
	public bool Active { get; private set; }

	#endregion

	#region Runtime

	private ShipInteriorRoom _room;
	private Vector3 _movement;
	private float _pitch;

	#endregion

	#region Setup

	// =========================================================
	// Creates a dedicated walking camera without taking control of flight.
	public override void _Ready()
	{
		_room = GetParent() as ShipInteriorRoom;

		if (_room == null)
		{
			GD.PushError("InteriorWalker must be a child of ShipInteriorRoom.");
			SetPhysicsProcess(false);
			SetProcessUnhandledInput(false);
			return;
		}

		Camera = new Camera3D
		{
			Name = "WalkingCamera",
			Position = new Vector3(0, EyeHeight, 0),
			Fov = FieldOfView,
			Near = 0.03f,
			Far = 20000.0f,
			Current = false
		};
		AddChild(Camera);
		SetActive(false);
	}

	// =========================================================
	// Activates walking and resets its local position when leaving the seat.
	public void SetActive(bool active)
	{
		Active = active;
		_movement = Vector3.Zero;
		SetPhysicsProcess(active);
		SetProcessUnhandledInput(active);

		if (!active || Camera == null)
			return;

		Position = _room.StandingPosition;
		Rotation = Vector3.Zero;
		_pitch = 0.0f;
		Camera.Rotation = Vector3.Zero;
		Camera.MakeCurrent();
	}

	#endregion

	#region Input

	// =========================================================
	// Looks around locally and releases or recaptures the mouse.
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!Active)
			return;

		if (inputEvent is InputEventKey key
			&& key.Pressed && !key.Echo
			&& key.PhysicalKeycode == Key.Escape)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (inputEvent is InputEventMouseButton button
			&& button.Pressed
			&& Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (inputEvent is not InputEventMouseMotion motion
			|| Input.MouseMode != Input.MouseModeEnum.Captured)
			return;

		RotateY(-motion.ScreenRelative.X * MouseSensitivity);

		float limit = Mathf.DegToRad(MaximumPitchDegrees);
		_pitch = Mathf.Clamp(
			_pitch - motion.ScreenRelative.Y * MouseSensitivity,
			-limit, limit
		);
		Camera.Rotation = new Vector3(_pitch, 0, 0);
		GetViewport().SetInputAsHandled();
	}

	#endregion

	#region Movement

	// =========================================================
	// Walks relative to viewing yaw while remaining on the room's floor.
	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;
		Vector2 input = Vector2.Zero;

		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (Input.IsPhysicalKeyPressed(Key.Left)) input.X -= 1;
			if (Input.IsPhysicalKeyPressed(Key.Right)) input.X += 1;
			if (Input.IsPhysicalKeyPressed(Key.Up)) input.Y -= 1;
			if (Input.IsPhysicalKeyPressed(Key.Down)) input.Y += 1;
		}

		if (input.LengthSquared() > 1.0f)
			input = input.Normalized();

		Vector3 desired = Basis * new Vector3(input.X, 0, input.Y);
		desired *= Mathf.Max(0.0f, WalkSpeed);

		_movement = _movement.MoveToward(
			desired, Mathf.Max(0.0f, WalkResponse) * seconds
		);

		Vector3 requested = Position + _movement * seconds;
		Vector3 constrained = _room.ClampWalkingPosition(requested);

		if (!Mathf.IsEqualApprox(requested.X, constrained.X))
			_movement.X = 0;
		if (!Mathf.IsEqualApprox(requested.Z, constrained.Z))
			_movement.Z = 0;

		Position = constrained;
	}

	// =========================================================
	// Stops local movement and releases the pointer when focus is lost.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut && Active)
		{
			_movement = Vector3.Zero;
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	#endregion
}
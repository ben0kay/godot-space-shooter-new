
using Godot;

public partial class PlayerShip : CharacterBody3D
{
	#region Controls

	private const Key KEY_FORWARD = Key.Up;
	private const Key KEY_REVERSE = Key.Down;

	private const Key KEY_YAW_LEFT = Key.Left;
	private const Key KEY_YAW_RIGHT = Key.Right;

	private const Key KEY_ROLL_LEFT = Key.X;
	private const Key KEY_ROLL_RIGHT = Key.Z;

	private const MouseButton MOUSE_ASCEND = MouseButton.Right;
	private const Key KEY_DESCEND = Key.Ctrl;
	private const KeyLocation KEY_DESCEND_LOCATION = KeyLocation.Right;

	private const Key KEY_RELEASE_MOUSE = Key.Escape;

	#endregion

	#region Flight Settings

	[Export] public float ForwardSpeed = 18.0f;
	[Export] public float ReverseSpeed = 8.0f;
	[Export] public float VerticalSpeed = 10.0f;
	[Export] public float Acceleration = 24.0f;
	[Export] public float Deceleration = 18.0f;

	[Export] public float TurnSpeed = 120.0f;
	[Export] public float RollSpeed = 150.0f;
	[Export] public float MousePitchSensitivity = 0.0025f;
	[Export] public float MouseYawSensitivity = 0.0025f;

	#endregion

	#region Variables

	private bool _rightCtrlHeld;
	private Vector2 _mouseMovement;

	#endregion

	#region Godot Events

	public override void _Ready()
	{
		CreateBox("Hull", Vector3.Zero, new Vector3(2, 0.5f, 3), Colors.SteelBlue);
		CreateBox("Nose", new Vector3(0, 0, -1.7f), new Vector3(0.8f, 0.35f, 1), Colors.OrangeRed);

		BoxShape3D shape = new BoxShape3D();
		shape.Size = new Vector3(2, 0.5f, 3);

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = shape;
		AddChild(collision);

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey key)
		{
			if (key.Keycode == KEY_RELEASE_MOUSE && key.Pressed && !key.Echo)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				_mouseMovement = Vector2.Zero;
				return;
			}

			if (key.Keycode == KEY_DESCEND
				&& key.Location == KEY_DESCEND_LOCATION
				&& !key.Echo)
			{
				_rightCtrlHeld = key.Pressed;
			}
		}

		if (inputEvent is InputEventMouseButton button
			&& button.Pressed
			&& Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			return;
		}

		if (inputEvent is InputEventMouseMotion motion
			&& Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_mouseMovement += motion.ScreenRelative;
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			_rightCtrlHeld = false;
			_mouseMovement = Vector2.Zero;
		}
	}

	public override void _ExitTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		UpdateRotation(seconds);
		UpdateMovement(seconds);
	}

	#endregion

	#region Flight Movement

	private void UpdateRotation(float seconds)
	{
		float keyboardYaw = 0.0f;
		float keyboardRoll = 0.0f;

		if (Input.IsPhysicalKeyPressed(KEY_YAW_LEFT)) keyboardYaw -= 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_YAW_RIGHT)) keyboardYaw += 1.0f;

		if (Input.IsPhysicalKeyPressed(KEY_ROLL_LEFT)) keyboardRoll -= 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_ROLL_RIGHT)) keyboardRoll += 1.0f;

		float pitch = _mouseMovement.Y * MousePitchSensitivity;

		float yaw =
			-_mouseMovement.X * MouseYawSensitivity
			-Mathf.DegToRad(keyboardYaw * TurnSpeed * seconds);

		float roll = Mathf.DegToRad(keyboardRoll * RollSpeed * seconds);

		RotateObjectLocal(Vector3.Right, pitch);
		RotateObjectLocal(Vector3.Up, yaw);
		RotateObjectLocal(Vector3.Forward, roll);

		_mouseMovement = Vector2.Zero;
	}

	private void UpdateMovement(float seconds)
	{
		float thrust = 0.0f;

		if (Input.IsPhysicalKeyPressed(KEY_FORWARD)) thrust += 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_REVERSE)) thrust -= 1.0f;

		float rise = 0.0f;

		if (Input.IsMouseButtonPressed(MOUSE_ASCEND)) rise += 1.0f;
		if (_rightCtrlHeld) rise -= 1.0f;

		float speed = thrust >= 0.0f ? ForwardSpeed : ReverseSpeed;

		Vector3 forwardVelocity = -GlobalBasis.Z * thrust * speed;
		Vector3 verticalVelocity = GlobalBasis.Y * rise * VerticalSpeed;
		Vector3 targetVelocity = forwardVelocity + verticalVelocity;

		float response = targetVelocity == Vector3.Zero
			? Deceleration
			: Acceleration;

		Velocity = Velocity.MoveToward(targetVelocity, response * seconds);

		MoveAndSlide();
	}

	#endregion

	#region Visual Helpers

	private void CreateBox(string name, Vector3 position, Vector3 size, Color color)
	{
		BoxMesh mesh = new BoxMesh();
		mesh.Size = size;

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = name;
		visual.Position = position;
		visual.Mesh = mesh;
		AddChild(visual);
	}

	#endregion
}

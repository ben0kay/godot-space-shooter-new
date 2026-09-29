using System;
using Godot;

public partial class PlayerShip : CharacterBody3D, IDamageable
{
	#region Definition

	[Export] public PlayerShipDefinition Definition;

	#endregion

	#region Controls

	private const Key KEY_FORWARD = Key.Up;
	private const Key KEY_REVERSE = Key.Down;
	private const Key KEY_STRAFE_LEFT = Key.Left;
	private const Key KEY_STRAFE_RIGHT = Key.Right;

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
	[Export] public float StrafeSpeed = 12.0f;
	[Export] public float VerticalSpeed = 10.0f;
	[Export] public float Acceleration = 24.0f;
	[Export] public float Deceleration = 18.0f;

	[Export] public float RollSpeed = 75.0f;
	[Export] public float MousePitchSensitivity = 0.0008f;
	[Export] public float MouseYawSensitivity = 0.0008f;

	#endregion

	#region Runtime

	private bool _rightCtrlHeld;
	private bool _destroyed;
	private Vector2 _mouseMovement;

	public ShipDefence Defence { get; private set; }
	public event Action DefenceChanged;

	#endregion

	#region Godot Events

	// Creates collision and initializes this ship's defence and HUD.
	public override void _Ready()
	{
		AddToGroup("player_ship");
		BoxShape3D shape = new BoxShape3D();
		shape.Size = new Vector3(3.8f, 1.0f, 3.8f);

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = shape;
		collision.Position = new Vector3(0, 0.06f, -0.225f);
		AddChild(collision);

		if (Definition == null)
		{
			GD.PushError("Assign a PlayerShipDefinition to PlayerShip.");
		}
		else
		{
			Defence = new ShipDefence(
				Definition.MaxShield,
				Definition.MaxArmour,
				Definition.MaxHull
			);

			PlayerDefenceHud hud = new PlayerDefenceHud();
			hud.Name = "PlayerDefenceHud";
			AddChild(hud);
		}

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (_destroyed)
		{
			return;
		}

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
		if (_destroyed)
		{
			return;
		}

		float seconds = (float)delta;

		UpdateRotation(seconds);
		UpdateMovement(seconds);
	}

	#endregion

	#region Defence

	// Applies a projectile hit through shield, armour, and hull.
	public void ApplyDamage(DamageInfo damage)
	{
		if (Defence == null || _destroyed || damage.Amount <= 0.0f)
		{
			return;
		}

		Defence.ApplyDamage(damage.Amount);
		DefenceChanged?.Invoke();

		if (Defence.Destroyed)
		{
			_destroyed = true;
			Velocity = Vector3.Zero;
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GD.Print("Player ship destroyed.");
		}
	}

	#endregion

	#region Flight Movement

	private void UpdateRotation(float seconds)
	{
		float keyboardRoll = 0.0f;

		if (Input.IsPhysicalKeyPressed(KEY_ROLL_LEFT)) keyboardRoll -= 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_ROLL_RIGHT)) keyboardRoll += 1.0f;

		float pitch = _mouseMovement.Y * MousePitchSensitivity;
		float yaw = -_mouseMovement.X * MouseYawSensitivity;
		float roll = Mathf.DegToRad(keyboardRoll * RollSpeed * seconds);

		RotateObjectLocal(Vector3.Right, pitch);
		RotateObjectLocal(Vector3.Up, yaw);
		RotateObjectLocal(Vector3.Forward, roll);

		_mouseMovement = Vector2.Zero;
	}

	private void UpdateMovement(float seconds)
	{
		float thrust = 0.0f;
		float strafe = 0.0f;
		float rise = 0.0f;

		if (Input.IsPhysicalKeyPressed(KEY_FORWARD)) thrust += 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_REVERSE)) thrust -= 1.0f;

		if (Input.IsPhysicalKeyPressed(KEY_STRAFE_LEFT)) strafe -= 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_STRAFE_RIGHT)) strafe += 1.0f;

		if (Input.IsMouseButtonPressed(MOUSE_ASCEND)) rise += 1.0f;
		if (_rightCtrlHeld) rise -= 1.0f;

		float forwardSpeed = thrust >= 0.0f ? ForwardSpeed : ReverseSpeed;

		Vector3 forwardVelocity = -GlobalBasis.Z * thrust * forwardSpeed;
		Vector3 strafeVelocity = GlobalBasis.X * strafe * StrafeSpeed;
		Vector3 verticalVelocity = GlobalBasis.Y * rise * VerticalSpeed;

		Vector3 targetVelocity =
			forwardVelocity + strafeVelocity + verticalVelocity;

		float response = targetVelocity == Vector3.Zero
			? Deceleration
			: Acceleration;

		Velocity = Velocity.MoveToward(targetVelocity, response * seconds);

		MoveAndSlide();
	}

	#endregion
}

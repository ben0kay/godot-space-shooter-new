using System;
using Godot;

// Handles player flight, boost input, defence, and steering signals for visuals.
public partial class PlayerShip : CharacterBody3D, IDamageable, ICombatTarget
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
	private const Key KEY_BOOST = Key.Shift;

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

	#region Steering Settings

[Export] public float MaxPitchSpeedDegrees = 60.0f;
[Export] public float MaxYawSpeedDegrees = 75.0f;
[Export] public float SteeringResponse = 12.0f;

private float _pitchRate;
private float _yawRate;

#endregion

	#region Boost Settings

	[Export] public float BoostSpeedMultiplier = 2.2f;
	[Export] public float BoostAccelerationMultiplier = 1.8f;
	[Export] public float BoostResponse = 4.0f;

	#endregion

	#region Runtime

	private bool _rightCtrlHeld;
	private bool _destroyed;
	private Vector2 _mouseMovement;

	public bool IsBoosting { get; private set; }
	public float BoostAmount { get; private set; }

	// Steering signals are normalized for presentation, independent of frame rate.
	public float PitchInput { get; private set; }
	public float YawInput { get; private set; }
	public float StrafeInput { get; private set; }
	public float RollInput { get; private set; }

	public ShipDefence Defence { get; private set; }
	public event Action DefenceChanged;

	public Faction CombatFaction => Faction.Player;

	public bool IsCombatTargetable =>
		Defence != null
		&& !_destroyed
		&& !IsQueuedForDeletion();

	#endregion

	#region Godot Events

	// Creates collision, defence, HUD, and the shared flight presentation.
	public override void _Ready()
	{
		AddToGroup("player_ship");
		AddToGroup("combat_targets");

		BoxShape3D shape = new()
		{
			Size = new Vector3(3.8f, 1.0f, 3.8f)
		};

		CollisionShape3D collision = new()
		{
			Name = "Collision",
			Shape = shape,
			Position = new Vector3(0, 0.06f, -0.225f)
		};

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

			AddChild(new PlayerDefenceHud
			{
				Name = "PlayerDefenceHud"
			});
		}

		if (GetNodeOrNull<PlayerFlightVisuals>("FlightVisuals") == null)
		{
			AddChild(new PlayerFlightVisuals
			{
				Name = "FlightVisuals"
			});
		}

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// Handles mouse capture, relative steering, and right-Control descent.
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
				_rightCtrlHeld = false;
				return;
			}

			if (
				key.Keycode == KEY_DESCEND
				&& key.Location == KEY_DESCEND_LOCATION
				&& !key.Echo
			)
			{
				_rightCtrlHeld = key.Pressed;
			}
		}

		if (
			inputEvent is InputEventMouseButton button
			&& button.Pressed
			&& Input.MouseMode == Input.MouseModeEnum.Visible
		)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			return;
		}

		if (
			inputEvent is InputEventMouseMotion motion
			&& Input.MouseMode == Input.MouseModeEnum.Captured
		)
		{
			_mouseMovement += motion.ScreenRelative;
		}
	}

// =========================================================
// Clears held input and steering momentum when the application loses focus.
public override void _Notification(int what)
{
	if (what == NotificationApplicationFocusOut)
	{
		_rightCtrlHeld = false;
		_mouseMovement = Vector2.Zero;

		_pitchRate = 0.0f;
		_yawRate = 0.0f;

		PitchInput = 0.0f;
		YawInput = 0.0f;
		RollInput = 0.0f;
	}
}

	// Restores the pointer when leaving gameplay.
	public override void _ExitTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	// Updates steering, boost, and physical movement.
	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		if (_destroyed)
		{
			IsBoosting = false;
			BoostAmount = Mathf.MoveToward(
				BoostAmount,
				0.0f,
				BoostResponse * seconds
			);

			PitchInput = 0.0f;
			YawInput = 0.0f;
			StrafeInput = 0.0f;
			RollInput = 0.0f;
			return;
		}

		UpdateRotation(seconds);
		UpdateMovement(seconds);
	}

	#endregion

	#region Defence

	// Applies incoming damage through shield, armour, and hull.
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

	// =========================================================
// Limits and smooths mouse steering, then publishes the actual turning strength.
private void UpdateRotation(float seconds)
{
	bool controlsActive = Input.MouseMode == Input.MouseModeEnum.Captured;

	if (!controlsActive)
	{
		_pitchRate = 0.0f;
		_yawRate = 0.0f;

		PitchInput = 0.0f;
		YawInput = 0.0f;
		RollInput = 0.0f;

		_mouseMovement = Vector2.Zero;
		return;
	}

	float safeSeconds = Mathf.Max(seconds, 0.0001f);

	float maxPitchRate = Mathf.DegToRad(
		Mathf.Max(0.0f, MaxPitchSpeedDegrees)
	);

	float maxYawRate = Mathf.DegToRad(
		Mathf.Max(0.0f, MaxYawSpeedDegrees)
	);

	// Convert this tick's mouse displacement into requested angular velocity.
	float desiredPitchRate = Mathf.Clamp(
		_mouseMovement.Y * MousePitchSensitivity / safeSeconds,
		-maxPitchRate,
		maxPitchRate
	);

	float desiredYawRate = Mathf.Clamp(
		-_mouseMovement.X * MouseYawSensitivity / safeSeconds,
		-maxYawRate,
		maxYawRate
	);

	// Consume input immediately so excess mouse movement never builds a backlog.
	_mouseMovement = Vector2.Zero;

	float blend = 1.0f - Mathf.Exp(
		-Mathf.Max(0.0f, SteeringResponse) * seconds
	);

	_pitchRate = Mathf.Clamp(
		Mathf.Lerp(_pitchRate, desiredPitchRate, blend),
		-maxPitchRate,
		maxPitchRate
	);

	_yawRate = Mathf.Clamp(
		Mathf.Lerp(_yawRate, desiredYawRate, blend),
		-maxYawRate,
		maxYawRate
	);

	float keyboardRoll = 0.0f;

	if (Input.IsPhysicalKeyPressed(KEY_ROLL_LEFT)) keyboardRoll -= 1.0f;
	if (Input.IsPhysicalKeyPressed(KEY_ROLL_RIGHT)) keyboardRoll += 1.0f;

	RollInput = keyboardRoll;

	PitchInput = maxPitchRate > 0.0001f
		? _pitchRate / maxPitchRate
		: 0.0f;

	YawInput = maxYawRate > 0.0001f
		? _yawRate / maxYawRate
		: 0.0f;

	RotateObjectLocal(Vector3.Right, _pitchRate * seconds);
	RotateObjectLocal(Vector3.Up, _yawRate * seconds);

	RotateObjectLocal(
		Vector3.Forward,
		Mathf.DegToRad(keyboardRoll * RollSpeed) * seconds
	);
}

	// Applies normal flight or automatic forward thrust while Shift is held.
	private void UpdateMovement(float seconds)
	{
		float thrust = 0.0f;
		float strafe = 0.0f;
		float rise = 0.0f;

		bool controlsActive = Input.MouseMode == Input.MouseModeEnum.Captured;

		if (controlsActive)
		{
			if (Input.IsPhysicalKeyPressed(KEY_FORWARD)) thrust += 1.0f;
			if (Input.IsPhysicalKeyPressed(KEY_REVERSE)) thrust -= 1.0f;

			if (Input.IsPhysicalKeyPressed(KEY_STRAFE_LEFT)) strafe -= 1.0f;
			if (Input.IsPhysicalKeyPressed(KEY_STRAFE_RIGHT)) strafe += 1.0f;

			if (Input.IsMouseButtonPressed(MOUSE_ASCEND)) rise += 1.0f;
			if (_rightCtrlHeld) rise -= 1.0f;
		}

		IsBoosting =
			controlsActive
			&& Input.IsPhysicalKeyPressed(KEY_BOOST);

		BoostAmount = Mathf.MoveToward(
			BoostAmount,
			IsBoosting ? 1.0f : 0.0f,
			Mathf.Max(0.0f, BoostResponse) * seconds
		);

		StrafeInput = strafe;

		float forwardSpeed = thrust >= 0.0f ? ForwardSpeed : ReverseSpeed;

		if (IsBoosting)
		{
			thrust = 1.0f;
			forwardSpeed = ForwardSpeed * Mathf.Max(1.0f, BoostSpeedMultiplier);
		}

		Vector3 targetVelocity =
			-GlobalBasis.Z * thrust * forwardSpeed
			+ GlobalBasis.X * strafe * StrafeSpeed
			+ GlobalBasis.Y * rise * VerticalSpeed;

		float response = targetVelocity == Vector3.Zero
			? Deceleration
			: Acceleration;

		if (IsBoosting)
		{
			response *= Mathf.Max(1.0f, BoostAccelerationMultiplier);
		}

		Velocity = Velocity.MoveToward(
			targetVelocity,
			response * seconds
		);

		MoveAndSlide();
	}

	#endregion
}

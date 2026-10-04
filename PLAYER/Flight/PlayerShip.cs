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

		#region Final Stats

	public PlayerRuntimeStats Stats { get; private set; }

	// Existing flight and presentation systems now read cached final values.
	public float ForwardSpeed => Stats.Get(PlayerStat.ForwardSpeed);
	public float ReverseSpeed => Stats.Get(PlayerStat.ReverseSpeed);
	public float StrafeSpeed => Stats.Get(PlayerStat.StrafeSpeed);
	public float VerticalSpeed => Stats.Get(PlayerStat.VerticalSpeed);

	public float Acceleration => Stats.Get(PlayerStat.Acceleration);
	public float Deceleration => Stats.Get(PlayerStat.Deceleration);
	public float RollSpeed => Stats.Get(PlayerStat.RollSpeed);

	public float MousePitchSensitivity =>
		Stats.Get(PlayerStat.MousePitchSensitivity);

	public float MouseYawSensitivity =>
		Stats.Get(PlayerStat.MouseYawSensitivity);

	public float MaxPitchSpeedDegrees =>
		Stats.Get(PlayerStat.MaxPitchSpeedDegrees);

	public float MaxYawSpeedDegrees =>
		Stats.Get(PlayerStat.MaxYawSpeedDegrees);

	public float SteeringResponse =>
		Stats.Get(PlayerStat.SteeringResponse);

	public float BoostSpeedMultiplier =>
		Stats.Get(PlayerStat.BoostSpeedMultiplier);

	public float BoostAccelerationMultiplier =>
		Stats.Get(PlayerStat.BoostAccelerationMultiplier);

	public float BoostResponse =>
		Stats.Get(PlayerStat.BoostResponse);

	private CargoHold _cargo;

	#endregion

	#region Steering Runtime

	private float _pitchRate;
	private float _yawRate;

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
	private ShipShield _shield;

	public Faction CombatFaction => Faction.Player;

	public bool IsCombatTargetable =>
		Defence != null
		&& !_destroyed
		&& !IsQueuedForDeletion();

	private PlayerDash _dash;

	public bool IsDashing => _dash?.IsDashing == true;

	public bool CockpitInteractionActive { get; private set; }

	#endregion

	#region Resources and Systems

public PlayerResources Resources { get; private set; }
public ShipSystems Systems { get; private set; }

// =========================================================
// Creates independent resource and subsystem runtimes after final stats exist.
private void InitializeResources()
{
	Systems = new ShipSystems(Definition.Systems);
	Resources = new PlayerResources(Stats);
}

#endregion

	#region Godot Events

		// =========================================================
	// Creates runtime stats, defence, collision, and existing player presentation.
	public override void _Ready()
	{
		_dash = GetNodeOrNull<PlayerDash>("Dash");
		_cargo = GetNodeOrNull<CargoHold>("Cargo");

		if (Definition == null
			|| Definition.Defence == null
			|| Definition.Handling == null
			|| Definition.Boost == null
			|| Definition.Dash == null
			|| Definition.Cargo == null
			|| _cargo == null
			|| _cargo.Definition != Definition.Cargo)
		{
			GD.PushError(
				"PlayerShip requires all stat groups and a Cargo node "
				+ "using the same CargoDefinition as PlayerShipDefinition."
			);

			SetProcessInput(false);
			SetPhysicsProcess(false);
			_dash?.SetProcessInput(false);
			_dash?.SetPhysicsProcess(false);
			return;
		}

		Stats = new PlayerRuntimeStats(Definition);
		InitializeResources();

		Defence = new ShipDefence(
			Stats.Get(PlayerStat.MaxShield),
			Stats.Get(PlayerStat.MaxArmour),
			Stats.Get(PlayerStat.MaxHull)
		);

		Stats.Changed += ApplyFinalStats;
		ApplyFinalStats();

		AddToGroup("player_ship");
		AddToGroup("combat_targets");

		AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = new BoxShape3D
			{
				Size = new Vector3(3.8f, 1.0f, 3.8f)
			},
			Position = new Vector3(0.0f, 0.06f, -0.225f)
		});

		AddChild(new PlayerDefenceHud
		{
			Name = "PlayerDefenceHud"
		});

		if (GetNodeOrNull<PlayerFlightVisuals>("FlightVisuals") == null)
		{
			AddChild(new PlayerFlightVisuals
			{
				Name = "FlightVisuals"
			});
		}

		_shield = ShipShield.Attach(
			this,
			Defence,
			CombatFaction,
			Definition.ShieldVisuals
		);

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// =========================================================
// Handles flight mouse capture and steering while respecting cockpit UI control.
public override void _Input(InputEvent inputEvent)
{
	if (_destroyed || CockpitInteractionActive)
	{
		return;
	}

	if (inputEvent is InputEventKey key)
	{
		if (key.Keycode == KEY_RELEASE_MOUSE
			&& key.Pressed
			&& !key.Echo)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			_mouseMovement = Vector2.Zero;
			_rightCtrlHeld = false;
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

	// =========================================================
	// Disconnects runtime stat updates and restores the pointer when leaving gameplay.
	public override void _ExitTree()
	{
		if (Stats != null)
		{
			Stats.Changed -= ApplyFinalStats;
		}

		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

// =========================================================
// Updates ship resources and flight while respecting cockpit interaction.
public override void _PhysicsProcess(double delta)
{
	float seconds = (float)delta;

	if (_destroyed)
	{
		IsBoosting = false;
		BoostAmount = Mathf.MoveToward(
			BoostAmount, 0.0f, Mathf.Max(0.0f, BoostResponse) * seconds
		);

		PitchInput = 0.0f;
		YawInput = 0.0f;
		StrafeInput = 0.0f;
		RollInput = 0.0f;
		return;
	}

	Systems.Update(seconds);
	Resources.Update(seconds, Stats, Systems);

	if (CockpitInteractionActive)
	{
		IsBoosting = false;
		BoostAmount = Mathf.MoveToward(
			BoostAmount, 0.0f, Mathf.Max(0.0f, BoostResponse) * seconds
		);

		MoveAndSlide();
		return;
	}

	UpdateRotation(seconds);
	UpdateMovement(seconds);
}

// =========================================================
// Synchronizes capacities without repairing damage or refilling resources.
private void ApplyFinalStats()
{
	Defence.SetMaximums(
		Stats.Get(PlayerStat.MaxShield),
		Stats.Get(PlayerStat.MaxArmour),
		Stats.Get(PlayerStat.MaxHull)
	);

	_cargo.SetMaximumMass(Stats.Get(PlayerStat.CargoMaximumMass));
	Resources.Synchronize(Stats);

	DefenceChanged?.Invoke();
}

	#endregion

	#region Defence

	// =========================================================
	// Resolves typed defence damage and informs shield visuals and the HUD.
	public void ApplyDamage(DamageInfo damage)
	{
		if (Defence == null || _destroyed || damage.Amount <= 0.0f)
		{
			return;
		}

		float shieldBefore = Defence.Shield;

		Defence.ApplyDamage(damage.Amount, damage.Type);

		_shield?.NotifyDamage(shieldBefore, damage);
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

	// =========================================================
// Applies flight and boost fuel costs; preserves momentum when fuel runs out.
private void UpdateMovement(float seconds)
{
	if (IsDashing)
	{
		IsBoosting = false;
		StrafeInput = 0.0f;

		BoostAmount = Mathf.MoveToward(
			BoostAmount, 1.0f, Mathf.Max(0.0f, BoostResponse) * seconds
		);

		Velocity = _dash.Direction * _dash.DashSpeed;
		MoveAndSlide();

		if (GetSlideCollisionCount() > 0) _dash.EndDash();
		return;
	}

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

	IsBoosting = controlsActive && Input.IsPhysicalKeyPressed(KEY_BOOST);

	if (IsBoosting) thrust = 1.0f;

	bool requestingThrust =
		thrust != 0.0f || strafe != 0.0f || rise != 0.0f;

	// Boost uses its total rate rather than adding both rates together.
	float fuelRate = Stats.Get(
		IsBoosting ? PlayerStat.BoostFuelPerSecond : PlayerStat.ThrustFuelPerSecond
	);

	bool powered = Resources.Fuel > 0.0f;

	if (requestingThrust && powered)
	{
		powered = Resources.TrySpendFuel(fuelRate * seconds);

		// Insufficient boost fuel can still allow ordinary powered flight.
		if (!powered && IsBoosting)
		{
			IsBoosting = false;
			powered = Resources.TrySpendFuel(
				Stats.Get(PlayerStat.ThrustFuelPerSecond) * seconds
			);
		}
	}

	if (!powered) IsBoosting = false;

	BoostAmount = Mathf.MoveToward(
		BoostAmount, IsBoosting ? 1.0f : 0.0f,
		Mathf.Max(0.0f, BoostResponse) * seconds
	);

	StrafeInput = powered ? strafe : 0.0f;

	if (!powered)
	{
		// No propulsion or automatic braking; collision handling still runs.
		MoveAndSlide();
		return;
	}

	float forwardSpeed = thrust >= 0.0f ? ForwardSpeed : ReverseSpeed;

	if (IsBoosting)
		forwardSpeed = ForwardSpeed * Mathf.Max(1.0f, BoostSpeedMultiplier);

	Vector3 targetVelocity =
		-GlobalBasis.Z * thrust * forwardSpeed
		+ GlobalBasis.X * strafe * StrafeSpeed
		+ GlobalBasis.Y * rise * VerticalSpeed;

	float response = targetVelocity == Vector3.Zero
		? Deceleration : Acceleration;

	if (IsBoosting)
		response *= Mathf.Max(1.0f, BoostAccelerationMultiplier);

	Velocity = Velocity.MoveToward(
		targetVelocity, Mathf.Max(0.0f, response) * seconds
	);

	MoveAndSlide();
}

		// =========================================================
	// Chooses current travel, requested movement, or forward as the dash direction.
	public Vector3 GetDashDirection()
	{
		Vector3 travel = GetRealVelocity();

		if (travel.LengthSquared() > 1.0f)
		{
			return travel.Normalized();
		}

		float thrust = 0.0f;
		float strafe = 0.0f;
		float rise = 0.0f;

		if (Input.IsPhysicalKeyPressed(KEY_FORWARD)) thrust += 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_REVERSE)) thrust -= 1.0f;

		if (Input.IsPhysicalKeyPressed(KEY_STRAFE_LEFT)) strafe -= 1.0f;
		if (Input.IsPhysicalKeyPressed(KEY_STRAFE_RIGHT)) strafe += 1.0f;

		if (Input.IsMouseButtonPressed(MOUSE_ASCEND)) rise += 1.0f;
		if (_rightCtrlHeld) rise -= 1.0f;

		Vector3 requested =
			-GlobalBasis.Z * thrust
			+ GlobalBasis.X * strafe
			+ GlobalBasis.Y * rise;

		return requested.LengthSquared() > 0.0001f
			? requested.Normalized()
			: -GlobalBasis.Z.Normalized();
	}

	#endregion

	// =========================================================
// Transfers mouse control between flight and cockpit UI without stopping travel.
public void SetCockpitInteraction(bool active)
{
	CockpitInteractionActive = active && IsCombatTargetable;

	_mouseMovement = Vector2.Zero;
	_rightCtrlHeld = false;

	_pitchRate = 0.0f;
	_yawRate = 0.0f;

	PitchInput = 0.0f;
	YawInput = 0.0f;
	StrafeInput = 0.0f;
	RollInput = 0.0f;

	IsBoosting = false;

	if (CockpitInteractionActive)
	{
		_dash?.EndDash();
	}

	Input.MouseMode = CockpitInteractionActive || !IsCombatTargetable
		? Input.MouseModeEnum.Visible
		: Input.MouseModeEnum.Captured;
}

#region Progression

// Remains with this player instance during sector transitions.
public PlayerProgression Progression { get; } = new();

#endregion
}

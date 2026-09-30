using Godot;

// Detects double-tap Right Shift and controls a short direction-locked dash.
public partial class PlayerDash : Node
{
	#region Settings

	[ExportGroup("Dash Movement")]

	[Export] public float Speed = 90.0f;
	[Export] public float DurationSeconds = 0.22f;
	[Export] public float ExitSpeedMultiplier = 0.45f;

	[ExportGroup("Dash Input")]

	[Export] public float DoubleTapWindowSeconds = 0.25f;
	[Export] public float CooldownSeconds = 1.2f;

	#endregion

	#region Runtime

	private PlayerShip _ship;

	private float _tapRemaining;
	private float _dashRemaining;
	private float _cooldownRemaining;

	private bool _startRequested;

	public bool IsDashing { get; private set; }
	public Vector3 Direction { get; private set; }

	public float DashSpeed => Mathf.Max(0.0f, Speed);
	public float CooldownRemaining => _cooldownRemaining;

	#endregion

	#region Setup

	// =========================================================
	// Connects the owning ship and updates dash state before player movement.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		ProcessPhysicsPriority = -10;

		if (_ship == null)
		{
			GD.PushError(
				"PlayerDash must be a direct child of PlayerShip."
			);

			SetPhysicsProcess(false);
			SetProcessInput(false);
		}
	}

	#endregion

	#region Input

	// =========================================================
	// Counts distinct Right Shift presses while gameplay input is captured.
	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey key
			|| !key.Pressed
			|| key.Echo
			|| key.Keycode != Key.Shift
			|| key.Location != KeyLocation.Right)
		{
			return;
		}

		if (!GodotObject.IsInstanceValid(_ship)
			|| !_ship.IsCombatTargetable
			|| Input.MouseMode != Input.MouseModeEnum.Captured
			|| IsDashing
			|| _startRequested
			|| _cooldownRemaining > 0.0f)
		{
			return;
		}

		if (_tapRemaining > 0.0f)
		{
			_startRequested = true;
			_tapRemaining = 0.0f;
		}
		else
		{
			_tapRemaining = Mathf.Max(
				0.01f,
				DoubleTapWindowSeconds
			);
		}
	}

	// =========================================================
	// Cancels pending input and active dash when the application loses focus.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			_tapRemaining = 0.0f;
			_startRequested = false;

			EndDash();
		}
	}

	#endregion

	#region Dash State

	// =========================================================
	// Updates cooldown, starts requested dashes, and ends expired dashes.
	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		_cooldownRemaining = Mathf.Max(
			0.0f,
			_cooldownRemaining - seconds
		);

		_tapRemaining = Mathf.Max(
			0.0f,
			_tapRemaining - seconds
		);

		if (!_ship.IsCombatTargetable
			|| Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			_startRequested = false;
			_tapRemaining = 0.0f;

			EndDash();
			return;
		}

		if (IsDashing)
		{
			_dashRemaining -= seconds;

			if (_dashRemaining <= 0.0f)
			{
				EndDash();
			}
		}

		if (_startRequested)
		{
			_startRequested = false;

			if (!IsDashing && _cooldownRemaining <= 0.0f)
			{
				BeginDash();
			}
		}
	}

	// =========================================================
	// Locks the current travel direction and starts the burst and cooldown.
	private void BeginDash()
	{
		Direction = _ship.GetDashDirection();

		_dashRemaining = Mathf.Max(0.01f, DurationSeconds);

		_cooldownRemaining = Mathf.Max(
			0.0f,
			CooldownSeconds
		);

		IsDashing = true;

		_ship.Velocity = Direction * DashSpeed;
	}

	// =========================================================
	// Ends the burst and retains a configurable fraction of exit momentum.
	public void EndDash()
	{
		if (!IsDashing)
		{
			return;
		}

		IsDashing = false;
		_dashRemaining = 0.0f;

		if (GodotObject.IsInstanceValid(_ship))
		{
			_ship.Velocity *= Mathf.Clamp(
				ExitSpeedMultiplier,
				0.0f,
				1.0f
			);
		}
	}

	#endregion
}
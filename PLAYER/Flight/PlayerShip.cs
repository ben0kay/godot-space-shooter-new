using System;
using Godot;

// Simulates the player ship using flight commands supplied by an independent controller.
public partial class PlayerShip : CharacterBody3D, IDamageable, ICombatTarget
{
	#region Definition

	[Export] public PlayerShipDefinition Definition;

	public PlayerFlightInput FlightInput { get; private set; }

	#endregion

	#region Final Stats

	public PlayerRuntimeStats Stats { get; private set; }

	public float ForwardSpeed => Stats.Get(PlayerStat.ForwardSpeed);
	public float ReverseSpeed => Stats.Get(PlayerStat.ReverseSpeed);
	public float StrafeSpeed => Stats.Get(PlayerStat.StrafeSpeed);
	public float VerticalSpeed => Stats.Get(PlayerStat.VerticalSpeed);
	public float Acceleration => Stats.Get(PlayerStat.Acceleration);
	public float Deceleration => Stats.Get(PlayerStat.Deceleration);
	public float RollSpeed => Stats.Get(PlayerStat.RollSpeed);

	public float MousePitchSensitivity => Stats.Get(PlayerStat.MousePitchSensitivity);
	public float MouseYawSensitivity => Stats.Get(PlayerStat.MouseYawSensitivity);
	public float MaxPitchSpeedDegrees => Stats.Get(PlayerStat.MaxPitchSpeedDegrees);
	public float MaxYawSpeedDegrees => Stats.Get(PlayerStat.MaxYawSpeedDegrees);
	public float SteeringResponse => Stats.Get(PlayerStat.SteeringResponse);

	public float BoostSpeedMultiplier => Stats.Get(PlayerStat.BoostSpeedMultiplier);
	public float BoostAccelerationMultiplier => Stats.Get(PlayerStat.BoostAccelerationMultiplier);
	public float BoostResponse => Stats.Get(PlayerStat.BoostResponse);

	#endregion

	#region Runtime

	private CargoHold _cargo;
	private PlayerDash _dash;
	private ShipShield _shield;
	private bool _destroyed;

	private float _pitchRate;
	private float _yawRate;

	public bool IsBoosting { get; private set; }
	public float BoostAmount { get; private set; }

	public float PitchInput { get; private set; }
	public float YawInput { get; private set; }
	public float StrafeInput { get; private set; }
	public float RollInput { get; private set; }

	public bool IsDashing => _dash?.IsDashing == true;
	public bool CockpitInteractionActive { get; private set; }

	public ShipDefence Defence { get; private set; }
	public event Action DefenceChanged;

	public PlayerResources Resources { get; private set; }
	public ShipSystems Systems { get; private set; }
	public PlayerProgression Progression { get; } = new();

	public Faction CombatFaction => Faction.Player;

	public bool IsCombatTargetable =>
		Defence != null && !_destroyed && !IsQueuedForDeletion();

	#endregion

	#region Setup

	// =========================================================
	// Creates ship runtimes and connects the independent flight input component.
	public override void _Ready()
	{
		_dash = GetNodeOrNull<PlayerDash>("Dash");
		_cargo = GetNodeOrNull<CargoHold>("Cargo");
		FlightInput = GetNodeOrNull<PlayerFlightInput>("FlightInput");

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

			SetPhysicsProcess(false);
			FlightInput?.SetEnabled(false);
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

		if (FlightInput == null)
		{
			FlightInput = new PlayerFlightInput { Name = "FlightInput" };
			AddChild(FlightInput);
		}

		AddToGroup("player_ship");
		AddToGroup("combat_targets");

		AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = new BoxShape3D { Size = new Vector3(3.8f, 1.0f, 3.8f) },
			Position = new Vector3(0.0f, 0.06f, -0.225f)
		});

		AddChild(new PlayerDefenceHud { Name = "PlayerDefenceHud" });

		if (GetNodeOrNull<PlayerFlightVisuals>("FlightVisuals") == null)
			AddChild(new PlayerFlightVisuals { Name = "FlightVisuals" });

		_shield = ShipShield.Attach(
			this, Defence, CombatFaction, Definition.ShieldVisuals
		);

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// =========================================================
	// Creates independent resource and subsystem runtimes after final stats exist.
	private void InitializeResources()
	{
		Systems = new ShipSystems(Definition.Systems);
		Resources = new PlayerResources(Stats);
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

	// =========================================================
	// Disconnects runtime stat updates when the ship leaves gameplay.
	public override void _ExitTree()
	{
		if (Stats != null)
			Stats.Changed -= ApplyFinalStats;

		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	#endregion

	#region Simulation

	// =========================================================
	// Updates resources and ship physics regardless of who currently owns input.
	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		if (_destroyed)
		{
			IsBoosting = false;
			BoostAmount = Mathf.MoveToward(
				BoostAmount, 0.0f, Mathf.Max(0.0f, BoostResponse) * seconds
			);
			ResetSteering();
			return;
		}

		Systems.Update(seconds);
		Resources.Update(seconds, Stats, Systems);

		// Preserve the existing seated cockpit-screen interaction behaviour.
		if (CockpitInteractionActive)
		{
			IsBoosting = false;
			BoostAmount = Mathf.MoveToward(
				BoostAmount, 0.0f, Mathf.Max(0.0f, BoostResponse) * seconds
			);
			MoveAndSlide();
			return;
		}

		ShipFlightCommands commands = FlightInput.ReadCommands();

		UpdateRotation(seconds, commands);
		UpdateMovement(seconds, commands);
	}

	// =========================================================
	// Removes steering momentum without altering the ship's travel velocity.
	private void ResetSteering()
	{
		_pitchRate = 0.0f;
		_yawRate = 0.0f;

		PitchInput = 0.0f;
		YawInput = 0.0f;
		StrafeInput = 0.0f;
		RollInput = 0.0f;
	}

	// =========================================================
	// Clears steering when application focus is lost.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			FlightInput?.ClearControls();
			ResetSteering();
		}
	}

	#endregion

	#region Flight Movement

	// =========================================================
	// Applies limited and smoothed rotation from the supplied flight commands.
	private void UpdateRotation(float seconds, ShipFlightCommands commands)
	{
		if (!commands.Active)
		{
			ResetSteering();
			return;
		}

		float safeSeconds = Mathf.Max(seconds, 0.0001f);
		float maxPitchRate = Mathf.DegToRad(Mathf.Max(0.0f, MaxPitchSpeedDegrees));
		float maxYawRate = Mathf.DegToRad(Mathf.Max(0.0f, MaxYawSpeedDegrees));

		float desiredPitchRate = Mathf.Clamp(
			commands.MouseMotion.Y * MousePitchSensitivity / safeSeconds,
			-maxPitchRate, maxPitchRate
		);
		float desiredYawRate = Mathf.Clamp(
			-commands.MouseMotion.X * MouseYawSensitivity / safeSeconds,
			-maxYawRate, maxYawRate
		);

		float blend = 1.0f - Mathf.Exp(
			-Mathf.Max(0.0f, SteeringResponse) * seconds
		);

		_pitchRate = Mathf.Clamp(
			Mathf.Lerp(_pitchRate, desiredPitchRate, blend),
			-maxPitchRate, maxPitchRate
		);
		_yawRate = Mathf.Clamp(
			Mathf.Lerp(_yawRate, desiredYawRate, blend),
			-maxYawRate, maxYawRate
		);

		PitchInput = maxPitchRate > 0.0001f ? _pitchRate / maxPitchRate : 0.0f;
		YawInput = maxYawRate > 0.0001f ? _yawRate / maxYawRate : 0.0f;
		RollInput = commands.Roll;

		RotateObjectLocal(Vector3.Right, _pitchRate * seconds);
		RotateObjectLocal(Vector3.Up, _yawRate * seconds);
		RotateObjectLocal(
			Vector3.Forward, Mathf.DegToRad(commands.Roll * RollSpeed) * seconds
		);
	}

	// =========================================================
	// Simulates propulsion, boost, braking and collisions from flight commands.
	private void UpdateMovement(float seconds, ShipFlightCommands commands)
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

			if (GetSlideCollisionCount() > 0)
				_dash.EndDash();

			return;
		}

		float thrust = commands.Thrust;
		float strafe = commands.Strafe;
		float rise = commands.Rise;

		IsBoosting = commands.Active && commands.Boost;
		if (IsBoosting)
			thrust = 1.0f;

		bool requestingThrust = thrust != 0 || strafe != 0 || rise != 0;
		bool powered = Resources.Fuel > 0.0f;

		float fuelRate = Stats.Get(
			IsBoosting ? PlayerStat.BoostFuelPerSecond : PlayerStat.ThrustFuelPerSecond
		);

		if (requestingThrust && powered)
		{
			powered = Resources.TrySpendFuel(fuelRate * seconds);

			if (!powered && IsBoosting)
			{
				IsBoosting = false;
				powered = Resources.TrySpendFuel(
					Stats.Get(PlayerStat.ThrustFuelPerSecond) * seconds
				);
			}
		}

		if (!powered)
			IsBoosting = false;

		BoostAmount = Mathf.MoveToward(
			BoostAmount, IsBoosting ? 1.0f : 0.0f,
			Mathf.Max(0.0f, BoostResponse) * seconds
		);
		StrafeInput = powered ? strafe : 0.0f;

		if (!powered)
		{
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
	// Chooses travel, requested movement or forward as the dash direction.
	public Vector3 GetDashDirection()
	{
		Vector3 travel = GetRealVelocity();

		if (travel.LengthSquared() > 1.0f)
			return travel.Normalized();

		ShipFlightCommands commands = FlightInput.ReadMovementCommands();

		Vector3 requested =
			-GlobalBasis.Z * commands.Thrust
			+ GlobalBasis.X * commands.Strafe
			+ GlobalBasis.Y * commands.Rise;

		return requested.LengthSquared() > 0.0001f
			? requested.Normalized()
			: -GlobalBasis.Z.Normalized();
	}

	#endregion

	#region Cockpit Interaction

	// =========================================================
	// Transfers pointer control to seated cockpit screens without stopping travel.
	public void SetCockpitInteraction(bool active)
	{
		CockpitInteractionActive = active && IsCombatTargetable;
		FlightInput?.ClearControls();
		ResetSteering();
		IsBoosting = false;

		if (CockpitInteractionActive)
			_dash?.EndDash();

		Input.MouseMode = CockpitInteractionActive || !IsCombatTargetable
			? Input.MouseModeEnum.Visible
			: Input.MouseModeEnum.Captured;
	}

	#endregion

	#region Defence

	// =========================================================
	// Resolves typed damage and updates shield presentation and HUD readouts.
	public void ApplyDamage(DamageInfo damage)
	{
		if (Defence == null || _destroyed || damage.Amount <= 0.0f)
			return;

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
}

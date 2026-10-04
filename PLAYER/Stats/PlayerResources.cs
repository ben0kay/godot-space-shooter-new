using Godot;

// Owns spent player resources and energy recovery independently of base stats.
public sealed class PlayerResources
{
	#region Runtime

	public float Fuel { get; private set; }
	public float MaximumFuel { get; private set; }

	public float Energy { get; private set; }
	public float MaximumEnergy { get; private set; }

	private float _energyDelay;

	#endregion

	#region Capacities

	// =========================================================
	// Starts a new player with full fuel and energy.
	public PlayerResources(PlayerRuntimeStats stats)
	{
		Synchronize(stats);
		Fuel = MaximumFuel;
		Energy = MaximumEnergy;
	}

	// =========================================================
	// Updates upgraded capacities without refilling spent resources.
	public void Synchronize(PlayerRuntimeStats stats)
	{
		MaximumFuel = stats.Get(PlayerStat.MaximumFuel);
		MaximumEnergy = stats.Get(PlayerStat.MaximumEnergy);

		Fuel = Mathf.Clamp(Fuel, 0.0f, MaximumFuel);
		Energy = Mathf.Clamp(Energy, 0.0f, MaximumEnergy);
	}

	#endregion

	#region Consumption

	// =========================================================
	// Spends a complete fuel cost, or leaves fuel unchanged if insufficient.
	public bool TrySpendFuel(float amount)
	{
		if (!float.IsFinite(amount) || amount < 0.0f) return false;
		if (Fuel < amount) return false;

		Fuel -= amount;
		return true;
	}

	// =========================================================
	// Spends energy and restarts its regeneration delay.
	public bool TrySpendEnergy(float amount, float rechargeDelaySeconds)
	{
		if (!float.IsFinite(amount) || amount < 0.0f) return false;
		if (Energy < amount) return false;
		if (amount == 0.0f) return true;

		Energy -= amount;
		_energyDelay = Mathf.Max(0.0f, rechargeDelaySeconds);
		return true;
	}

	#endregion

	#region Recovery

	// =========================================================
	// Adds fuel through refuelling or future pickups.
	public void AddFuel(float amount)
	{
		if (!float.IsFinite(amount) || amount <= 0.0f) return;
		Fuel = Mathf.Min(MaximumFuel, Fuel + amount);
	}

	// =========================================================
	// Adds energy through a pickup or other explicit recovery.
	public void AddEnergy(float amount)
	{
		if (!float.IsFinite(amount) || amount <= 0.0f) return;
		Energy = Mathf.Min(MaximumEnergy, Energy + amount);
	}

	// =========================================================
	// Regenerates energy after its delay using the reactor's current output.
	public void Update(
		float seconds, PlayerRuntimeStats stats, ShipSystems systems
	)
	{
		if (seconds <= 0.0f) return;

		// Only regenerate for the portion of this tick after the delay expires.
		float recoverySeconds = Mathf.Max(0.0f, seconds - _energyDelay);
		_energyDelay = Mathf.Max(0.0f, _energyDelay - seconds);

		if (recoverySeconds <= 0.0f || Energy >= MaximumEnergy) return;

		float rate = stats.Get(PlayerStat.EnergyRegenerationPerSecond)
			* systems.GetEffectiveness(ShipSystem.Reactor);

		Energy = Mathf.Min(MaximumEnergy, Energy + rate * recoverySeconds);
	}

	#endregion
}
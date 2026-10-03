using Godot;

// Groups authored fuel and energy settings within the player definition.
[GlobalClass]
public partial class PlayerResourceStats : Resource
{
	#region Fuel

	[Export] public float MaximumFuel = 1000.0f;
	[Export] public float ThrustFuelPerSecond = 0.9f;

	// Total boost consumption; replaces the normal thrust rate.
	[Export] public float BoostFuelPerSecond = 3.6f;

	[Export] public float DashFuelCost = 8.0f;
	[Export] public float JumpFuelCost = 50.0f;

	#endregion

	#region Energy

	[Export] public float MaximumEnergy = 500.0f;
	[Export] public float EnergyRegenerationPerSecond = 12.0f;
	[Export] public float EnergyRechargeDelaySeconds = 0.75f;

	#endregion
}
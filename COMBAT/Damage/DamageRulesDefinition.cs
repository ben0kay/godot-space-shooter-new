using Godot;

// Stores the game's shared damage-type balance without holding ship runtime values.
[GlobalClass]
public partial class DamageRulesDefinition : Resource
{
	#region Types

	[Export] public DamageTypeStats Neutral = new();

	[Export] public DamageTypeStats Kinetic = new()
	{
		ShieldMultiplier = 0.45f,
		ArmourMultiplier = 1.2f,
		HullMultiplier = 1.0f
	};

	[Export] public DamageTypeStats Energy = new()
	{
		ShieldMultiplier = 1.3f,
		ArmourMultiplier = 0.75f,
		HullMultiplier = 1.0f
	};

	[Export] public DamageTypeStats Explosive = new()
	{
		ShieldMultiplier = 0.75f,
		ArmourMultiplier = 1.0f,
		HullMultiplier = 1.4f
	};

	[Export] public DamageTypeStats Electric = new()
	{
		ShieldMultiplier = 1.2f,
		ArmourMultiplier = 0.4f,
		HullMultiplier = 0.75f
	};

	[Export] public DamageTypeStats Thermal = new()
	{
		ShieldMultiplier = 0.4f,
		ArmourMultiplier = 1.1f,
		HullMultiplier = 1.25f
	};

	[Export] public DamageTypeStats Corrosive = new()
	{
		ShieldMultiplier = 0.5f,
		ArmourMultiplier = 1.5f,
		HullMultiplier = 1.2f
	};

	#endregion

	#region Lookup

	// =========================================================
	// Returns the configured multipliers for one damage type.
	public DamageTypeStats GetStats(DamageType type)
	{
		return type switch
		{
			DamageType.Kinetic => Kinetic,
			DamageType.Energy => Energy,
			DamageType.Explosive => Explosive,
			DamageType.Electric => Electric,
			DamageType.Thermal => Thermal,
			DamageType.Corrosive => Corrosive,
			_ => Neutral
		};
	}

	#endregion
}
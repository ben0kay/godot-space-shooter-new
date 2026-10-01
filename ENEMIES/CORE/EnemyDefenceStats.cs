using Godot;

// Groups an enemy type's defence capacities and shield recharge settings.
[GlobalClass]
public partial class EnemyDefenceStats : Resource
{
	#region Defence Layers

	[ExportGroup("Defence Layers")]

	[Export] public float MaxShield = 0.0f;
	[Export] public float MaxArmour = 0.0f;
	[Export] public float MaxHull = 24.0f;

	#endregion

	#region Shield Recharge

	[ExportGroup("Shield Recharge")]

	// Shield points restored per second; zero disables recharge.
	[Export] public float ShieldRechargeRate = 0.0f;

	#endregion
}
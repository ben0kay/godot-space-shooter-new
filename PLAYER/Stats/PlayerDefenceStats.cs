using Godot;

// Defines the player's starting defence capacities.
[GlobalClass]
public partial class PlayerDefenceStats : Resource
{
	#region Defence

	[Export] public float MaxShield = 100.0f;
	[Export] public float MaxArmour = 80.0f;
	[Export] public float MaxHull = 100.0f;

	#endregion
}
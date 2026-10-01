using Godot;

// Groups one damage type's multipliers against the three defence layers.
[GlobalClass]
public partial class DamageTypeStats : Resource
{
	#region Multipliers

	[Export] public float ShieldMultiplier = 1.0f;
	[Export] public float ArmourMultiplier = 1.0f;
	[Export] public float HullMultiplier = 1.0f;

	#endregion
}
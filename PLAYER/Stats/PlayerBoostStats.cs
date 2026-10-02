using Godot;

// Defines held-Shift boost strength and transition speed.
[GlobalClass]
public partial class PlayerBoostStats : Resource
{
	#region Boost

	[Export] public float SpeedMultiplier = 2.2f;
	[Export] public float AccelerationMultiplier = 1.8f;
	[Export] public float Response = 4.0f;

	#endregion
}
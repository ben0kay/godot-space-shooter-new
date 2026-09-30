using Godot;

// Defines weapon spread and how movement affects accuracy.
[GlobalClass]
public partial class WeaponAccuracySettings : Resource
{
	#region Spread

	// Spread values are cone half-angles in degrees.
	[Export] public float BaseSpreadDegrees = 0.3f;
	[Export] public float MovementSpreadDegrees = 3.0f;

	#endregion

	#region Movement Influence

	// Speed at which the full movement penalty is reached.
	[Export] public float FullSpreadSpeed = 90.0f;

	// Controls how quickly accuracy responds to speed changes.
	[Export] public float Response = 12.0f;

	#endregion
}
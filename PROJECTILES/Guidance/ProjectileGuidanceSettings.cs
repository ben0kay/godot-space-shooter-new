using Godot;

// Defines reusable homing settings without storing any projectile runtime state.
[GlobalClass]
public partial class ProjectileGuidanceSettings : Resource
{
	#region Target Acquisition

	[ExportGroup("Target Acquisition")]

	[Export] public float AcquireRange = 350.0f;

	// Full cone angle: 360 allows acquisition in every direction.
	[Export] public float LockAngleDegrees = 360.0f;

	[Export] public float ReacquireIntervalSeconds = 0.2f;

	// Keeps the assigned target until it dies or leaves acquisition range.
	[Export] public bool RetainAssignedTarget = true;

	#endregion

	#region Steering

	[ExportGroup("Steering")]

	// Maximum turning speed before GuidanceStrength is applied.
	[Export] public float TurnSpeedDegrees = 90.0f;

	// Zero disables steering; one uses the configured turning speed.
	[Export] public float GuidanceStrength = 1.0f;

	[Export] public float GuidanceDelaySeconds = 0.0f;

	#endregion

	#region Predictive Lead

	[ExportGroup("Predictive Lead")]

	// Zero aims at the target's current position.
	[Export] public float LeadStrength = 0.0f;

	// Prevents excessive prediction against distant or fast targets.
	[Export] public float MaxLeadSeconds = 1.5f;

	#endregion
}

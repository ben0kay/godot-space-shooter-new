using Godot;

// Defines player flight speeds, acceleration, and steering.
[GlobalClass]
public partial class PlayerHandlingStats : Resource
{
	#region Movement

	[ExportGroup("Movement")]

	[Export] public float ForwardSpeed = 18.0f;
	[Export] public float ReverseSpeed = 8.0f;
	[Export] public float StrafeSpeed = 12.0f;
	[Export] public float VerticalSpeed = 10.0f;

	[Export] public float Acceleration = 24.0f;
	[Export] public float Deceleration = 18.0f;

	#endregion

	#region Steering

	[ExportGroup("Steering")]

	[Export] public float RollSpeed = 75.0f;

	[Export] public float MousePitchSensitivity = 0.0008f;
	[Export] public float MouseYawSensitivity = 0.0008f;

	[Export] public float MaxPitchSpeedDegrees = 110.0f;
	[Export] public float MaxYawSpeedDegrees = 135.0f;
	[Export] public float SteeringResponse = 14.0f;

	#endregion
}
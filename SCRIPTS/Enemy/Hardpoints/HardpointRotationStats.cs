using Godot;

public enum HardpointRotationMode
{
	Fixed,
	Target
}

[GlobalClass]
public partial class HardpointRotationStats : Resource
{
	[Export] public HardpointRotationMode Mode = HardpointRotationMode.Target;

	[Export] public float TurnSpeedDegrees = 120.0f;

	// Limits measured from the mount's resting direction.
	// YawLimitDegrees = 65 allows 65 degrees to either side.
	[Export(PropertyHint.Range, "0,180,1")]
	public float YawLimitDegrees = 65.0f;

	[Export(PropertyHint.Range, "0,89,1")]
	public float PitchUpDegrees = 35.0f;

	[Export(PropertyHint.Range, "0,89,1")]
	public float PitchDownDegrees = 35.0f;

	[Export] public bool ReturnToRest = true;
}

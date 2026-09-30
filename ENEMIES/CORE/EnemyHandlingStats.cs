using Godot;

[GlobalClass]
public partial class EnemyHandlingStats : Resource
{
	[Export] public float MaxSpeed = 12.0f;
	[Export] public float Acceleration = 8.0f;
	[Export] public float Braking = 10.0f;
	[Export] public float TurnSpeedDegrees = 90.0f;

	[Export] public float PitchSpeedDegrees = 25.0f;
	[Export] public float MaxPitchDegrees = 15.0f;
	[Export] public float MaxBankDegrees = 10.0f;
	[Export] public float BankResponse = 3.0f;
}

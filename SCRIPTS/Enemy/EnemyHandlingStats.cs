using Godot;

[GlobalClass]
public partial class EnemyHandlingStats : Resource
{
	[Export] public float MaxSpeed = 12.0f;
	[Export] public float Acceleration = 8.0f;
	[Export] public float Braking = 10.0f;
	[Export] public float TurnSpeedDegrees = 90.0f;
}

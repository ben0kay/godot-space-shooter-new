using Godot;

// Defines double-tap dash movement, timing, and cooldown.
[GlobalClass]
public partial class PlayerDashStats : Resource
{
	#region Movement

	[ExportGroup("Movement")]

	[Export] public float Speed = 90.0f;
	[Export] public float DurationSeconds = 0.22f;
	[Export] public float ExitSpeedMultiplier = 0.45f;

	#endregion

	#region Timing

	[ExportGroup("Timing")]

	[Export] public float DoubleTapWindowSeconds = 0.25f;
	[Export] public float CooldownSeconds = 1.2f;

	#endregion
}
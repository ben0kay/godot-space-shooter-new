using Godot;

[GlobalClass]
public partial class HardpointDefinition : Resource
{
	[ExportGroup("Identity")]
	[Export] public string Key = "hardpoint";
	[Export] public string Group = "cannons";

	[ExportGroup("Placement")]
	[Export] public Vector3 Position = Vector3.Zero;
	[Export] public Vector3 RestRotationDegrees = Vector3.Zero;
	[Export] public Vector3 MuzzleOffset = new(0, 0, -1.4f);

	[ExportGroup("Aiming")]
	[Export] public HardpointRotationStats RotationStats = new();

	[ExportGroup("Visual")]
	[Export] public PackedScene VisualScene;

	// Used while designing ships with simple shapes.
	[Export] public bool ShowPlaceholderBarrel = true;
	[Export] public Vector3 BarrelOffset = new(0, 0, -0.8f);
	[Export] public Vector3 BarrelSize = new(0.26f, 0.26f, 1.15f);
}

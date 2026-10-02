using Godot;

// Describes the population, spacing, and layout of a procedural asteroid field.
[GlobalClass]
public partial class AsteroidFieldDefinition : Resource
{
	#region Population

	[ExportGroup("Small Asteroids")]

	[Export] public int SmallCount = 60;
	[Export] public Vector2 SmallRadiusRange = new(2.0f, 5.0f);
	[Export] public bool SmallDestructible = true;

	[ExportGroup("Medium Asteroids")]

	[Export] public int MediumCount = 30;
	[Export] public Vector2 MediumRadiusRange = new(8.0f, 15.0f);
	[Export] public bool MediumDestructible = true;

	[ExportGroup("Large Asteroids")]

	[Export] public int LargeCount = 10;
	[Export] public Vector2 LargeRadiusRange = new(22.0f, 38.0f);
	[Export] public bool LargeDestructible = false;

	#endregion

	#region Layout

	[ExportGroup("Field Layout")]

	// Half-size of the allowed volume, relative to the generator.
	[Export] public Vector3 HalfExtents = new(500.0f, 160.0f, 500.0f);

	[Export] public int ClusterCount = 4;

	// Half-size of each elliptical cluster.
	[Export] public Vector3 ClusterSpread = new(170.0f, 85.0f, 170.0f);

	#endregion

	#region Clearance

	[ExportGroup("Clearance")]

	// Minimum extra surface-to-surface spacing between generated rocks.
	[Export] public float MinimumGap = 8.0f;

	// Empty space around each sector arrival marker.
	[Export] public float ArrivalClearance = 90.0f;

	// Bounded placement attempts prevent impossible settings from hanging loading.
	[Export] public int AttemptsPerAsteroid = 200;

	#endregion
}
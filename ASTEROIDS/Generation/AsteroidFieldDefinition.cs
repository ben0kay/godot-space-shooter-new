using Godot;

// Describes the population, spacing, layout, and deposits of an asteroid field.
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

	[Export] public float MinimumGap = 8.0f;
	[Export] public float ArrivalClearance = 90.0f;

	// Bounded attempts prevent impossible settings from hanging generation.
	[Export] public int AttemptsPerAsteroid = 200;

	#endregion

	#region Deposits

	[ExportGroup("Resource Deposits")]

	[Export] public Godot.Collections.Array<ResourceDepositDefinition>
		DepositTypes = new();

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float DepositChance = 0.55f;

	[Export] public Vector2I DepositCountRange = new(1, 3);

	// Keeps the smallest rocks clear of deposits for this first pass.
	[Export] public float MinimumDepositAsteroidRadius = 6.0f;

	#endregion
}
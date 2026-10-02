using Godot;

// Defines one reusable asteroid field's shape, population, distribution,
// resource deposits and navigation influence.
[GlobalClass]
public partial class AsteroidFieldDefinition : Resource
{
    #region Field Types

    public enum FieldShape { Box, Ellipsoid, Ring }
    public enum FieldDistribution { Uniform, DenseCore, EdgeHeavy, Clustered }

    #endregion

    #region Shape

    [ExportGroup("Field Shape")]

    [Export] public FieldShape Shape = FieldShape.Box;

    // Half-size along the generator's local X, Y and Z axes.
    // Change these values to enlarge the field without enlarging its rocks.
    [Export] public Vector3 HalfExtents = new(500.0f, 160.0f, 500.0f);

    // Ring inner radius as a fraction of its outer radius.
    // Rings lie in local X/Z; HalfExtents.Y controls vertical thickness.
    [Export(PropertyHint.Range, "0,0.95,0.01")]
    public float RingInnerRadius = 0.55f;

    #endregion

    #region Distribution

    [ExportGroup("Distribution")]

    [Export] public FieldDistribution Distribution =
        FieldDistribution.Clustered;

    // Higher values bias DenseCore placement towards the centre.
    [Export(PropertyHint.Range, "0.1,6,0.1")]
    public float CoreStrength = 2.0f;

    // Where the outer band begins, measured from centre to edge.
    [Export(PropertyHint.Range, "0,0.95,0.01")]
    public float EdgeStart = 0.65f;

    // Probability of sampling the whole field instead of only its outer band.
    // Zero makes EdgeHeavy hollow; one gives uniform placement.
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float InteriorFill = 0.15f;

    [ExportSubgroup("Clusters")]

    [Export(PropertyHint.Range, "1,64,1")]
    public int ClusterCount = 4;

    // Half-size of each elliptical cluster.
    [Export] public Vector3 ClusterSpread = new(170.0f, 85.0f, 170.0f);

    #endregion

    #region Population

    [ExportGroup("Population")]

    // Multiplies all three requested counts.
    // 1 = normal, 0.5 = half, 2 = double.
    [Export(PropertyHint.Range, "0,5,0.05,or_greater")]
    public float DensityMultiplier = 1.0f;

    [ExportSubgroup("Small Asteroids")]

    [Export] public int SmallCount = 60;
    [Export] public Vector2 SmallRadiusRange = new(2.0f, 5.0f);
    [Export] public bool SmallDestructible = true;

    [ExportSubgroup("Medium Asteroids")]

    [Export] public int MediumCount = 30;
    [Export] public Vector2 MediumRadiusRange = new(8.0f, 15.0f);
    [Export] public bool MediumDestructible = true;

    [ExportSubgroup("Large Asteroids")]

    [Export] public int LargeCount = 10;
    [Export] public Vector2 LargeRadiusRange = new(22.0f, 38.0f);
    [Export] public bool LargeDestructible = false;

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

    [Export] public float MinimumDepositAsteroidRadius = 6.0f;

    // Multiplies the reserve inside each patch, not its visual size.
    // Zero disables deposit generation for this field.
    [Export(PropertyHint.Range, "0,5,0.05,or_greater")]
    public float ResourceRichness = 1.0f;

    #endregion

    #region Navigation

    [ExportGroup("Navigation")]

    // Advisory field density for AI; not a collision or pathfinding result.
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float NavigationDensity = 0.65f;

    [Export] public bool LargeShipsAvoidField = true;

    // Reserved for the future large-ship movement controller.
    [Export] public float LargeShipClearance = 100.0f;

    #endregion
}
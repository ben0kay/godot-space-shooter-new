using System.Collections.Generic;
using Godot;

// Generates a seeded asteroid field once and retains its logical boundary
// for future navigation queries.
public partial class AsteroidFieldGenerator : Node3D
{
    #region Configuration

    [ExportGroup("Assets")]

    [Export] public PackedScene AsteroidScene;
    [Export] public AsteroidFieldDefinition Definition;

    [ExportGroup("Identity And Seed")]

    // Keep this unique among fields within the same sector.
    [Export] public string FieldKey = "belt_main";

    [Export] public bool UseSectorSeed = true;
    [Export] public int FallbackSeed = 2;
    [Export] public int SeedOffset = 0;

    #endregion

    #region Runtime

    private Node3D _generated;

    private readonly List<Vector3> _clusterCentres = new();
    private readonly List<Vector3> _arrivalPositions = new();
    private readonly List<Placement> _placements = new();

    private Vector3 _halfExtents;
    private Vector3 _clusterSpread;

    public int SpawnedCount { get; private set; }

    public int RemainingCount =>
        GodotObject.IsInstanceValid(_generated)
            ? _generated.GetChildCount()
            : 0;

    private readonly struct Placement
    {
        public readonly Vector3 Position;
        public readonly float Radius;

        // =========================================================
        // Stores conservative occupied bounds for generation spacing.
        // =========================================================
        public Placement(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }

    #endregion

    #region Setup

    // =========================================================
    // Waits for the sector hierarchy before generating its content.
    // =========================================================
    public override void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);

        CallDeferred(nameof(Generate));
    }

    // =========================================================
    // Creates one deterministic field with larger rocks placed first.
    // =========================================================
    public void Generate()
    {
        if (!IsInsideTree()
            || IsQueuedForDeletion()
            || _generated != null)
        {
            return;
        }

        if (AsteroidScene == null || Definition == null)
        {
            GD.PushError(
                $"{Name}: assign AsteroidScene and AsteroidFieldDefinition."
            );

            return;
        }

        _halfExtents = PositiveExtents(Definition.HalfExtents);
        _clusterSpread = PositiveExtents(Definition.ClusterSpread);

        RandomNumberGenerator random = new()
        {
            Seed = ResolveSeed()
        };

        _generated = new Node3D
        {
            Name = "GeneratedAsteroids"
        };

        AddChild(_generated);

        CacheArrivalPositions();

        if (Definition.Distribution
            == AsteroidFieldDefinition.FieldDistribution.Clustered)
        {
            CreateClusterCentres(random);
        }

        int large = SpawnPopulation(
            random,
            "large",
            Definition.LargeCount,
            Definition.LargeRadiusRange,
            Definition.LargeDestructible
        );

        int medium = SpawnPopulation(
            random,
            "medium",
            Definition.MediumCount,
            Definition.MediumRadiusRange,
            Definition.MediumDestructible
        );

        int small = SpawnPopulation(
            random,
            "small",
            Definition.SmallCount,
            Definition.SmallRadiusRange,
            Definition.SmallDestructible
        );

        SpawnedCount = small + medium + large;

        AddToGroup("asteroid_fields");

        GD.Print(
            $"Field '{FieldKey}': {Definition.Shape}, "
            + $"{Definition.Distribution} — "
            + $"{small} small, {medium} medium, {large} large. "
            + $"Total {SpawnedCount}."
        );
    }

    // =========================================================
    // Sanitises field dimensions without modifying the resource.
    // =========================================================
    private static Vector3 PositiveExtents(Vector3 value)
    {
        return new Vector3(
            Mathf.Max(1.0f, value.X),
            Mathf.Max(1.0f, value.Y),
            Mathf.Max(1.0f, value.Z)
        );
    }

    // =========================================================
    // Resolves the sector seed and this field's independent offset.
    // =========================================================
    private ulong ResolveSeed()
    {
        int seed = FallbackSeed;

        if (UseSectorSeed)
        {
            SectorManager manager =
                GetTree().GetFirstNodeInGroup(
                    "sector_manager"
                ) as SectorManager;

            WorldSector sector = FindSector();

            if (GodotObject.IsInstanceValid(manager)
                && manager.CurrentSector == sector
                && manager.CurrentDefinition != null)
            {
                seed = manager.CurrentDefinition.GenerationSeed;
            }
        }

        return unchecked(
            (ulong)(uint)seed
            + (ulong)(uint)SeedOffset * 0x9E3779B9UL
        );
    }

    // =========================================================
    // Finds the owning sector through this node's ancestors.
    // =========================================================
    private WorldSector FindSector()
    {
        Node ancestor = GetParent();

        while (ancestor != null)
        {
            if (ancestor is WorldSector sector)
            {
                return sector;
            }

            ancestor = ancestor.GetParent();
        }

        return null;
    }

    // =========================================================
    // Converts arrival markers into field-local clearance positions.
    // =========================================================
    private void CacheArrivalPositions()
    {
        Node arrivals = FindSector()?.GetNodeOrNull<Node>("Arrivals");

        if (arrivals == null)
        {
            return;
        }

        foreach (Node child in arrivals.GetChildren())
        {
            if (child is Marker3D marker)
            {
                _arrivalPositions.Add(ToLocal(marker.GlobalPosition));
            }
        }
    }

    // =========================================================
    // Places cluster centres inside the selected field shape.
    // =========================================================
    private void CreateClusterCentres(RandomNumberGenerator random)
    {
        int count = Mathf.Clamp(Definition.ClusterCount, 1, 64);

        for (int index = 0; index < count; index++)
        {
            Vector3 centre = SampleUniformShape(random);

            if (Definition.Shape
                != AsteroidFieldDefinition.FieldShape.Ring)
            {
                centre *= new Vector3(0.65f, 0.4f, 0.65f);
            }

            _clusterCentres.Add(centre);
        }
    }

    #endregion

    #region Population

    // =========================================================
    // Places one size population with stable IDs and surface deposits.
    // Impossible placements stop after the configured attempt limit.
    // =========================================================
    private int SpawnPopulation(
        RandomNumberGenerator random,
        string sizeKey,
        int requestedCount,
        Vector2 radiusRange,
        bool destructible
    )
    {
        int count = Mathf.RoundToInt(
            Mathf.Max(0, requestedCount)
            * Mathf.Max(0.0f, Definition.DensityMultiplier)
        );

        float minimum = Mathf.Max(
            0.1f,
            Mathf.Min(radiusRange.X, radiusRange.Y)
        );

        float maximum = Mathf.Max(
            minimum,
            Mathf.Max(radiusRange.X, radiusRange.Y)
        );

        int attempts = Mathf.Max(1, Definition.AttemptsPerAsteroid);
        int spawned = 0;

        for (int index = 0; index < count; index++)
        {
            float radius = random.RandfRange(minimum, maximum);
            float placementRadius = radius * 1.8f;

            Vector3 position = Vector3.Zero;
            bool foundPosition = false;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                position = SamplePosition(random);

                if (CanPlace(position, placementRadius))
                {
                    foundPosition = true;
                    break;
                }
            }

            if (!foundPosition)
            {
                GD.PushWarning(
                    $"Field '{FieldKey}' could not place "
                    + $"{sizeKey} asteroid {index}. "
                    + "Increase space, widen the distribution, "
                    + "or reduce population/spacing."
                );

                continue;
            }

            Node instance = AsteroidScene.Instantiate();

            if (instance is not Asteroid asteroid)
            {
                instance.Free();

                GD.PushError(
                    "AsteroidScene must have an Asteroid.cs root."
                );

                return spawned;
            }

            ulong shapeSeed =
                ((ulong)random.Randi() << 32) | random.Randi();

            asteroid.Name = $"{sizeKey}_{index:D3}";
            asteroid.PersistentId = $"{FieldKey}/{sizeKey}/{index:D3}";

            asteroid.Destructible = destructible;
            asteroid.Position = position;

            asteroid.Rotation = new Vector3(
                random.RandfRange(0.0f, Mathf.Tau),
                random.RandfRange(0.0f, Mathf.Tau),
                random.RandfRange(0.0f, Mathf.Tau)
            );

            asteroid.Configure(radius, shapeSeed);
            _generated.AddChild(asteroid);

            AsteroidDepositGenerator.Populate(
                asteroid,
                Definition,
                shapeSeed
            );

            _placements.Add(new Placement(position, placementRadius));
            spawned++;
        }

        return spawned;
    }

    // =========================================================
    // Rejects shape crossings, blocked arrivals and overlapping rocks.
    // =========================================================
    private bool CanPlace(Vector3 position, float radius)
    {
        if (!ContainsLocalSphere(position, radius))
        {
            return false;
        }

        float arrivalClearance = Mathf.Max(
            0.0f,
            Definition.ArrivalClearance
        );

        foreach (Vector3 arrival in _arrivalPositions)
        {
            float clearance = radius + arrivalClearance;

            if (position.DistanceSquaredTo(arrival)
                < clearance * clearance)
            {
                return false;
            }
        }

        float gap = Mathf.Max(0.0f, Definition.MinimumGap);

        foreach (Placement existing in _placements)
        {
            float clearance = radius + existing.Radius + gap;

            if (position.DistanceSquaredTo(existing.Position)
                < clearance * clearance)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Distribution Sampling

    // =========================================================
    // Applies the selected distribution to candidates inside the field.
    // =========================================================
    private Vector3 SamplePosition(RandomNumberGenerator random)
    {
        switch (Definition.Distribution)
        {
            case AsteroidFieldDefinition.FieldDistribution.Clustered:
            {
                Vector3 centre = _clusterCentres[
                    random.RandiRange(0, _clusterCentres.Count - 1)
                ];

                return centre + SampleUnitBall(random) * _clusterSpread;
            }

            case AsteroidFieldDefinition.FieldDistribution.DenseCore:
            {
                Vector3 point = SampleUniformShape(random);

                float strength = Mathf.Max(0.1f, Definition.CoreStrength);
                float compression = Mathf.Pow(random.Randf(), strength);

                if (Definition.Shape
                    == AsteroidFieldDefinition.FieldShape.Ring)
                {
                    // Bias towards the ring's inner edge, preserving its hole.
                    float radial = HorizontalRadius(point);
                    float inner = Mathf.Clamp(
                        Definition.RingInnerRadius,
                        0.0f,
                        0.95f
                    );

                    float target = Mathf.Lerp(inner, radial, compression);

                    if (radial > 0.0001f)
                    {
                        float ratio = target / radial;
                        point.X *= ratio;
                        point.Z *= ratio;
                    }

                    point.Y *= compression;
                    return point;
                }

                return point * compression;
            }

            case AsteroidFieldDefinition.FieldDistribution.EdgeHeavy:
            {
                Vector3 point = SampleUniformShape(random);

                if (random.Randf() < Mathf.Clamp(
                    Definition.InteriorFill,
                    0.0f,
                    1.0f
                ))
                {
                    return point;
                }

                float radial = DistributionRadius(point);

                if (radial <= 0.0001f)
                {
                    return point;
                }

                float start = Mathf.Clamp(
                    Definition.EdgeStart,
                    0.0f,
                    0.95f
                );

                if (Definition.Shape
                    == AsteroidFieldDefinition.FieldShape.Ring)
                {
                    start = Mathf.Max(
                        start,
                        Mathf.Clamp(
                            Definition.RingInnerRadius,
                            0.0f,
                            0.95f
                        )
                    );
                }

                float target = Mathf.Lerp(start, 1.0f, radial);
                float ratio = target / radial;

                if (Definition.Shape
                    == AsteroidFieldDefinition.FieldShape.Ring)
                {
                    point.X *= ratio;
                    point.Z *= ratio;
                    return point;
                }

                return point * ratio;
            }

            default:
                return SampleUniformShape(random);
        }
    }

    // =========================================================
    // Samples uniformly within a box, ellipsoid or elliptical ring.
    // =========================================================
    private Vector3 SampleUniformShape(RandomNumberGenerator random)
    {
        switch (Definition.Shape)
        {
            case AsteroidFieldDefinition.FieldShape.Ellipsoid:
                return SampleUnitBall(random) * _halfExtents;

            case AsteroidFieldDefinition.FieldShape.Ring:
            {
                float inner = Mathf.Clamp(
                    Definition.RingInnerRadius,
                    0.0f,
                    0.95f
                );

                float radius = Mathf.Sqrt(
                    Mathf.Lerp(inner * inner, 1.0f, random.Randf())
                );

                float angle = random.RandfRange(0.0f, Mathf.Tau);

                return new Vector3(
                    Mathf.Cos(angle) * radius * _halfExtents.X,
                    random.RandfRange(-_halfExtents.Y, _halfExtents.Y),
                    Mathf.Sin(angle) * radius * _halfExtents.Z
                );
            }

            default:
                return new Vector3(
                    random.RandfRange(-_halfExtents.X, _halfExtents.X),
                    random.RandfRange(-_halfExtents.Y, _halfExtents.Y),
                    random.RandfRange(-_halfExtents.Z, _halfExtents.Z)
                );
        }
    }

    // =========================================================
    // Samples a unit sphere uniformly throughout its volume.
    // =========================================================
    private static Vector3 SampleUnitBall(RandomNumberGenerator random)
    {
        float vertical = random.RandfRange(-1.0f, 1.0f);
        float angle = random.RandfRange(0.0f, Mathf.Tau);

        float horizontal = Mathf.Sqrt(
            Mathf.Max(0.0f, 1.0f - vertical * vertical)
        );

        float radius = Mathf.Pow(random.Randf(), 1.0f / 3.0f);

        return new Vector3(
            horizontal * Mathf.Cos(angle),
            vertical,
            horizontal * Mathf.Sin(angle)
        ) * radius;
    }

    // =========================================================
    // Returns normalized distance from centre to the selected shape's edge.
    // =========================================================
    private float DistributionRadius(Vector3 point)
    {
        Vector3 normalized = point / _halfExtents;

        return Definition.Shape switch
        {
            AsteroidFieldDefinition.FieldShape.Box =>
                Mathf.Max(
                    Mathf.Abs(normalized.X),
                    Mathf.Max(
                        Mathf.Abs(normalized.Y),
                        Mathf.Abs(normalized.Z)
                    )
                ),

            AsteroidFieldDefinition.FieldShape.Ring =>
                HorizontalRadius(point),

            _ => normalized.Length()
        };
    }

    // =========================================================
    // Returns normalized radius through the ring's local X/Z plane.
    // =========================================================
    private float HorizontalRadius(Vector3 point)
    {
        float x = point.X / _halfExtents.X;
        float z = point.Z / _halfExtents.Z;

        return Mathf.Sqrt(x * x + z * z);
    }

    #endregion

    #region Field Boundaries

    // =========================================================
    // Checks whether conservative rock bounds fit inside the field.
    // Ellipsoid and ring checks deliberately leave safe edge clearance.
    // =========================================================
    private bool ContainsLocalSphere(Vector3 point, float radius)
    {
        if (Definition.Shape == AsteroidFieldDefinition.FieldShape.Box)
        {
            return Mathf.Abs(point.X) + radius <= _halfExtents.X
                && Mathf.Abs(point.Y) + radius <= _halfExtents.Y
                && Mathf.Abs(point.Z) + radius <= _halfExtents.Z;
        }

        if (Definition.Shape
            == AsteroidFieldDefinition.FieldShape.Ellipsoid)
        {
            float minimumExtent = Mathf.Min(
                _halfExtents.X,
                Mathf.Min(_halfExtents.Y, _halfExtents.Z)
            );

            return (point / _halfExtents).Length()
                + radius / minimumExtent <= 1.0f;
        }

        float radial = HorizontalRadius(point);

        float margin = radius / Mathf.Min(
            _halfExtents.X,
            _halfExtents.Z
        );

        float inner = Mathf.Clamp(
            Definition.RingInnerRadius,
            0.0f,
            0.95f
        );

        return Mathf.Abs(point.Y) + radius <= _halfExtents.Y
            && radial + margin <= 1.0f
            && (inner <= 0.0f || radial - margin >= inner);
    }

    // =========================================================
    // Tests a world position against this field's logical shape.
    // It does not test individual asteroid collisions.
    // =========================================================
    public bool ContainsWorldPoint(Vector3 worldPosition)
    {
        return Definition != null
            && GodotObject.IsInstanceValid(_generated)
            && ContainsLocalSphere(ToLocal(worldPosition), 0.0f);
    }

    // =========================================================
    // Returns advisory navigation density, reduced as rocks are destroyed.
    // Future AI should combine this with local obstacle checks.
    // =========================================================
    public float GetNavigationDensity(Vector3 worldPosition)
    {
        if (SpawnedCount <= 0 || !ContainsWorldPoint(worldPosition))
        {
            return 0.0f;
        }

        float remaining = Mathf.Clamp(
            (float)RemainingCount / SpawnedCount,
            0.0f,
            1.0f
        );

        float density = Mathf.Clamp(
            Definition.NavigationDensity,
            0.0f,
            1.0f
        );

        float radial = Mathf.Clamp(
            DistributionRadius(ToLocal(worldPosition)),
            0.0f,
            1.0f
        );

        if (Definition.Distribution
            == AsteroidFieldDefinition.FieldDistribution.DenseCore)
        {
            density *= Mathf.Lerp(1.0f, 0.25f, radial);
        }
        else if (Definition.Distribution
            == AsteroidFieldDefinition.FieldDistribution.EdgeHeavy
            && radial < Definition.EdgeStart)
        {
            density *= Mathf.Clamp(
                Definition.InteriorFill,
                0.0f,
                1.0f
            );
        }

        return density * remaining;
    }

    #endregion
}
using System.Collections.Generic;
using Godot;

// Generates a repeatable clustered asteroid population once when a sector loads.
public partial class AsteroidFieldGenerator : Node3D
{
	#region Configuration

	[ExportGroup("Assets")]

	[Export] public PackedScene AsteroidScene;
	[Export] public AsteroidFieldDefinition Definition;

	[ExportGroup("Identity And Seed")]

	// Keep this unique among generators within the same sector.
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

	private readonly struct Placement
	{
		public readonly Vector3 Position;
		public readonly float Radius;

		// =========================================================
		// Stores a conservative occupied sphere for generation-time spacing.
		public Placement(Vector3 position, float radius)
		{
			Position = position;
			Radius = radius;
		}
	}

	#endregion

	#region Setup

	// =========================================================
	// Waits for the sector hierarchy before generating its population.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		CallDeferred(nameof(Generate));
	}

	// =========================================================
	// Generates the configured population once with a repeatable random sequence.
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
				$"{Name}: assign AsteroidScene and an AsteroidFieldDefinition."
			);

			return;
		}

		_halfExtents = new Vector3(
			Mathf.Max(1.0f, Definition.HalfExtents.X),
			Mathf.Max(1.0f, Definition.HalfExtents.Y),
			Mathf.Max(1.0f, Definition.HalfExtents.Z)
		);

		_clusterSpread = new Vector3(
			Mathf.Max(1.0f, Definition.ClusterSpread.X),
			Mathf.Max(1.0f, Definition.ClusterSpread.Y),
			Mathf.Max(1.0f, Definition.ClusterSpread.Z)
		);

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
		CreateClusterCentres(random);

		// Reserve space for the largest obstacles before placing smaller rocks.
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

		GD.Print(
			$"Asteroid field '{FieldKey}': "
			+ $"{small} small, {medium} medium, {large} large "
			+ $"— total {small + medium + large}."
		);
	}

	// =========================================================
	// Uses the active sector seed with a configurable offset for each field.
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
	// Finds the sector that owns this generator.
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
	// Converts sector arrival positions into generator-local clearance centres.
	private void CacheArrivalPositions()
	{
		WorldSector sector = FindSector();
		Node arrivals = sector?.GetNodeOrNull<Node>("Arrivals");

		if (arrivals == null)
		{
			return;
		}

		foreach (Node child in arrivals.GetChildren())
		{
			if (child is Marker3D marker)
			{
				_arrivalPositions.Add(
					ToLocal(marker.GlobalPosition)
				);
			}
		}
	}

	// =========================================================
	// Places repeatable cluster centres inside the field volume.
	private void CreateClusterCentres(RandomNumberGenerator random)
	{
		int count = Mathf.Clamp(Definition.ClusterCount, 1, 64);

		for (int index = 0; index < count; index++)
		{
			_clusterCentres.Add(new Vector3(
				random.RandfRange(
					-_halfExtents.X * 0.65f,
					_halfExtents.X * 0.65f
				),
				random.RandfRange(
					-_halfExtents.Y * 0.40f,
					_halfExtents.Y * 0.40f
				),
				random.RandfRange(
					-_halfExtents.Z * 0.65f,
					_halfExtents.Z * 0.65f
				)
			));
		}
	}

	#endregion

	#region Placement

	// =========================================================
	// Places one size population with bounded attempts and stable asteroid IDs.
	private int SpawnPopulation(
		RandomNumberGenerator random,
		string sizeKey,
		int requestedCount,
		Vector2 radiusRange,
		bool destructible
	)
	{
		int count = Mathf.Max(0, requestedCount);

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
					+ "Increase field space or reduce population/spacing."
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
				((ulong)random.Randi() << 32)
				| random.Randi();

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

			// Configure before AddChild because AddChild triggers asteroid _Ready.
			_generated.AddChild(asteroid);

			_placements.Add(new Placement(position, placementRadius));
			spawned++;
		}

		return spawned;
	}

	// =========================================================
	// Samples an elliptical cluster with uniform distribution through its volume.
	private Vector3 SamplePosition(RandomNumberGenerator random)
	{
		Vector3 centre = _clusterCentres[
			random.RandiRange(0, _clusterCentres.Count - 1)
		];

		float vertical = random.RandfRange(-1.0f, 1.0f);
		float angle = random.RandfRange(0.0f, Mathf.Tau);

		float horizontal = Mathf.Sqrt(
			Mathf.Max(0.0f, 1.0f - vertical * vertical)
		);

		float distance = Mathf.Pow(random.Randf(), 1.0f / 3.0f);

		Vector3 point = new(
			horizontal * Mathf.Cos(angle),
			vertical,
			horizontal * Mathf.Sin(angle)
		);

		return centre + new Vector3(
			point.X * _clusterSpread.X,
			point.Y * _clusterSpread.Y,
			point.Z * _clusterSpread.Z
		) * distance;
	}

	// =========================================================
	// Rejects field-edge crossings, blocked arrivals, and overlapping rock bounds.
	private bool CanPlace(Vector3 position, float radius)
	{
		if (Mathf.Abs(position.X) + radius > _halfExtents.X
			|| Mathf.Abs(position.Y) + radius > _halfExtents.Y
			|| Mathf.Abs(position.Z) + radius > _halfExtents.Z)
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
}
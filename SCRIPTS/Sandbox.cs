using System.Collections.Generic;
using Godot;

public partial class Sandbox : Node3D
{
	
#region Settings

[Export] public PackedScene AsteroidScene;
[Export] public int AsteroidCount = 100;
[Export] public ulong AsteroidSeed = 12345;

// Room dimensions
[Export] public float RoomWidth = 1600.0f;
[Export] public float RoomHeight = 600.0f;
[Export] public float RoomDepth = 1600.0f;

// Asteroid sizes
[Export] public float SmallAsteroidMinRadius = 2.0f;
[Export] public float SmallAsteroidMaxRadius = 7.0f;

[Export] public float MediumAsteroidMinRadius = 9.0f;
[Export] public float MediumAsteroidMaxRadius = 24.0f;

[Export] public float LargeAsteroidMinRadius = 40.0f;
[Export] public float LargeAsteroidMaxRadius = 75.0f;

// Number of special-sized asteroids
[Export] public int MediumAsteroidCount = 22;
[Export] public int LargeAsteroidCount = 8;

#endregion

#region Space Appearance

[Export] public float StarBrightness = 1.4f;
[Export] public float NebulaStrength = 0.025f;

[Export] public float SunEnergy = 1.2f;
[Export] public float AmbientEnergy = 0.18f;

#endregion


	#region Godot Events

	// =========================================================
// Creates finite invisible boundaries, the space environment, lighting, and asteroids.
public override void _Ready()
{
	CreateRoom();
	CreateSpaceEnvironment();
	CreateLighting();
	SpawnAsteroids();
}
	#endregion

	#region Room

	// =========================================================
// Creates six invisible collision boundaries around the playable volume.
private void CreateRoom()
{
	float halfWidth = RoomWidth * 0.5f;
	float halfHeight = RoomHeight * 0.5f;
	float halfDepth = RoomDepth * 0.5f;

	CreateBoundary(
		"Floor",
		new Vector3(0.0f, -halfHeight - 0.5f, 0.0f),
		new Vector3(RoomWidth, 1.0f, RoomDepth)
	);

	CreateBoundary(
		"Ceiling",
		new Vector3(0.0f, halfHeight + 0.5f, 0.0f),
		new Vector3(RoomWidth, 1.0f, RoomDepth)
	);

	CreateBoundary(
		"NorthWall",
		new Vector3(0.0f, 0.0f, -halfDepth - 0.5f),
		new Vector3(RoomWidth, RoomHeight, 1.0f)
	);

	CreateBoundary(
		"SouthWall",
		new Vector3(0.0f, 0.0f, halfDepth + 0.5f),
		new Vector3(RoomWidth, RoomHeight, 1.0f)
	);

	CreateBoundary(
		"WestWall",
		new Vector3(-halfWidth - 0.5f, 0.0f, 0.0f),
		new Vector3(1.0f, RoomHeight, RoomDepth)
	);

	CreateBoundary(
		"EastWall",
		new Vector3(halfWidth + 0.5f, 0.0f, 0.0f),
		new Vector3(1.0f, RoomHeight, RoomDepth)
	);
}

// =========================================================
// Creates a collision-only boundary without a visible wall mesh.
private void CreateBoundary(
	string name,
	Vector3 position,
	Vector3 size
)
{
	StaticBody3D body = new StaticBody3D
	{
		Name = name,
		Position = position
	};

	AddChild(body);

	body.AddChild(new CollisionShape3D
	{
		Name = "Collision",
		Shape = new BoxShape3D
		{
			Size = size
		}
	});
}


// =========================================================
// Uses the shared sunlight builder for the sandbox.
// =========================================================
private void CreateLighting()
{
	SpaceEnvironment.AddSun(this, SunEnergy);
}

	#endregion

	#region Space Environment

// =========================================================
// Uses the shared sky builder with this sandbox's saved settings.
// =========================================================
private void CreateSpaceEnvironment()
{
	SpaceEnvironment.ConfigureSky(
		this,
		StarBrightness,
		NebulaStrength,
		AmbientEnergy
	);
}

#endregion

	#region Asteroid Spawning

	// Places asteroid scene instances while keeping the spawn area clear and avoiding overlaps.
	
private void SpawnAsteroids()
{
	if (AsteroidScene == null)
	{
		GD.PushError("Assign Asteroid.tscn to Sandbox's Asteroid Scene field.");
		return;
	}

	RandomNumberGenerator random = new RandomNumberGenerator();
	random.Seed = AsteroidSeed;

	List<(Vector3 Position, float Radius)> placed = new();

	// Allow room for the largest deformed asteroids near walls.
	float clearance = LargeAsteroidMaxRadius * 1.8f + 15.0f;

	float spawnX = RoomWidth * 0.5f - clearance;
	float spawnY = RoomHeight * 0.5f - clearance;
	float spawnZ = RoomDepth * 0.5f - clearance;

	if (spawnX <= 0 || spawnY <= 0 || spawnZ <= 0)
	{
		GD.PushError("Room dimensions are too small for the largest asteroids.");
		return;
	}

	for (int index = 0; index < AsteroidCount; index++)
	{
		bool foundPosition = false;

		for (int attempt = 0; attempt < 100; attempt++)
		{
			float radius = GetAsteroidRadius(random, index);

			Vector3 position = new Vector3(
				random.RandfRange(-spawnX, spawnX),
				random.RandfRange(-spawnY, spawnY),
				random.RandfRange(-spawnZ, spawnZ)
			);

			float safeRadius = radius * 1.8f;

			// Keep the player's starting location clear.
			if (position.DistanceTo(new Vector3(0, 2, 0)) < 30.0f + safeRadius)
			{
				continue;
			}

			bool overlaps = false;

			foreach (var other in placed)
			{
				float minimumDistance =
					safeRadius + other.Radius * 1.8f + 5.0f;

				if (position.DistanceTo(other.Position) < minimumDistance)
				{
					overlaps = true;
					break;
				}
			}

			if (overlaps)
			{
				continue;
			}

			Asteroid asteroid = AsteroidScene.Instantiate<Asteroid>();
			asteroid.Name = $"Asteroid_{index:000}";
			asteroid.Configure(radius, random.Randi());
			asteroid.Position = position;

			AddChild(asteroid);

			placed.Add((position, radius));
			foundPosition = true;
			break;
		}

		if (!foundPosition)
		{
			GD.PushWarning($"Could not place asteroid {index}.");
		}
	}
}


private float GetAsteroidRadius(RandomNumberGenerator random, int index)
{
	if (index < LargeAsteroidCount)
	{
		return random.RandfRange(
			LargeAsteroidMinRadius,
			LargeAsteroidMaxRadius
		);
	}

	if (index < LargeAsteroidCount + MediumAsteroidCount)
	{
		return random.RandfRange(
			MediumAsteroidMinRadius,
			MediumAsteroidMaxRadius
		);
	}

	return random.RandfRange(
		SmallAsteroidMinRadius,
		SmallAsteroidMaxRadius
	);
}



	#endregion
}

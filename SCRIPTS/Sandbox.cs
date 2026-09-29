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


	#region Godot Events

	// Creates the room, lighting, and asteroid field when Sandbox starts.
	public override void _Ready()
	{
		CreateRoom();
		CreateLighting();
		SpawnAsteroids();
	}

	#endregion

	#region Room

	// Creates the six collidable boundaries of the large test room.
	
private void CreateRoom()
{
	float halfWidth = RoomWidth * 0.5f;
	float halfHeight = RoomHeight * 0.5f;
	float halfDepth = RoomDepth * 0.5f;

	CreateBox(
		"Floor",
		new Vector3(0, -halfHeight - 0.5f, 0),
		new Vector3(RoomWidth, 1, RoomDepth),
		Colors.DarkSlateGray
	);

	CreateBox(
		"Ceiling",
		new Vector3(0, halfHeight + 0.5f, 0),
		new Vector3(RoomWidth, 1, RoomDepth),
		Colors.DarkSlateGray
	);

	CreateBox(
		"NorthWall",
		new Vector3(0, 0, -halfDepth - 0.5f),
		new Vector3(RoomWidth, RoomHeight, 1),
		Colors.DimGray
	);

	CreateBox(
		"SouthWall",
		new Vector3(0, 0, halfDepth + 0.5f),
		new Vector3(RoomWidth, RoomHeight, 1),
		Colors.DimGray
	);

	CreateBox(
		"WestWall",
		new Vector3(-halfWidth - 0.5f, 0, 0),
		new Vector3(1, RoomHeight, RoomDepth),
		Colors.DimGray
	);

	CreateBox(
		"EastWall",
		new Vector3(halfWidth + 0.5f, 0, 0),
		new Vector3(1, RoomHeight, RoomDepth),
		Colors.DimGray
	);
}


	// Adds a light so the room and asteroids are visible.
	private void CreateLighting()
	{
		DirectionalLight3D light = new DirectionalLight3D();
		light.RotationDegrees = new Vector3(-45, -30, 0);
		AddChild(light);
	}

	// Creates one visible box with a matching static collision shape.
	private void CreateBox(string name, Vector3 position, Vector3 size, Color color)
	{
		StaticBody3D body = new StaticBody3D();
		body.Name = name;
		body.Position = position;
		AddChild(body);

		BoxMesh mesh = new BoxMesh();
		mesh.Size = size;

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Mesh = mesh;
		body.AddChild(visual);

		BoxShape3D shape = new BoxShape3D();
		shape.Size = size;

		CollisionShape3D collision = new CollisionShape3D();
		collision.Shape = shape;
		body.AddChild(collision);
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

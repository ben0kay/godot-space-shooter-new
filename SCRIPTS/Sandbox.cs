using System.Collections.Generic;
using Godot;

public partial class Sandbox : Node3D
{
	#region Settings

	[Export] public PackedScene AsteroidScene;
	[Export] public int AsteroidCount = 100;
	[Export] public ulong AsteroidSeed = 12345;

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
		CreateBox("Floor", new Vector3(0, -100.5f, 0), new Vector3(500, 1, 500), Colors.DarkSlateGray);
		CreateBox("Ceiling", new Vector3(0, 100.5f, 0), new Vector3(500, 1, 500), Colors.DarkSlateGray);

		CreateBox("NorthWall", new Vector3(0, 0, -250.5f), new Vector3(500, 200, 1), Colors.DimGray);
		CreateBox("SouthWall", new Vector3(0, 0, 250.5f), new Vector3(500, 200, 1), Colors.DimGray);
		CreateBox("WestWall", new Vector3(-250.5f, 0, 0), new Vector3(1, 200, 500), Colors.DimGray);
		CreateBox("EastWall", new Vector3(250.5f, 0, 0), new Vector3(1, 200, 500), Colors.DimGray);
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

		for (int index = 0; index < AsteroidCount; index++)
		{
			bool foundPosition = false;

			for (int attempt = 0; attempt < 100; attempt++)
			{
				float radius = random.RandfRange(2.0f, 6.0f);

				Vector3 position = new Vector3(
					random.RandfRange(-190.0f, 190.0f),
					random.RandfRange(-75.0f, 75.0f),
					random.RandfRange(-190.0f, 190.0f)
				);

				if (position.DistanceTo(new Vector3(0, 2, 0)) < 25.0f + radius)
				{
					continue;
				}

				bool overlaps = false;

				foreach (var other in placed)
				{
					if (position.DistanceTo(other.Position) < radius + other.Radius + 4.0f)
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

	#endregion
}

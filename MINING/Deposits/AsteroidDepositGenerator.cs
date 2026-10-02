using System.Collections.Generic;
using Godot;

// Places seeded resource patches on exposed asteroid faces without runtime scanning.
public static class AsteroidDepositGenerator
{
	#region Generation

	// =========================================================
	// Adds a repeatable selection of deposits to an already-built asteroid.
	public static void Populate(
		Asteroid asteroid,
		AsteroidFieldDefinition field,
		ulong shapeSeed
	)
	{
		if (!GodotObject.IsInstanceValid(asteroid)
			|| field == null
			|| field.DepositTypes == null
			|| field.DepositTypes.Count == 0
			|| asteroid.Radius < field.MinimumDepositAsteroidRadius)
		{
			return;
		}

		RandomNumberGenerator random = new()
		{
			Seed = shapeSeed ^ 0xD1B54A32D192ED03UL
		};

		if (random.Randf() >= Mathf.Clamp(
			field.DepositChance,
			0.0f,
			1.0f
		))
		{
			return;
		}

		MeshInstance3D visual =
			asteroid.GetNodeOrNull<MeshInstance3D>("Visual");

		if (visual?.Mesh == null)
		{
			return;
		}

		List<Surface> surfaces = FindExposedSurfaces(
			visual.Mesh.GetFaces(),
			asteroid.Radius
		);

		int minimum = Mathf.Max(
			0,
			Mathf.Min(
				field.DepositCountRange.X,
				field.DepositCountRange.Y
			)
		);

		int maximum = Mathf.Max(
			minimum,
			Mathf.Max(
				field.DepositCountRange.X,
				field.DepositCountRange.Y
			)
		);

		int requested = random.RandiRange(minimum, maximum);
		int spawned = 0;

		List<Vector3> occupiedCentres = new();
		List<float> occupiedRadii = new();

		while (spawned < requested && surfaces.Count > 0)
		{
			int index = random.RandiRange(
				0,
				surfaces.Count - 1
			);

			Surface surface = surfaces[index];
			surfaces.RemoveAt(index);

			float radius = Mathf.Min(
				Mathf.Clamp(
					asteroid.Radius * 0.16f,
					0.5f,
					3.5f
				),
				surface.Clearance * 0.75f
			);

			if (radius < 0.25f)
			{
				continue;
			}

			bool overlaps = false;

			for (int occupied = 0;
				occupied < occupiedCentres.Count;
				occupied++)
			{
				float spacing =
					radius + occupiedRadii[occupied] + 0.1f;

				if (surface.Centre.DistanceSquaredTo(
					occupiedCentres[occupied]
				) < spacing * spacing)
				{
					overlaps = true;
					break;
				}
			}

			if (overlaps)
			{
				continue;
			}

			ResourceDepositDefinition definition =
				ChooseDefinition(field.DepositTypes, random);

			if (definition == null)
			{
				return;
			}

			float reserveMinimum = Mathf.Max(
				0.01f,
				Mathf.Min(
					definition.ReserveRange.X,
					definition.ReserveRange.Y
				)
			);

			float reserveMaximum = Mathf.Max(
				reserveMinimum,
				Mathf.Max(
					definition.ReserveRange.X,
					definition.ReserveRange.Y
				)
			);

			float reserve = random.RandfRange(
				reserveMinimum,
				reserveMaximum
			) * Mathf.Clamp(
				asteroid.Radius / 10.0f,
				0.5f,
				4.0f
			);

			Vector3 reference =
				Mathf.Abs(surface.Normal.Dot(Vector3.Up)) > 0.95f
					? Vector3.Right
					: Vector3.Up;

			Vector3 tangent =
				reference.Cross(surface.Normal).Normalized();

			Basis basis = new(
				tangent,
				surface.Normal,
				tangent.Cross(surface.Normal)
			);

			float height = radius * 0.45f;

			ResourceDeposit deposit = new()
			{
				Name = $"Deposit_{spawned:D2}_{definition.ResourceType}",

				// Embed the bottom slightly into the host face.
				Position = surface.Centre
					+ surface.Normal * (height * 0.45f + 0.01f),

				Basis = basis
			};

			deposit.Configure(
				definition,
				Mathf.Max(1.0f, Mathf.Round(reserve)),
				radius,
				$"{asteroid.PersistentId}/deposit/{spawned:D2}"
			);

			// Parenting keeps the deposit attached when the asteroid rotates.
			asteroid.AddChild(deposit);

			occupiedCentres.Add(surface.Centre);
			occupiedRadii.Add(radius);

			spawned++;
		}
	}

	#endregion

	#region Surface Selection

	// =========================================================
	// Finds visible triangles that lie on the asteroid's convex outer hull.
	private static List<Surface> FindExposedSurfaces(
		Vector3[] faces,
		float asteroidRadius
	)
	{
		List<Surface> surfaces = new();

		float tolerance = Mathf.Max(
			0.0001f,
			asteroidRadius * 0.0001f
		);

		for (int index = 0; index + 2 < faces.Length; index += 3)
		{
			Vector3 a = faces[index];
			Vector3 b = faces[index + 1];
			Vector3 c = faces[index + 2];

			Vector3 cross = (b - a).Cross(c - a);
			float doubleArea = cross.Length();

			if (doubleArea <= 0.0001f)
			{
				continue;
			}

			Vector3 normal = cross / doubleArea;
			Vector3 centroid = (a + b + c) / 3.0f;

			if (normal.Dot(centroid) < 0.0f)
			{
				normal = -normal;
			}

			bool exposed = true;

			foreach (Vector3 vertex in faces)
			{
				if (normal.Dot(vertex - a) > tolerance)
				{
					exposed = false;
					break;
				}
			}

			if (!exposed)
			{
				continue;
			}

			float oppositeA = b.DistanceTo(c);
			float oppositeB = a.DistanceTo(c);
			float oppositeC = a.DistanceTo(b);

			float perimeter = oppositeA + oppositeB + oppositeC;

			if (perimeter <= 0.0001f)
			{
				continue;
			}

			// The incenter has equal clearance from all three triangle edges.
			Vector3 centre = (
				a * oppositeA
				+ b * oppositeB
				+ c * oppositeC
			) / perimeter;

			float clearance = doubleArea / perimeter;

			surfaces.Add(new Surface(
				centre,
				normal,
				clearance
			));
		}

		return surfaces;
	}

	#endregion

	#region Deposit Selection

	// =========================================================
	// Chooses a deposit definition using its configured generation weight.
	private static ResourceDepositDefinition ChooseDefinition(
		Godot.Collections.Array<ResourceDepositDefinition> definitions,
		RandomNumberGenerator random
	)
	{
		float total = 0.0f;

		foreach (ResourceDepositDefinition definition in definitions)
		{
			if (definition != null)
			{
				total += Mathf.Max(
					0.0f,
					definition.GenerationWeight
				);
			}
		}

		if (total <= 0.0f)
		{
			return null;
		}

		float roll = random.Randf() * total;
		ResourceDepositDefinition fallback = null;

		foreach (ResourceDepositDefinition definition in definitions)
		{
			if (definition == null || definition.GenerationWeight <= 0.0f)
			{
				continue;
			}

			fallback = definition;
			roll -= definition.GenerationWeight;

			if (roll <= 0.0f)
			{
				return definition;
			}
		}

		return fallback;
	}

	#endregion

	#region Surface Data

	private readonly struct Surface
	{
		public readonly Vector3 Centre;
		public readonly Vector3 Normal;
		public readonly float Clearance;

		// =========================================================
		// Stores an exposed face's placement point, orientation, and usable radius.
		public Surface(
			Vector3 centre,
			Vector3 normal,
			float clearance
		)
		{
			Centre = centre;
			Normal = normal;
			Clearance = clearance;
		}
	}

	#endregion
}
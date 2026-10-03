using Godot;

// Builds a reusable industrial mining-station placeholder with matching solid collision.
[Tool]
public partial class MiningStationVisual : Node3D
{
	#region Appearance

	[Export] public Color HullColor = new(0.27f, 0.29f, 0.32f);
	[Export] public Color TrimColor = new(0.12f, 0.14f, 0.17f);
	[Export] public Color LightColor = new(0.2f, 0.8f, 1.0f);

	#endregion

	#region Runtime

	private StaticBody3D _generated;
	private StandardMaterial3D _hull;
	private StandardMaterial3D _trim;
	private StandardMaterial3D _lights;

	#endregion

	#region Setup

	// =========================================================
	// Builds the station once, including an open approach between its arms.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		GetNodeOrNull<Node>("Generated")?.Free();

		_generated = new StaticBody3D { Name = "Generated" };
		AddChild(_generated);

		_hull = new StandardMaterial3D
		{
			AlbedoColor = HullColor,
			Roughness = 0.8f,
			Metallic = 0.35f
		};

		_trim = new StandardMaterial3D
		{
			AlbedoColor = TrimColor,
			Roughness = 0.9f,
			Metallic = 0.2f
		};

		_lights = new StandardMaterial3D
		{
			AlbedoColor = LightColor,
			EmissionEnabled = true,
			Emission = LightColor,
			EmissionEnergyMultiplier = 2.0f,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};

		BuildSpine();
		BuildRing(-90.0f, 180.0f);
		BuildRing(110.0f, 140.0f);
		BuildArms();

		_generated.AddChild(new Marker3D
		{
			Name = "DockApproach",
			Position = new Vector3(0, -30, 330)
		});
	}

	#endregion

	#region Station Sections

	// =========================================================
	// Creates the central tower, command decks and communications mast.
	private void BuildSpine()
	{
		AddCylinder("Spine", Vector3.Zero, 28, 34, 500, _hull);
		AddCylinder("LowerDeck", new Vector3(0, -170, 0), 60, 60, 28, _trim);
		AddCylinder("CommandDeck", new Vector3(0, 150, 0), 70, 55, 38, _hull);
		AddCylinder("UpperDeck", new Vector3(0, 215, 0), 38, 45, 24, _trim);
		AddCylinder("Antenna", new Vector3(0, 315, 0), 3, 5, 140, _trim);

		for (int index = 0; index < 4; index++)
		{
			float angle = index * Mathf.Tau / 4;
			Vector3 radial = new(Mathf.Sin(angle), 0, Mathf.Cos(angle));

			AddBox(
				$"SpineLight_{index}",
				radial * 35 + new Vector3(0, 40, 0),
				new Vector3(3, 100, 2),
				_lights, angle, false
			);

			AddBox(
				$"CommandWindow_{index}",
				radial * 64 + new Vector3(0, 157, 0),
				new Vector3(30, 3, 2),
				_lights, angle, false
			);
		}
	}

	// =========================================================
	// Constructs an open ring from solid segments rather than a filled disc.
	private void BuildRing(float height, float radius)
	{
		const int segments = 24;
		float length = Mathf.Tau * radius / segments * 1.04f;

		for (int index = 0; index < segments; index++)
		{
			float angle = index * Mathf.Tau / segments;
			Vector3 position = new(
				Mathf.Sin(angle) * radius,
				height,
				Mathf.Cos(angle) * radius
			);

			AddBox(
				$"Ring_{height}_{index}",
				position, new Vector3(length, 14, 16),
				_hull, angle
			);

			if (index % 3 == 0)
			{
				AddBox(
					$"RingLight_{height}_{index}",
					position + Vector3.Up * 8,
					new Vector3(length * 0.45f, 1, 2),
					_lights, angle, false
				);
			}
		}

		for (int index = 0; index < 4; index++)
		{
			float angle = index * Mathf.Tau / 4;
			Vector3 radial = new(Mathf.Sin(angle), 0, Mathf.Cos(angle));

			AddBox(
				$"RingSupport_{height}_{index}",
				radial * radius * 0.5f + Vector3.Up * height,
				new Vector3(10, 12, radius),
				_trim, angle
			);
		}
	}

	// =========================================================
	// Adds docking arms, industrial modules and storage tanks.
	private void BuildArms()
	{
		for (int index = 0; index < 4; index++)
		{
			float angle = index * Mathf.Tau / 4 + Mathf.Pi / 4;
			Vector3 radial = new(Mathf.Sin(angle), 0, Mathf.Cos(angle));
			Vector3 end = radial * 230 + Vector3.Down * 30;

			AddBox(
				$"Arm_{index}",
				radial * 120 + Vector3.Down * 30,
				new Vector3(22, 20, 240),
				_trim, angle
			);

			AddBox(
				$"DockModule_{index}",
				end, new Vector3(65, 42, 105),
				_hull, angle
			);

			AddBox(
				$"DockLight_{index}",
				end + Vector3.Up * 23,
				new Vector3(45, 2, 75),
				_lights, angle, false
			);

			AddCylinder(
				$"StorageTank_{index}",
				end + Vector3.Down * 70,
				23, 23, 85, _hull
			);
		}
	}

	#endregion

	#region Primitive Helpers

	// =========================================================
	// Adds a box and optionally its matching collision shape.
	private void AddBox(
		string name, Vector3 position, Vector3 size,
		Material material, float yaw = 0, bool solid = true
	)
	{
		_generated.AddChild(new MeshInstance3D
		{
			Name = name,
			Position = position,
			Rotation = new Vector3(0, yaw, 0),
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material,
			CastShadow = solid
				? GeometryInstance3D.ShadowCastingSetting.On
				: GeometryInstance3D.ShadowCastingSetting.Off
		});

		if (!solid) return;

		_generated.AddChild(new CollisionShape3D
		{
			Name = name + "_Collision",
			Position = position,
			Rotation = new Vector3(0, yaw, 0),
			Shape = new BoxShape3D { Size = size }
		});
	}

	// =========================================================
	// Adds a low-sided cylindrical section with a matching convex collider.
	private void AddCylinder(
		string name, Vector3 position,
		float topRadius, float bottomRadius, float height,
		Material material
	)
	{
		CylinderMesh mesh = new()
		{
			TopRadius = topRadius,
			BottomRadius = bottomRadius,
			Height = height,
			RadialSegments = 12,
			Rings = 1
		};

		_generated.AddChild(new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = mesh,
			MaterialOverride = material
		});

		_generated.AddChild(new CollisionShape3D
		{
			Name = name + "_Collision",
			Position = position,
			Shape = mesh.CreateConvexShape()
		});
	}

	#endregion
}
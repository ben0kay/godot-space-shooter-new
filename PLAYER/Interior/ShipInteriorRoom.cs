using Godot;

// Builds a replaceable single-room ship interior with open viewing windows.
public partial class ShipInteriorRoom : Node3D
{
	#region Layout

	[ExportGroup("Room Dimensions")]
	[Export] public Vector2 HalfSize = new(2.2f, 3.4f);
	[Export] public float RoomHeight = 2.6f;

	[ExportGroup("Appearance")]
	[Export] public Color WallColor = new(0.055f, 0.07f, 0.085f);
	[Export] public Color FloorColor = new(0.025f, 0.035f, 0.045f);
	[Export] public Color TrimColor = new(0.16f, 0.21f, 0.25f);
	[Export] public Color LightColor = new(0.15f, 0.8f, 1.0f);

	// Positions are relative to this room, independent of world movement.
	public Vector3 PilotPosition =>
		new(0.0f, 0.0f, -HalfSize.Y + 1.3f);

	public Vector3 StandingPosition =>
		PilotPosition + Vector3.Back * 0.9f;

	#endregion

	#region Runtime

	private Node3D _geometry;
	private StandardMaterial3D _wall;
	private StandardMaterial3D _floor;
	private StandardMaterial3D _trim;
	private StandardMaterial3D _light;

	#endregion

	#region Construction

	// =========================================================
	// Builds static room geometry once when the scene enters the tree.
	public override void _Ready()
	{
		HalfSize = new Vector2(
			Mathf.Max(1.5f, HalfSize.X),
			Mathf.Max(2.5f, HalfSize.Y)
		);
		RoomHeight = Mathf.Max(2.2f, RoomHeight);

		_wall = CreateMaterial(WallColor);
		_floor = CreateMaterial(FloorColor);
		_trim = CreateMaterial(TrimColor);
		_light = CreateMaterial(LightColor, true);

		_geometry = new Node3D { Name = "Geometry" };
		AddChild(_geometry);

		BuildShell();
		BuildEquipment();
		BuildLighting();
	}

	// =========================================================
	// Creates shared materials with predictable placeholder lighting.
	private StandardMaterial3D CreateMaterial(Color color, bool glowing = false)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			EmissionEnabled = glowing,
			Emission = color,
			EmissionEnergyMultiplier = glowing ? 1.5f : 0.0f
		};
	}

	// =========================================================
	// Builds floor, ceiling, rear wall and window frames.
	private void BuildShell()
	{
		float x = HalfSize.X;
		float z = HalfSize.Y;
		float height = RoomHeight;

		AddBox("Floor", new(x * 2, 0.12f, z * 2),
			new(0, -0.06f, 0), _floor);

		AddBox("Ceiling", new(x * 2, 0.12f, z * 2),
			new(0, height + 0.06f, 0), _wall);

		AddBox("RearWall", new(x * 2, height, 0.12f),
			new(0, height * 0.5f, z), _wall);

		// Window openings have no opaque panes: the real world is outside.
		AddBox("FrontLowerWall", new(x * 2, 0.7f, 0.12f),
			new(0, 0.35f, -z), _wall);

		AddBox("FrontUpperWall", new(x * 2, 0.4f, 0.12f),
			new(0, height - 0.2f, -z), _wall);

		AddBox("FrontCentreFrame", new(0.045f, height - 1.1f, 0.12f),
			new(0, (height + 0.3f) * 0.5f, -z), _trim);

		for (int side = -1; side <= 1; side += 2)
		{
			AddBox($"SideLowerWall{side}", new(0.12f, 0.7f, z * 2),
				new(side * x, 0.35f, 0), _wall);

			AddBox($"SideUpperWall{side}", new(0.12f, 0.4f, z * 2),
				new(side * x, height - 0.2f, 0), _wall);

			for (int index = -1; index <= 1; index++)
			{
				AddBox($"WindowPillar{side}_{index}",
					new(0.12f, height - 1.1f, 0.08f),
					new(side * x, (height + 0.3f) * 0.5f, index * z * 0.65f),
					_trim);
			}

			AddBox($"FrontCorner{side}", new(0.12f, height, 0.16f),
				new(side * x, height * 0.5f, -z), _trim);
		}

		// Floor seams provide scale and movement references.
		for (int index = -2; index <= 2; index++)
		{
			AddBox($"FloorSeam{index}", new(x * 2, 0.006f, 0.018f),
				new(0, 0.004f, index * z * 0.32f), _trim);
		}
	}

	// =========================================================
	// Places decorative equipment outside the central walking area.
	private void BuildEquipment()
	{
		float front = -HalfSize.Y;

		AddBox("PilotConsole", new(2.0f, 0.8f, 0.55f),
			new(0, 0.4f, front + 0.3f), _wall);

		AddBox("PilotDisplay", new(1.35f, 0.22f, 0.012f),
			new(0, 0.66f, front + 0.583f), _light);

		AddBox("PilotStationMark", new(0.8f, 0.006f, 0.045f),
			PilotPosition + new Vector3(0, 0.008f, 0), _light);

		for (int side = -1; side <= 1; side += 2)
		{
			AddBox($"EquipmentCabinet{side}", new(0.38f, 0.6f, 1.4f),
				new(side * (HalfSize.X - 0.25f), 0.3f, 1.0f), _trim);

			AddBox($"EquipmentDisplay{side}", new(0.012f, 0.16f, 0.7f),
				new(side * (HalfSize.X - 0.447f), 0.43f, 1.0f), _light);
		}

		AddBox("RearDoor", new(1.1f, 2.1f, 0.025f),
			new(0, 1.05f, HalfSize.Y - 0.075f), _trim);

		AddBox("RearDoorSeam", new(0.012f, 1.9f, 0.015f),
			new(0, 1.05f, HalfSize.Y - 0.095f), _light);
	}

	// =========================================================
	// Adds thin illumination strips without extra dynamic lights.
	private void BuildLighting()
	{
		for (int side = -1; side <= 1; side += 2)
		{
			AddBox($"CeilingStrip{side}", new(0.035f, 0.012f, HalfSize.Y * 1.7f),
				new(side * HalfSize.X * 0.65f, RoomHeight - 0.01f, 0), _light);

			AddBox($"FloorStrip{side}", new(0.018f, 0.008f, HalfSize.Y * 1.7f),
				new(side * (HalfSize.X - 0.6f), 0.008f, 0), _light);
		}
	}

	// =========================================================
	// Adds one static visual part without affecting exterior ship collision.
	private void AddBox(string name, Vector3 size, Vector3 position, Material material)
	{
		_geometry.AddChild(new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		});
	}

	#endregion

	#region Walking Boundaries

	// =========================================================
	// Constrains this prototype to the clear central floor area.
	public Vector3 ClampWalkingPosition(Vector3 position)
	{
		float halfWidth = Mathf.Max(0.5f, HalfSize.X - 0.65f);

		return new Vector3(
			Mathf.Clamp(position.X, -halfWidth, halfWidth),
			0.0f,
			Mathf.Clamp(position.Z, -HalfSize.Y + 1.0f, HalfSize.Y - 0.35f)
		);
	}

	#endregion
}
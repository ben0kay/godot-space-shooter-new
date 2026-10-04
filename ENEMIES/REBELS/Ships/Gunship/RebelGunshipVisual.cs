using Godot;

// Builds a temporary Rebel gunship with four deck turrets and two fixed guns.
public partial class RebelGunshipVisual : Node3D
{
	#region Materials

	private StandardMaterial3D _hull;
	private StandardMaterial3D _plate;
	private StandardMaterial3D _metal;
	private StandardMaterial3D _glass;
	private StandardMaterial3D _engine;

	#endregion

	#region Setup

	// =========================================================
	// Builds geometry and hardpoints once before EnemyShip registers its mounts.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		_hull = MakeMaterial(new Color(0.24f, 0.20f, 0.16f));
		_plate = MakeMaterial(new Color(0.42f, 0.35f, 0.24f));
		_metal = MakeMaterial(new Color(0.15f, 0.16f, 0.17f));
		_glass = MakeMaterial(new Color(0.025f, 0.06f, 0.07f));
		_engine = MakeMaterial(new Color(1.0f, 0.45f, 0.08f), true);

		Node3D geometry = new() { Name = "Geometry" };
		AddChild(geometry);

		AddBox(geometry, "Hull", Vector3.Zero,
			new Vector3(2.0f, 1.0f, 7.0f), _hull);
		AddBox(geometry, "Spine", new Vector3(0, 0.65f, 0),
			new Vector3(1.2f, 0.4f, 4.8f), _plate);
		AddBox(geometry, "Cockpit", new Vector3(0, 0.6f, -2.3f),
			new Vector3(1.3f, 0.65f, 1.4f), _glass);

		for (int side = -1; side <= 1; side += 2)
		{
			AddBox(geometry, $"Wing_{side}", new Vector3(side * 2, 0, 0.7f),
				new Vector3(2.4f, 0.35f, 3.6f), _plate);
			AddBox(geometry, $"EngineHousing_{side}",
				new Vector3(side * 2.1f, 0, 2.5f),
				new Vector3(0.9f, 0.8f, 2.0f), _metal);
			AddBox(geometry, $"EngineGlow_{side}",
				new Vector3(side * 2.1f, 0, 3.52f),
				new Vector3(0.65f, 0.55f, 0.12f), _engine);
		}

		Node3D mounts = new() { Name = "Hardpoints" };
		AddChild(mounts);

		AddGun(mounts, "TurretFrontLeft", "turrets",
			new Vector3(-1.45f, 1.35f, -1.6f), false);
		AddGun(mounts, "TurretFrontRight", "turrets",
			new Vector3(1.45f, 1.35f, -1.6f), false);
		AddGun(mounts, "TurretRearLeft", "turrets",
			new Vector3(-1.45f, 1.35f, 1.4f), false);
		AddGun(mounts, "TurretRearRight", "turrets",
			new Vector3(1.45f, 1.35f, 1.4f), false);

		AddGun(mounts, "FixedLeft", "forward_guns",
			new Vector3(-0.65f, 0, -3.5f), true);
		AddGun(mounts, "FixedRight", "forward_guns",
			new Vector3(0.65f, 0, -3.5f), true);
	}

	#endregion

	#region Construction

	// =========================================================
	// Creates one shared rough material for this visual's geometry.
	private StandardMaterial3D MakeMaterial(Color color, bool glowing = false)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = 0.85f,
			EmissionEnabled = glowing,
			Emission = color,
			EmissionEnergyMultiplier = glowing ? 2.0f : 1.0f
		};
	}

	// =========================================================
	// Adds a decorative box without an additional physics body.
	private void AddBox(
		Node3D parent, string name, Vector3 position,
		Vector3 size, Material material
	)
	{
		parent.AddChild(new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material
		});
	}

	// =========================================================
	// Creates an independently aiming mount with a muzzle outside its barrel.
	private void AddGun(
		Node3D parent, string name, string group,
		Vector3 position, bool fixedGun
	)
	{
		EnemyHardpoint gun = new()
		{
			Name = name,
			Key = name,
			Group = group,
			Position = position,
			RotationStats = new HardpointRotationStats
			{
				Mode = fixedGun
					? HardpointRotationMode.Fixed
					: HardpointRotationMode.Target,
				TurnSpeedDegrees = 240.0f,
				YawLimitDegrees = 180.0f,
				PitchUpDegrees = 80.0f,
				PitchDownDegrees = 80.0f,
				ReturnToRest = true
			}
		};

		// Assemble children before entering the tree so Ready finds the muzzle.
		AddBox(gun, "Housing", Vector3.Zero,
			new Vector3(0.45f, 0.3f, 0.55f), _metal);
		AddBox(gun, "Barrel", new Vector3(0, 0, -0.55f),
			new Vector3(0.16f, 0.16f, 0.8f), _plate);

		gun.AddChild(new Marker3D
		{
			Name = "Muzzle",
			Position = new Vector3(0, 0, -1.0f)
		});

		parent.AddChild(gun);
	}

	#endregion
}
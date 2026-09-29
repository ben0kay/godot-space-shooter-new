using Godot;

public partial class SimulantDreadwingVisual : Node3D
{
	private FactionPalette _palette;

	private Color _dark => _palette.HullDark;
	private Color _hull => _palette.HullMid;
	private Color _armour => _palette.HullLight;
	private Color _highlight => _palette.Metal;
	private Color _energy => _palette.Energy;
	private Color _core => _palette.Core;
	[Export(PropertyHint.Range, "0.25,4.0,0.05")]
public float VisualScale = 1.0f;

	public override void _Ready()
{
	Faction faction = GetParent() is ICombatTarget owner
		? owner.CombatFaction
		: Faction.Simulant;

	_palette = FactionPalettes.Get(faction);

	Scale = Vector3.One * VisualScale;

	CreateBody();
	CreateWings();
	CreateFins();
	CreateWeapons();
	CreateCore();
	CreateEngines();
}

	private void CreateBody()
	{
		// A deep underside gives the ship mass when seen from the side.
		AddBox("Lower Keel", new Vector3(0, -0.65f, 0.15f),
			new Vector3(2.5f, 1.4f, 5.6f), _dark);

		AddBox("Main Hull", new Vector3(0, 0.15f, 0),
			new Vector3(2.8f, 0.9f, 5.2f), _hull);

		AddBox("Upper Deck", new Vector3(0, 0.85f, -0.35f),
			new Vector3(2.0f, 0.55f, 3.8f), _armour);

		AddBox("Raised Bridge", new Vector3(0, 1.35f, -0.75f),
			new Vector3(1.35f, 0.75f, 2.3f), _hull);

		AddBox("Dorsal Spine", new Vector3(0, 1.85f, -0.75f),
			new Vector3(0.55f, 0.45f, 2.8f), _highlight);

		AddBox("Armoured Nose", new Vector3(0, 0.4f, -2.65f),
			new Vector3(1.25f, 0.65f, 1.65f), _armour);

		AddBox("Nose Blade", new Vector3(0, 0.65f, -3.15f),
			new Vector3(0.4f, 0.22f, 0.85f), _highlight);

		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Lower Hull Rib", new Vector3(side * 1.15f, -0.55f, 0.1f),
				new Vector3(0.35f, 1.0f, 4.4f), _armour);

			AddBox("Upper Hull Rib", new Vector3(side * 1.05f, 1.0f, -0.4f),
				new Vector3(0.28f, 0.35f, 3.2f), _highlight);
		}
	}

	private void CreateWings()
	{
		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Inner Wing", new Vector3(side * 2.45f, 0.05f, 0.2f),
				new Vector3(3.5f, 0.7f, 2.75f), _hull,
				yaw: side * 15f);

			AddBox("Lower Wing Armour", new Vector3(side * 2.9f, -0.5f, 0.35f),
				new Vector3(3.5f, 0.45f, 1.9f), _dark,
				yaw: side * 15f);

			AddBox("Outer Wing Blade", new Vector3(side * 5.0f, 0.15f, 0.65f),
				new Vector3(2.7f, 0.4f, 1.25f), _armour,
				yaw: side * 27f);

			// The shoulder rises well above the wing instead of lying flat.
			AddBox("Shoulder Base", new Vector3(side * 1.95f, 0.85f, -0.1f),
				new Vector3(1.45f, 1.05f, 2.25f), _armour,
				yaw: side * 12f);

			AddBox("Shoulder Crest", new Vector3(side * 2.15f, 1.48f, -0.25f),
				new Vector3(1.0f, 0.35f, 1.75f), _highlight,
				yaw: side * 12f);

			AddBox("Raised Wing Plate", new Vector3(side * 3.35f, 0.85f, 0.2f),
				new Vector3(2.35f, 0.23f, 1.55f), _armour,
				yaw: side * 15f,
				roll: side * 10f);

			AddBox("Wing Energy Seam", new Vector3(side * 4.0f, 0.78f, 0.65f),
				new Vector3(1.7f, 0.08f, 0.14f), _energy,
				glowing: true,
				yaw: side * 20f);
		}
	}

	private void CreateFins()
	{
		foreach (int side in new[] { -1, 1 })
		{
			// These are the main vertical silhouette from a side or rear view.
			AddBox("Tall Shoulder Fin", new Vector3(side * 2.45f, 2.25f, 0.15f),
				new Vector3(0.24f, 2.35f, 1.65f), _dark,
				roll: side * 16f);

			AddBox("Shoulder Fin Edge", new Vector3(side * 2.54f, 2.35f, -0.55f),
				new Vector3(0.1f, 1.8f, 0.12f), _energy,
				glowing: true,
				roll: side * 16f);

			AddBox("Outer Wing Fin", new Vector3(side * 5.25f, 1.45f, 0.75f),
				new Vector3(0.19f, 1.75f, 1.2f), _hull,
				roll: side * 22f);

			AddBox("Outer Fin Tip", new Vector3(side * 5.45f, 2.25f, 0.75f),
				new Vector3(0.12f, 0.55f, 0.85f), _highlight,
				roll: side * 22f);
		}

		AddBox("Central Spine Fin", new Vector3(0, 2.35f, 0.65f),
			new Vector3(0.22f, 1.4f, 1.25f), _dark);

		AddBox("Spine Fin Edge", new Vector3(0, 2.45f, 0.03f),
			new Vector3(0.09f, 0.9f, 0.12f), _energy,
			glowing: true);
	}

	private void CreateWeapons()
	{
		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Inner Weapon Mount", new Vector3(side * 1.4f, 1.35f, -1.55f),
				new Vector3(0.75f, 0.65f, 1.1f), _dark);

			AddBox("Inner Cannon", new Vector3(side * 1.4f, 1.38f, -2.35f),
				new Vector3(0.26f, 0.26f, 1.15f), _highlight);

			AddBox("Outer Weapon Mount", new Vector3(side * 4.35f, 0.85f, -0.35f),
				new Vector3(0.85f, 0.6f, 1.15f), _dark);

			AddBox("Outer Cannon", new Vector3(side * 4.35f, 0.85f, -1.2f),
				new Vector3(0.3f, 0.3f, 1.2f), _highlight);
		}
	}

	private void CreateCore()
	{
		AddCylinder("Core Socket", new Vector3(0, 1.65f, 0.55f),
			0.78f, 0.32f, _dark);

		AddSphere("Energy Core", new Vector3(0, 1.94f, 0.55f),
			0.43f, _core, glowing: true);

		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Core Clamp", new Vector3(side * 0.62f, 1.93f, 0.55f),
				new Vector3(0.22f, 0.48f, 0.85f), _highlight);
		}
	}

	private void CreateEngines()
{
	foreach (float x in new[] { -4.0f, -1.1f, 1.1f, 4.0f })
	{
		AddBox(
			"Engine Housing",
			new Vector3(x, -0.1f, 1.65f),
			new Vector3(0.85f, 0.85f, 1.35f),
			_dark
		);

		AddBox(
			"Engine Exhaust",
			new Vector3(x, -0.1f, 2.37f),
			new Vector3(0.55f, 0.5f, 0.12f),
			_palette.Thruster,
			glowing: true
		);
	}
}

	private void AddBox(
		string name,
		Vector3 position,
		Vector3 size,
		Color color,
		bool glowing = false,
		float yaw = 0f,
		float roll = 0f)
	{
		var mesh = new MeshInstance3D
		{
			Name = name,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = CreateMaterial(color, glowing),
			Position = position,
			RotationDegrees = new Vector3(0, yaw, roll)
		};

		AddChild(mesh);
	}

	private void AddSphere(
		string name,
		Vector3 position,
		float radius,
		Color color,
		bool glowing = false)
	{
		var mesh = new MeshInstance3D
		{
			Name = name,
			Mesh = new SphereMesh
			{
				Radius = radius,
				Height = radius * 2f
			},
			MaterialOverride = CreateMaterial(color, glowing),
			Position = position
		};

		AddChild(mesh);
	}

	private void AddCylinder(
		string name,
		Vector3 position,
		float radius,
		float height,
		Color color)
	{
		var mesh = new MeshInstance3D
		{
			Name = name,
			Mesh = new CylinderMesh
			{
				TopRadius = radius,
				BottomRadius = radius,
				Height = height
			},
			MaterialOverride = CreateMaterial(color, false),
			Position = position
		};

		AddChild(mesh);
	}

	private StandardMaterial3D CreateMaterial(Color color, bool glowing)
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = color,
			Metallic = 0.45f,
			Roughness = 0.38f
		};

		if (glowing)
		{
			material.EmissionEnabled = true;
			material.Emission = color;
			material.EmissionEnergyMultiplier = 3.0f;
		}

		return material;
	}
}

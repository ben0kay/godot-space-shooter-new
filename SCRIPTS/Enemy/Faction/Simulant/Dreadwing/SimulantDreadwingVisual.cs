using Godot;

[Tool]
public partial class SimulantDreadwingVisual : Node3D
{
	#region Settings

	[Export(PropertyHint.Range, "0.25,4.0,0.05")]
	public float VisualScale = 1.0f;

	[Export] public Faction PreviewFaction = Faction.Simulant;

	// Tick this in the Inspector after changing palette colours.
	[Export] public bool RefreshPreview = false;

	#endregion

	#region References

	private Node3D _geometry;
	private FactionPalette _palette;

	private Color _dark => _palette.HullDark;
	private Color _hull => _palette.HullMid;
	private Color _armour => _palette.HullLight;
	private Color _highlight => _palette.Metal;
	private Color _energy => _palette.Energy;
	private Color _core => _palette.Core;

	#endregion

	#region Preview State

	private float _lastScale;
	private Faction _lastPreviewFaction;

	#endregion

	#region Godot Events

	// Builds the ship in both the editor and the running game.
	public override void _Ready()
	{
		RebuildVisual();

		// Preview changes only need monitoring inside the editor.
		SetProcess(Engine.IsEditorHint());
	}

	// Refreshes the editor preview when its settings change.
	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			return;
		}

		if (!RefreshPreview
			&& Mathf.IsEqualApprox(VisualScale, _lastScale)
			&& PreviewFaction == _lastPreviewFaction)
		{
			return;
		}

		RefreshPreview = false;
		RebuildVisual();
	}

	#endregion

	#region Visual Setup

	// Replaces generated shapes while preserving manually placed scene nodes.
	private void RebuildVisual()
	{
		Node3D previous = GetNodeOrNull<Node3D>("GeneratedGeometry");

		if (previous != null)
		{
			RemoveChild(previous);
			previous.QueueFree();
		}

		_palette = ResolvePalette();

		Scale = Vector3.One * Mathf.Max(0.01f, VisualScale);
		_lastScale = VisualScale;
		_lastPreviewFaction = PreviewFaction;

		_geometry = new Node3D
		{
			Name = "GeneratedGeometry"
		};

		// No Owner is assigned: these shapes are regenerated, not saved.
		AddChild(_geometry);

		CreateBody();
		CreateWings();
		CreateFins();
		CreateWeapons();
		CreateCore();
		CreateEngines();
	}

	// Uses the preview faction in the editor and the owning ship in game.
	private FactionPalette ResolvePalette()
	{
		if (Engine.IsEditorHint())
		{
			string path =
				$"res://RESOURCES/Factions/{PreviewFaction}Palette.tres";

			if (ResourceLoader.Exists(path))
			{
				FactionPalette palette = GD.Load<FactionPalette>(path);

				if (palette != null)
				{
					return palette;
				}
			}

			return new FactionPalette();
		}

		Faction faction = GetParent() is ICombatTarget owner
			? owner.CombatFaction
			: PreviewFaction;

		return FactionPalettes.Get(faction);
	}

	#endregion

	#region Hull

	// Builds the central hull, raised bridge, nose, and armour ribs.
	private void CreateBody()
	{
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
			AddBox("Lower Hull Rib",
				new Vector3(side * 1.15f, -0.55f, 0.1f),
				new Vector3(0.35f, 1.0f, 4.4f), _armour);

			AddBox("Upper Hull Rib",
				new Vector3(side * 1.05f, 1.0f, -0.4f),
				new Vector3(0.28f, 0.35f, 3.2f), _highlight);
		}
	}

	// Builds the swept wings, raised shoulders, and energy seams.
	private void CreateWings()
	{
		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Inner Wing",
				new Vector3(side * 2.45f, 0.05f, 0.2f),
				new Vector3(3.5f, 0.7f, 2.75f), _hull,
				yaw: side * 15f);

			AddBox("Lower Wing Armour",
				new Vector3(side * 2.9f, -0.5f, 0.35f),
				new Vector3(3.5f, 0.45f, 1.9f), _dark,
				yaw: side * 15f);

			AddBox("Outer Wing Blade",
				new Vector3(side * 5.0f, 0.15f, 0.65f),
				new Vector3(2.7f, 0.4f, 1.25f), _armour,
				yaw: side * 27f);

			AddBox("Shoulder Base",
				new Vector3(side * 1.95f, 0.85f, -0.1f),
				new Vector3(1.45f, 1.05f, 2.25f), _armour,
				yaw: side * 12f);

			AddBox("Shoulder Crest",
				new Vector3(side * 2.15f, 1.48f, -0.25f),
				new Vector3(1.0f, 0.35f, 1.75f), _highlight,
				yaw: side * 12f);

			AddBox("Raised Wing Plate",
				new Vector3(side * 3.35f, 0.85f, 0.2f),
				new Vector3(2.35f, 0.23f, 1.55f), _armour,
				yaw: side * 15f,
				roll: side * 10f);

			AddBox("Wing Energy Seam",
				new Vector3(side * 4.0f, 0.78f, 0.65f),
				new Vector3(1.7f, 0.08f, 0.14f), _energy,
				glowing: true,
				yaw: side * 20f);
		}
	}

	// Builds the tall shoulder, outer wing, and central fins.
	private void CreateFins()
	{
		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Tall Shoulder Fin",
				new Vector3(side * 2.45f, 2.25f, 0.15f),
				new Vector3(0.24f, 2.35f, 1.65f), _dark,
				roll: side * 16f);

			AddBox("Shoulder Fin Edge",
				new Vector3(side * 2.54f, 2.35f, -0.55f),
				new Vector3(0.1f, 1.8f, 0.12f), _energy,
				glowing: true,
				roll: side * 16f);

			AddBox("Outer Wing Fin",
				new Vector3(side * 5.25f, 1.45f, 0.75f),
				new Vector3(0.19f, 1.75f, 1.2f), _hull,
				roll: side * 22f);

			AddBox("Outer Fin Tip",
				new Vector3(side * 5.45f, 2.25f, 0.75f),
				new Vector3(0.12f, 0.55f, 0.85f), _highlight,
				roll: side * 22f);
		}

		AddBox("Central Spine Fin", new Vector3(0, 2.35f, 0.65f),
			new Vector3(0.22f, 1.4f, 1.25f), _dark);

		AddBox("Spine Fin Edge", new Vector3(0, 2.45f, 0.03f),
			new Vector3(0.09f, 0.9f, 0.12f), _energy,
			glowing: true);
	}

	#endregion

	#region Equipment

	// Builds the stationary housings used to position the cannon pivots.
	private void CreateWeapons()
	{
		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Inner Weapon Mount",
				new Vector3(side * 1.4f, 1.35f, -1.55f),
				new Vector3(0.75f, 0.65f, 1.1f), _dark);

			AddBox("Outer Weapon Mount",
				new Vector3(side * 4.35f, 0.85f, -0.35f),
				new Vector3(0.85f, 0.6f, 1.15f), _dark);
		}
	}

	// Builds the central energy core, socket, and retaining clamps.
	private void CreateCore()
	{
		AddCylinder("Core Socket", new Vector3(0, 1.65f, 0.55f),
			0.78f, 0.32f, _dark);

		AddSphere("Energy Core", new Vector3(0, 1.94f, 0.55f),
			0.43f, _core, glowing: true);

		foreach (int side in new[] { -1, 1 })
		{
			AddBox("Core Clamp",
				new Vector3(side * 0.62f, 1.93f, 0.55f),
				new Vector3(0.22f, 0.48f, 0.85f), _highlight);
		}
	}

	// Builds the engine housings and faction-coloured exhaust apertures.
	private void CreateEngines()
	{
		foreach (float x in new[] { -4.0f, -1.1f, 1.1f, 4.0f })
		{
			AddBox("Engine Housing", new Vector3(x, -0.1f, 1.65f),
				new Vector3(0.85f, 0.85f, 1.35f), _dark);

			AddBox("Engine Exhaust", new Vector3(x, -0.1f, 2.37f),
				new Vector3(0.55f, 0.5f, 0.12f), _palette.Thruster,
				glowing: true);
		}
	}

	#endregion

	#region Shape Helpers

	// Adds a box to the generated geometry with optional rotation and glow.
	private void AddBox(
		string name,
		Vector3 position,
		Vector3 size,
		Color color,
		bool glowing = false,
		float yaw = 0f,
		float roll = 0f)
	{
		MeshInstance3D mesh = new()
		{
			Name = name,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = CreateMaterial(color, glowing),
			Position = position,
			RotationDegrees = new Vector3(0, yaw, roll)
		};

		_geometry.AddChild(mesh);
	}

	// Adds a sphere to the generated geometry with optional glow.
	private void AddSphere(
		string name,
		Vector3 position,
		float radius,
		Color color,
		bool glowing = false)
	{
		MeshInstance3D mesh = new()
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

		_geometry.AddChild(mesh);
	}

	// Adds an upright cylinder to the generated geometry.
	private void AddCylinder(
		string name,
		Vector3 position,
		float radius,
		float height,
		Color color)
	{
		MeshInstance3D mesh = new()
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

		_geometry.AddChild(mesh);
	}

	// Creates a hull material or an emissive energy material.
	private StandardMaterial3D CreateMaterial(Color color, bool glowing)
	{
		StandardMaterial3D material = new()
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

	#endregion
}

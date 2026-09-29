using Godot;

public partial class SimulantDreadwingVisual : Node3D
{
	#region Colours

	private readonly Color _hull = new Color(0.11f, 0.09f, 0.18f);
	private readonly Color _armour = new Color(0.24f, 0.17f, 0.36f);
	private readonly Color _edge = new Color(0.37f, 0.28f, 0.53f);
	private readonly Color _energy = new Color(0.63f, 0.24f, 1.0f);
	private readonly Color _core = new Color(0.86f, 0.60f, 1.0f);

	#endregion

	#region Setup

	// Builds the temporary 3D Dreadwing entirely from mesh primitives.
	public override void _Ready()
	{
		CreateBody();
		CreateWings();
		CreateWeapons();
		CreateCore();
		CreateEngines();
	}

	#endregion

	#region Parts

	// Creates the central spine and pointed forward section.
	private void CreateBody()
	{
		AddBox(
			"CentralHull",
			new Vector3(0, 0, 0),
			new Vector3(2.6f, 0.9f, 5.2f),
			_hull
		);

		AddBox(
			"UpperArmour",
			new Vector3(0, 0.55f, -0.2f),
			new Vector3(1.8f, 0.35f, 3.4f),
			_armour
		);

		AddBox(
			"ForwardSpine",
			new Vector3(0, 0.15f, -2.5f),
			new Vector3(0.8f, 0.5f, 2.0f),
			_edge
		);

		AddBox(
			"EnergyTrench",
			new Vector3(0, 0.76f, -0.65f),
			new Vector3(0.12f, 0.05f, 2.4f),
			_energy,
			true
		);
	}

	// Forms a broad swept silhouette on both sides.
	private void CreateWings()
	{
		for (int side = -1; side <= 1; side += 2)
		{
			AddBox(
				side < 0 ? "LeftInnerWing" : "RightInnerWing",
				new Vector3(side * 2.5f, 0.05f, 0.1f),
				new Vector3(3.5f, 0.42f, 2.8f),
				_armour,
				false,
				side * 15.0f
			);

			AddBox(
				side < 0 ? "LeftOuterBlade" : "RightOuterBlade",
				new Vector3(side * 5.05f, 0.16f, -0.1f),
				new Vector3(2.8f, 0.26f, 1.25f),
				_hull,
				false,
				side * 27.0f
			);

			AddBox(
				side < 0 ? "LeftEnergyChannel" : "RightEnergyChannel",
				new Vector3(side * 3.3f, 0.33f, -0.6f),
				new Vector3(2.5f, 0.06f, 0.12f),
				_energy,
				true,
				side * 15.0f
			);
		}
	}

	// Shows the positions of four cannons and the central beam.
	private void CreateWeapons()
	{
		float[] cannonX = { -4.4f, -1.45f, 1.45f, 4.4f };

		for (int index = 0; index < cannonX.Length; index++)
		{
			AddBox(
				$"CannonHousing_{index}",
				new Vector3(cannonX[index], 0.45f, -1.15f),
				new Vector3(0.65f, 0.45f, 1.3f),
				_edge
			);

			AddBox(
				$"CannonBarrel_{index}",
				new Vector3(cannonX[index], 0.45f, -2.05f),
				new Vector3(0.24f, 0.24f, 0.9f),
				_hull
			);
		}

		AddBox(
			"CentreBeamEmitter",
			new Vector3(0, 0.35f, -3.55f),
			new Vector3(0.42f, 0.42f, 0.25f),
			_energy,
			true
		);
	}

	// Places the purple seeker-core glow on the upper rear body.
	private void CreateCore()
	{
		SphereMesh mesh = new SphereMesh();
		mesh.Radius = 0.48f;
		mesh.Height = 0.96f;
		mesh.RadialSegments = 16;
		mesh.Rings = 8;

		StandardMaterial3D material = CreateMaterial(_core, true);
		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = "SeekerCore";
		visual.Position = new Vector3(0, 0.85f, 0.6f);
		visual.Mesh = mesh;
		AddChild(visual);
	}

	// Adds four visible purple exhaust ports at the back.
	private void CreateEngines()
	{
		float[] engineX = { -4.2f, -1.2f, 1.2f, 4.2f };

		for (int index = 0; index < engineX.Length; index++)
		{
			AddBox(
				$"EngineHousing_{index}",
				new Vector3(engineX[index], 0, 1.45f),
				new Vector3(0.65f, 0.55f, 1.25f),
				_hull
			);

			AddBox(
				$"EngineGlow_{index}",
				new Vector3(engineX[index], 0, 2.13f),
				new Vector3(0.38f, 0.33f, 0.08f),
				_energy,
				true
			);
		}
	}

	#endregion

	#region Mesh Helpers

	// Adds one visual box, optionally angled and emissive.
	private void AddBox(
		string name,
		Vector3 position,
		Vector3 size,
		Color color,
		bool glowing = false,
		float yRotation = 0.0f
	)
	{
		BoxMesh mesh = new BoxMesh();
		mesh.Size = size;
		mesh.Material = CreateMaterial(color, glowing);

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = name;
		visual.Position = position;
		visual.RotationDegrees = new Vector3(0, yRotation, 0);
		visual.Mesh = mesh;

		AddChild(visual);
	}

	// Creates the material used by the temporary primitives.
	private StandardMaterial3D CreateMaterial(Color color, bool glowing)
	{
		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		material.Metallic = glowing ? 0.0f : 0.65f;
		material.Roughness = glowing ? 0.3f : 0.32f;

		if (glowing)
		{
			material.EmissionEnabled = true;
			material.Emission = color;
			material.EmissionEnergyMultiplier = 2.5f;
		}

		return material;
	}

	#endregion
}

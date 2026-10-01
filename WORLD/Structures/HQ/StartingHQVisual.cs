using Godot;

// Generates a tall tapered HQ with a real horizontal passage.
// Separate collision pieces leave the corridor open along local Z.
[Tool]
public partial class StartingHQVisual : Node3D
{
	#region Settings

	[ExportGroup("Dimensions")]

	[Export] public float BodyRadius = 90.0f;
	[Export] public float UpperHeight = 135.0f;
	[Export] public float LowerHeight = 155.0f;
	[Export] public float CrownHeight = 70.0f;

	[ExportGroup("Corridor")]

	[Export] public float CorridorWidth = 48.0f;
	[Export] public float CorridorHeight = 32.0f;
	[Export] public float CorridorWallThickness = 4.0f;

	[ExportGroup("Appearance")]

	[Export] public Color HullColor =
		new Color(0.16f, 0.21f, 0.25f);

	[Export] public Color TrimColor =
		new Color(0.32f, 0.4f, 0.45f);

	[Export] public Color LightColor =
		new Color(0.1f, 0.8f, 1.0f);

	[ExportGroup("Editor")]

	[Export]
	public bool Rebuild
	{
		get => false;
		set
		{
			if (value && IsInsideTree())
			{
				CallDeferred(nameof(Build));
			}
		}
	}

	#endregion

	#region Runtime

	private Node3D _generated;

	#endregion

	#region Generation

	// =========================================================
	// Defers generation until the scene finishes entering the tree.
	// =========================================================
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		CallDeferred(nameof(Build));
	}

	// =========================================================
	// Rebuilds only the generated geometry, preserving authored nodes.
	// =========================================================
	private void Build()
	{
		if (!IsInsideTree() || IsQueuedForDeletion())
		{
			return;
		}

		if (GodotObject.IsInstanceValid(_generated))
		{
			RemoveChild(_generated);
			_generated.QueueFree();
		}

		_generated = new Node3D
		{
			Name = "GeneratedHQ"
		};

		AddChild(_generated);

		float radius = Mathf.Max(20.0f, BodyRadius);
		float wall = Mathf.Max(1.0f, CorridorWallThickness);

		float width = Mathf.Clamp(
			CorridorWidth,
			8.0f,
			radius * 1.5f
		);

		float height = Mathf.Max(8.0f, CorridorHeight);
		float halfHeight = height * 0.5f;
		float depth = radius * 2.0f;

		float upper = Mathf.Max(20.0f, UpperHeight);
		float lower = Mathf.Max(20.0f, LowerHeight);
		float crown = Mathf.Max(10.0f, CrownHeight);

		StandardMaterial3D hull = CreateMaterial(HullColor);
		StandardMaterial3D trim = CreateMaterial(TrimColor);

		StandardMaterial3D lights = CreateMaterial(LightColor);
		lights.EmissionEnabled = true;
		lights.Emission = LightColor;
		lights.EmissionEnergyMultiplier = 2.5f;

		// Each solid cylinder ends outside the open passage.
		AddCylinder(
			"UpperBody",
			halfHeight + wall + upper * 0.5f,
			upper,
			radius * 0.55f,
			radius,
			hull
		);

		AddCylinder(
			"LowerBody",
			-halfHeight - wall - lower * 0.5f,
			lower,
			radius,
			radius * 0.65f,
			hull
		);

		AddCylinder(
			"Crown",
			halfHeight + wall + upper + crown * 0.5f,
			crown,
			radius * 0.08f,
			radius * 0.55f,
			trim
		);

		// The middle section is assembled around an empty corridor.
		float sideWidth = radius - width * 0.5f;
		float sidePosition = width * 0.5f + sideWidth * 0.5f;

		AddBox(
			"LeftHangarBlock",
			new Vector3(-sidePosition, 0.0f, 0.0f),
			new Vector3(sideWidth, height, depth),
			hull,
			true
		);

		AddBox(
			"RightHangarBlock",
			new Vector3(sidePosition, 0.0f, 0.0f),
			new Vector3(sideWidth, height, depth),
			hull,
			true
		);

		AddBox(
			"CorridorFloor",
			new Vector3(0.0f, -halfHeight - wall * 0.5f, 0.0f),
			new Vector3(depth, wall, depth),
			trim,
			true
		);

		AddBox(
			"CorridorCeiling",
			new Vector3(0.0f, halfHeight + wall * 0.5f, 0.0f),
			new Vector3(depth, wall, depth),
			trim,
			true
		);

		// Small emissive strips mark the floor edges.
		for (int index = 0; index < 9; index++)
		{
			float z = Mathf.Lerp(
				-radius + 5.0f,
				radius - 5.0f,
				index / 8.0f
			);

			for (int side = -1; side <= 1; side += 2)
			{
				AddBox(
					$"GuideLight_{index}_{side}",
					new Vector3(
						side * (width * 0.5f - 1.0f),
						-halfHeight + 0.15f,
						z
					),
					new Vector3(0.35f, 0.2f, 4.0f),
					lights,
					false
				);
			}
		}
	}

	#endregion

	#region Parts

	// =========================================================
	// Creates a shared surface material for one set of HQ parts.
	// =========================================================
	private StandardMaterial3D CreateMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Metallic = 0.55f,
			Roughness = 0.7f
		};
	}

	// =========================================================
	// Adds one tapered cylinder with matching convex collision.
	// Each cylinder is solid and never spans the open corridor.
	// =========================================================
	private void AddCylinder(
		string name,
		float y,
		float height,
		float topRadius,
		float bottomRadius,
		Material material
	)
	{
		CylinderMesh mesh = new()
		{
			Height = height,
			TopRadius = topRadius,
			BottomRadius = bottomRadius,
			RadialSegments = 24,
			Rings = 1
		};

		StaticBody3D body = new()
		{
			Name = name,
			Position = new Vector3(0.0f, y, 0.0f),
			CollisionLayer = 1,
			CollisionMask = 1
		};

		_generated.AddChild(body);

		body.AddChild(new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = material
		});

		body.AddChild(new CollisionShape3D
		{
			Shape = mesh.CreateConvexShape()
		});
	}

	// =========================================================
	// Adds a box part with optional matching static collision.
	// =========================================================
	private void AddBox(
		string name,
		Vector3 position,
		Vector3 size,
		Material material,
		bool collision
	)
	{
		Node3D part = collision
			? new StaticBody3D
			{
				CollisionLayer = 1,
				CollisionMask = 1
			}
			: new Node3D();

		part.Name = name;
		part.Position = position;

		_generated.AddChild(part);

		part.AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh
			{
				Size = size
			},
			MaterialOverride = material
		});

		if (collision)
		{
			part.AddChild(new CollisionShape3D
			{
				Shape = new BoxShape3D
				{
					Size = size
				}
			});
		}
	}

	#endregion
}

using Godot;

public partial class EnemyShip : CharacterBody3D, IDamageable
{
	#region Definition

	[Export] public EnemyDefinition Definition;

	#endregion

	#region Runtime

	private ShipDefence _defence;

	public ShipDefence Defence => _defence;

	#endregion

	#region Setup

	// Reads the definition and creates this ship's defence, visual, and collision.
public override void _Ready()
{
	if (Definition == null)
	{
		GD.PushError($"{Name} needs an EnemyDefinition.");
		return;
	}

	_defence = new ShipDefence(
		Definition.MaxShield,
		Definition.MaxArmour,
		Definition.MaxHull
	);

	if (Definition.VisualScene != null)
	{
		Node3D visual = Definition.VisualScene.Instantiate<Node3D>();
		visual.Name = "Visual";
		AddChild(visual);
	}
	else
	{
		CreateVisuals();
	}

	CreateCollision();
}

	#endregion

	#region Damage

	// Passes projectile damage through the ship's three defence layers.
	public void ApplyDamage(DamageInfo damage)
	{
		if (_defence == null
			|| damage.Amount <= 0.0f
			|| IsQueuedForDeletion())
		{
			return;
		}

		_defence.ApplyDamage(damage.Amount);

		if (_defence.Destroyed)
		{
			QueueFree();
		}
	}

	#endregion

	#region Appearance

	// Assembles a simple ship pointing along local negative Z.
	private void CreateVisuals()
	{
		AddBox(
			"Hull",
			Vector3.Zero,
			Definition.HullSize,
			Definition.HullColor
		);

		AddBox(
			"Cockpit",
			new Vector3(0, 0.5f, -0.65f),
			new Vector3(0.9f, 0.5f, 1.4f),
			new Color(0.08f, 0.09f, 0.09f)
		);

		AddBox(
			"LeftWing",
			new Vector3(-1.8f, 0, 0.35f),
			Definition.WingSize,
			Definition.LeftWingColor
		);

		AddBox(
			"RightWing",
			new Vector3(1.8f, 0, 0.35f),
			Definition.WingSize,
			Definition.RightWingColor
		);

		AddBox(
			"LeftEngine",
			new Vector3(-0.55f, 0, 2.1f),
			new Vector3(0.35f, 0.35f, 0.2f),
			Definition.EngineColor,
			true
		);

		AddBox(
			"RightEngine",
			new Vector3(0.55f, 0, 2.1f),
			new Vector3(0.35f, 0.35f, 0.2f),
			Definition.EngineColor,
			true
		);
	}

	// Creates one collision shape covering the temporary ship body.
	private void CreateCollision()
	{
		BoxShape3D shape = new BoxShape3D();
		shape.Size = Definition.CollisionSize;

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = shape;

		AddChild(collision);
	}

	// Adds one decorative box with an optional glowing material.
	private void AddBox(
		string name,
		Vector3 position,
		Vector3 size,
		Color color,
		bool glowing = false
	)
	{
		BoxMesh mesh = new BoxMesh();
		mesh.Size = size;

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;

		if (glowing)
		{
			material.EmissionEnabled = true;
			material.Emission = color;
		}

		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = name;
		visual.Position = position;
		visual.Mesh = mesh;

		AddChild(visual);
	}

	#endregion
}

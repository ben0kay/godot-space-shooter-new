using Godot;

// Builds an enemy ship from its definition and manages its defence and controllers.
public partial class EnemyShip : CharacterBody3D, IDamageable, ICombatTarget
{
	#region Definition

	[Export] public EnemyDefinition Definition;

	#endregion

	#region Runtime

	private ShipDefence _defence;
	public ShipDefence Defence => _defence;
	public EnemyTargeting Targeting { get; private set; }
	public Faction CombatFaction => Definition?.Faction ?? Faction.Neutral;
	public bool IsCombatTargetable =>
		Definition != null
		&& _defence != null
		&& !_defence.Destroyed
		&& !IsQueuedForDeletion();
	public System.Collections.Generic.List<EnemyHardpoint> Hardpoints { get; } = new();
	private ShipShield _shield;

		private float _shieldRechargeRate;

	#endregion

	#region Setup

	// =========================================================
	// Creates defence, visuals, collision, controllers, and the shared shield.
	public override void _Ready()
	{
		// Recharge processing starts only after this ship loses shield.
		SetPhysicsProcess(false);

		if (Definition == null || Definition.Defence == null)
		{
			GD.PushError(
				$"{Name} needs an EnemyDefinition with Defence assigned."
			);

			return;
		}

		EnemyDefenceStats stats = Definition.Defence;

		_defence = new ShipDefence(
			stats.MaxShield,
			stats.MaxArmour,
			stats.MaxHull
		);

		_shieldRechargeRate = Mathf.Max(
			0.0f,
			stats.ShieldRechargeRate
		);

		AddToGroup("combat_targets");

		if (Definition.VisualScene != null)
		{
			Node3D visual =
				Definition.VisualScene.Instantiate<Node3D>();

			visual.Name = "Visual";

			AddChild(visual);
		}
		else
		{
			CreateVisuals();
		}

		CreateCollision();

		if (Definition.Ranges != null)
		{
			Targeting = new EnemyTargeting
			{
				Name = "Targeting"
			};

			Targeting.Initialize(this);
			AddChild(Targeting);
		}

		CreateHardpoints();

		_shield = ShipShield.Attach(
			this,
			_defence,
			CombatFaction,
			Definition.ShieldVisuals
		);

		CreateAttackController();

		if (Definition.MovementControllerScene == null)
		{
			return;
		}

		if (Definition.Handling == null || Definition.Ranges == null)
		{
			GD.PushError(
				$"{Name} needs Handling and Ranges for movement."
			);

			return;
		}

		EnemyMovementController controller =
			Definition.MovementControllerScene
				.Instantiate<EnemyMovementController>();

		controller.Name = "MovementController";
		controller.Initialize(this);

		AddChild(controller);
	}

// Creates this ship's attack runtime after its hardpoints have been registered.
private void CreateAttackController()
{
	if (Definition.AttackController == null)
	{
		return;
	}

	EnemyAttackController controller = new()
	{
		Name = "AttackController"
	};

	controller.Initialize(this, Definition.AttackController);
	AddChild(controller);
}

// Registers scene-placed hardpoints once when the ship is created.
private void CreateHardpoints()
{
	Hardpoints.Clear();

	Node3D visual = GetNodeOrNull<Node3D>("Visual");

	if (visual != null)
	{
		RegisterHardpoints(visual);
	}
}

// Finds mounts recursively so ships can organise them beneath any visual node.
private void RegisterHardpoints(Node parent)
{
	foreach (Node child in parent.GetChildren())
	{
		if (child is EnemyHardpoint hardpoint)
		{
			hardpoint.Initialize(this);
			Hardpoints.Add(hardpoint);
		}

		RegisterHardpoints(child);
	}
}

#endregion

	#region Damage

	// =========================================================
	// Resolves typed damage, updates shield feedback, and enables recovery.
	public void ApplyDamage(DamageInfo damage)
	{
		if (_defence == null
			|| _defence.Destroyed
			|| damage.Amount <= 0.0f
			|| IsQueuedForDeletion())
		{
			return;
		}

		float shieldBefore = _defence.Shield;

		_defence.ApplyDamage(damage.Amount, damage.Type);

		_shield?.NotifyDamage(shieldBefore, damage);

		if (_defence.Destroyed)
		{
			SetPhysicsProcess(false);
			QueueFree();
			return;
		}

		if (_shieldRechargeRate > 0.0f
			&& _defence.Shield < _defence.MaxShield)
		{
			SetPhysicsProcess(true);
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
	Vector3 size = Definition.CollisionSize;
	Node3D visual = GetNodeOrNull<Node3D>("Visual");

	if (visual != null)
	{
		size *= visual.Scale;
	}

	BoxShape3D shape = new BoxShape3D
	{
		Size = size
	};

	CollisionShape3D collision = new CollisionShape3D
	{
		Name = "Collision",
		Shape = shape
	};

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

		#region Shield Recovery

	// =========================================================
	// Recharges damaged shields and stops processing when recovery is complete.
	public override void _PhysicsProcess(double delta)
	{
		if (_defence == null
			|| _defence.Destroyed
			|| IsQueuedForDeletion()
			|| _shieldRechargeRate <= 0.0f)
		{
			SetPhysicsProcess(false);
			return;
		}

		bool changed = _defence.RechargeShield(
			_shieldRechargeRate * (float)delta
		);

		if (changed)
		{
			// Restores shield visibility and collision when recovering from zero.
			_shield?.Refresh();
		}

		if (_defence.Shield >= _defence.MaxShield)
		{
			SetPhysicsProcess(false);
		}
	}

	#endregion
}

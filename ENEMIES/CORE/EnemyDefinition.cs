using Godot;

// Stores a ship type's identity, defence, movement, appearance, and attacks.
[GlobalClass]
public partial class EnemyDefinition : Resource
{
	#region Identity

	[ExportGroup("Identity")]

	[Export] public string DisplayName = "Enemy Ship";
	[Export] public Faction Faction = Faction.Neutral;
	[Export] public EnemyRole Role = EnemyRole.Fighter;
	[Export] public EnemyClass Class = EnemyClass.Light;
	[Export] public EnemyRank Rank = EnemyRank.Common;

	#endregion

	#region Doctrine

[ExportGroup("Doctrine")]

[Export] public FactionDoctrine DoctrineOverride;

// =========================================================
// Uses this ship type's override or its shared faction doctrine.
// =========================================================
public FactionDoctrine GetDoctrine()
{
	return DoctrineOverride ?? FactionDoctrines.Get(Faction);
}

#endregion

		#region Defence

	[Export] public EnemyDefenceStats Defence;
	[Export] public ShieldVisualSettings ShieldVisuals;

	#endregion

	#region Flight And Awareness

	[Export] public EnemyHandlingStats Handling;
	[Export] public EnemyRangeStats Ranges;
	[Export] public PackedScene MovementControllerScene;
	[Export] public AsteroidResponse AsteroidResponse = AsteroidResponse.Ignore;

	#endregion

	#region Attacks

	[Export] public EnemyAttackControllerDefinition AttackController;

	#endregion

	#region Shape

	[Export] public PackedScene VisualScene;
	[Export] public Vector3 HullSize = new Vector3(2.0f, 0.7f, 4.0f);
	[Export] public Vector3 WingSize = new Vector3(2.2f, 0.25f, 1.6f);
	[Export] public Vector3 CollisionSize = new Vector3(5.8f, 0.9f, 4.3f);

	#endregion

	#region Colours

	[Export] public Color HullColor = new Color(0.33f, 0.29f, 0.24f);
	[Export] public Color LeftWingColor = new Color(0.59f, 0.54f, 0.43f);
	[Export] public Color RightWingColor = new Color(0.48f, 0.42f, 0.32f);
	[Export] public Color EngineColor = new Color(1.0f, 0.56f, 0.15f);

	#endregion

	#region Hardpoints

	[Export]
	public Godot.Collections.Array<HardpointDefinition> Hardpoints = new();

	#endregion

	#region Rewards

[Export] public EnemyRewardStats Reward = new();

#endregion
}

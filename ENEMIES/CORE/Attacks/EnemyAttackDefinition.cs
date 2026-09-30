using Godot;

public enum HardpointFireOrder
{
	Sequential,
	Random,
	All
}

[GlobalClass]
public partial class EnemyAttackDefinition : Resource
{
	#region Identity And Weapon

	[ExportGroup("Identity And Weapon")]
	[Export] public string Key = "attack";
	[Export] public string Channel = "primary";
	[Export] public string HardpointGroup = "cannons";
	[Export] public float Weight = 1.0f;
	[Export] public WeaponDefinition Weapon;

	#endregion

	#region Conditions And Aim

	[ExportGroup("Conditions And Aim")]
	[Export] public float RangeMin = 0.0f;
	[Export] public float RangeMax = 100.0f;
	[Export] public bool RequireLineOfSight = true;
	[Export] public float FireToleranceDegrees = 10.0f;

	#endregion

	#region Firing

	[ExportGroup("Firing")]
	[Export] public HardpointFireOrder Order = HardpointFireOrder.Sequential;

	// All fires a salvo per round; other orders fire one mount per round.
	[Export] public int VolleyRounds = 16;
	[Export] public float IntervalSeconds = 0.13f;
	[Export] public float CooldownSeconds = 2.5f;

	// A blocked or unaligned round waits, then skips to the next round.
	[Export] public float MaxRoundWaitSeconds = 0.5f;

	#endregion
}

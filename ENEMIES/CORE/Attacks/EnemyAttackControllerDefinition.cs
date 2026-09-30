using Godot;

[GlobalClass]
public partial class EnemyAttackControllerDefinition : Resource
{
	#region Scheduling

	[Export] public int MaxActiveChannels = 1;
	[Export] public float SelectionIntervalSeconds = 0.2f;

	#endregion

	#region Attacks

	[Export]
	public Godot.Collections.Array<EnemyAttackDefinition> Attacks = new();

	#endregion
}

using Godot;

// Defines the credit payout for destroying one enemy ship.
[GlobalClass]
public partial class EnemyRewardStats : Resource
{
	#region Reward

	[Export(PropertyHint.Range, "0,1000000,1")]
	public int Credits = 0;

	#endregion
}
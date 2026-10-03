using Godot;

// Defines starting subsystem condition without storing mutable runtime state.
[GlobalClass]
public partial class ShipSystemsDefinition : Resource
{
	#region Condition

	[Export(PropertyHint.Range, "1,1000,1")]
	public float MaximumCondition = 100.0f;

	#endregion
}

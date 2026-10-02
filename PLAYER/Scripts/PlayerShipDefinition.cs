using Godot;

// Groups player gameplay settings into one editable ship definition.
[GlobalClass]
public partial class PlayerShipDefinition : Resource
{
	#region Identity

	[Export] public string DisplayName = "Cyan Interceptor";

	#endregion

	#region Gameplay Stats

	[Export] public PlayerDefenceStats Defence = new();
	[Export] public PlayerHandlingStats Handling = new();
	[Export] public PlayerBoostStats Boost = new();
	[Export] public PlayerDashStats Dash = new();

	#endregion

	#region Presentation

	[Export] public ShieldVisualSettings ShieldVisuals;

	#endregion

	#region Defence Access

	// Existing systems can read capacities without owning their configuration.
	public float MaxShield => Defence.MaxShield;
	public float MaxArmour => Defence.MaxArmour;
	public float MaxHull => Defence.MaxHull;

	#endregion
}
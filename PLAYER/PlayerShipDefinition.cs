using Godot;

[GlobalClass]
public partial class PlayerShipDefinition : Resource
{
	#region Identity

	[Export] public string DisplayName = "Cyan Interceptor";

	#endregion

	#region Defence

	[Export] public float MaxShield = 100.0f;
	[Export] public float MaxArmour = 80.0f;
	[Export] public float MaxHull = 100.0f;

	#endregion
}

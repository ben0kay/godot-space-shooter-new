using Godot;

// Allows a weapon to override selected defaults of its shared beam.
[GlobalClass]
public partial class BeamLaunchOverrides : Resource
{
	#region Geometry

	[Export] public bool OverrideRange = false;
	[Export] public float Range = 120.0f;

	[Export] public bool OverrideWidth = false;
	[Export] public float Width = 0.12f;

	#endregion

	#region Behaviour

	[Export] public bool OverrideDamage = false;
	[Export] public float DamagePerSecond = 20.0f;

	[Export] public bool OverrideExtensionSpeed = false;
	[Export] public float ExtensionSpeed = 1000.0f;

	#endregion
}

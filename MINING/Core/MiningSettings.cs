using Godot;

// Configures resource extraction independently of a weapon's combat damage.
[GlobalClass]
public partial class MiningSettings : Resource
{
	#region Extraction

	[Export] public float UnitsPerSecond = 4.0f;
	[Export] public float Strength = 1.0f;

	#endregion
}
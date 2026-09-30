using Godot;

// Stores reusable defaults for an extending, sustained beam.
[GlobalClass]
public partial class BeamDefinition : Resource
{
	#region Geometry

	[Export] public float Range = 120.0f;
	[Export] public float Width = 0.12f;

	#endregion

	#region Behaviour

	[Export] public float ExtensionSpeed = 1000.0f;
	[Export] public float DamagePerSecond = 20.0f;

	[Export(PropertyHint.Layers3DPhysics)]
	public uint CollisionMask = 1;

	#endregion

	#region Appearance

	[Export] public bool UseFactionPalette = true;

	[Export] public Color EnergyColor = new Color(
		0.15f, 0.85f, 1.0f
	);

	[Export] public Color CoreColor = new Color(
		0.8f, 1.0f, 1.0f
	);

	[Export] public float EmissionEnergy = 3.0f;
	[Export] public float GlowWidthMultiplier = 3.0f;
	[Export] public float GlowOpacity = 0.12f;

	#endregion
}

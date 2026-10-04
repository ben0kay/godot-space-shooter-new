using Godot;

// Stores shared beam geometry, damage, animation, and particle settings.
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
	[Export] public float ReleaseDuration = 0.12f;

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

	[Export] public float EmissionEnergy = 1.0f;
	[Export] public float GlowWidthMultiplier = 3.0f;
	[Export] public float GlowOpacity = 0.1f;

	#endregion

	#region Flow Animation

	// World-space displacement: keep this small for a precise player beam.
	[Export] public float WobbleAmount = 0.035f;
	[Export] public float WobbleWavelength = 10.0f;
	[Export] public float WobbleSpeed = 9.0f;

	[Export] public float PulseAmount = 0.08f;
	[Export] public float PulseSpeed = 12.0f;

	[Export] public float BandSpacing = 8.0f;
	[Export] public float BandSpeed = 32.0f;
	[Export] public float BandStrength = 0.35f;

	#endregion

	#region Particles

	[Export] public bool ParticlesEnabled = true;

	// Amount is the particle budget for each reusable emitter.
	[Export] public int EmberAmount = 48;
	[Export] public float EmberLifetime = 0.45f;
	[Export] public float EmberSize = 0.045f;
	[Export] public float EmberSpeed = 0.65f;

	[Export] public int MuzzleAmount = 12;
	[Export] public int ImpactAmount = 24;

	#region Damage Classification

	[Export] public DamageType DamageType = global::DamageType.Neutral;

	#endregion

	#endregion
}

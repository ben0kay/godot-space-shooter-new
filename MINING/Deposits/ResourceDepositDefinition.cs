using Godot;

// Describes one deposit type, including its yield, mining requirement, and colour.
[GlobalClass]
public partial class ResourceDepositDefinition : Resource
{
	#region Identity

	[Export] public MiningResourceType ResourceType =
		MiningResourceType.Iron;

	#endregion

	#region Generation

	[Export] public float GenerationWeight = 10.0f;

	// Yield range for an asteroid with a radius of approximately 10.
	[Export] public Vector2 ReserveRange = new(12.0f, 24.0f);

	#endregion

	#region Mining

	[Export] public float RequiredStrength = 1.0f;

	#endregion

	#region Appearance

	[Export] public Color SurfaceColor =
		new(0.76f, 0.80f, 0.83f);

	[Export] public Color GlowColor =
		new(0.33f, 0.45f, 0.52f);

	[Export] public float EmissionEnergy = 0.7f;

	#endregion
}
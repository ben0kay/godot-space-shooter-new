// Defines mined materials and the shared interface used by mining tools.
public enum MiningResourceType
{
	Iron = 0,
	Carbon = 1,
	Silicon = 2,
	Copper = 3,
	Sulfur = 4,
	Ice = 5
}

public interface IMineable
{
	MiningResourceType ResourceType { get; }

	// =========================================================
	// Removes available material when the tool meets the strength requirement.
	float Extract(
		float requestedAmount,
		float miningStrength,
		out MiningResourceType resource
	);
}
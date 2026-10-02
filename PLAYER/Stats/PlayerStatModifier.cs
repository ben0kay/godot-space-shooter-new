using System;

// Describes one flat bonus, additive percentage, and independent multiplier.
public readonly struct StatModifier
{
	#region Values

	public readonly PlayerStat Stat;
	public readonly float Flat;
	public readonly float Percent;
	public readonly float Multiplier;

	#endregion

	#region Construction

	// =========================================================
	// Creates a modifier; 0.20 percent means +20%, while multiplier defaults to 1.
	public StatModifier(
		PlayerStat stat,
		float flat = 0.0f,
		float percent = 0.0f,
		float multiplier = 1.0f
	)
	{
		if (stat < 0 || stat >= PlayerStat.Count
			|| !float.IsFinite(flat)
			|| !float.IsFinite(percent)
			|| !float.IsFinite(multiplier)
			|| multiplier < 0.0f)
		{
			throw new ArgumentException("Invalid stat modifier.");
		}

		Stat = stat;
		Flat = flat;
		Percent = percent;
		Multiplier = multiplier;
	}

	#endregion
}
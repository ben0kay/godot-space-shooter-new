using System;

// Converts authored credit rewards into XP using shared class multipliers.
public static class EnemyRewardRules
{
	#region Configuration

	public const double CreditsPerExperience = 10.0;

	#endregion

	#region Calculation

	// =========================================================
	// Returns the experience multiplier for one enemy size class.
	public static double GetClassMultiplier(EnemyClass shipClass)
	{
		return shipClass switch
		{
			EnemyClass.Tiny => 0.5,
			EnemyClass.Light => 1.0,
			EnemyClass.Standard => 1.25,
			EnemyClass.Heavy => 1.5,
			EnemyClass.Superheavy => 2.0,
			EnemyClass.Capital => 3.0,
			EnemyClass.Titan => 5.0,
			_ => 1.0
		};
	}

	// =========================================================
	// Calculates whole XP, rounding only after applying the class multiplier.
	public static long GetExperience(int credits, EnemyClass shipClass)
	{
		if (credits <= 0) return 0;

		return (long)Math.Ceiling(
			credits / CreditsPerExperience * GetClassMultiplier(shipClass)
		);
	}

	#endregion
}
using System;

// Stores player credits and total experience independently of ship stats.
public sealed class PlayerProgression
{
	#region State

	public long Credits { get; private set; }
	public long Experience { get; private set; }

	public event Action Changed;

	#endregion

	#region Rewards

	// =========================================================
	// Grants a reward without allowing negative values or integer overflow.
	public void Grant(long credits, long experience)
	{
		if (credits < 0 || experience < 0)
			throw new ArgumentOutOfRangeException(nameof(credits));

		long nextCredits = checked(Credits + credits);
		long nextExperience = checked(Experience + experience);

		Credits = nextCredits;
		Experience = nextExperience;

		if (credits > 0 || experience > 0) Changed?.Invoke();
	}

	// =========================================================
	// Spends a complete credit cost without modifying experience.
	public bool TrySpendCredits(long amount)
	{
		if (amount < 0 || Credits < amount) return false;
		if (amount == 0) return true;

		Credits -= amount;
		Changed?.Invoke();
		return true;
	}

	#endregion
}
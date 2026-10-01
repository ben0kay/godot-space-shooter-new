using Godot;

public sealed class ShipDefence
{
	#region Values

	public float MaxShield { get; }
	public float MaxArmour { get; }
	public float MaxHull { get; }

	public float Shield { get; private set; }
	public float Armour { get; private set; }
	public float Hull { get; private set; }

	public bool Destroyed => Hull <= 0.0f;

	#endregion

	#region Setup

	// Creates full defence layers from a ship's maximum values.
	public ShipDefence(float maxShield, float maxArmour, float maxHull)
	{
		MaxShield = Mathf.Max(0.0f, maxShield);
		MaxArmour = Mathf.Max(0.0f, maxArmour);
		MaxHull = Mathf.Max(1.0f, maxHull);

		Shield = MaxShield;
		Armour = MaxArmour;
		Hull = MaxHull;
	}

	#endregion

	#region Damage

	// Carries excess damage through shield, armour, then hull.
	public void ApplyDamage(float amount)
	{
		if (amount <= 0.0f || Destroyed)
		{
			return;
		}

		float shieldDamage = Mathf.Min(Shield, amount);
		Shield -= shieldDamage;
		amount -= shieldDamage;

		float armourDamage = Mathf.Min(Armour, amount);
		Armour -= armourDamage;
		amount -= armourDamage;

		float hullDamage = Mathf.Min(Hull, amount);
		Hull -= hullDamage;
	}

	#endregion

		#region Recovery

	// =========================================================
	// Restores shield up to its maximum and returns whether the value changed.
	public bool RechargeShield(float amount)
	{
		if (Destroyed
			|| amount <= 0.0f
			|| Shield >= MaxShield)
		{
			return false;
		}

		float previous = Shield;

		Shield = Mathf.Min(
			MaxShield,
			Shield + amount
		);

		return Shield > previous;
	}

	#endregion
}

using Godot;

// Holds one ship's current defence and resolves typed damage across its layers.
public sealed class ShipDefence
{
	#region Values
	public float MaxShield { get; private set; }
	public float MaxArmour { get; private set; }
	public float MaxHull { get; private set; }

	public float Shield { get; private set; }
	public float Armour { get; private set; }
	public float Hull { get; private set; }

	public bool Destroyed => Hull <= 0.0f;

	#endregion


	#region Setup

	// =========================================================
	// Updates capacities without healing damage or reviving a destroyed ship.
	public void SetMaximums(float maxShield, float maxArmour, float maxHull)
	{
		MaxShield = Mathf.Max(0.0f, maxShield);
		MaxArmour = Mathf.Max(0.0f, maxArmour);
		MaxHull = Mathf.Max(1.0f, maxHull);

		Shield = Mathf.Min(Shield, MaxShield);
		Armour = Mathf.Min(Armour, MaxArmour);
		Hull = Mathf.Min(Hull, MaxHull);
	}

	// =========================================================
	// Creates independent runtime defence from the ship's maximum values.
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

	// =========================================================
	// Resolves each layer using remaining base damage and that layer's multiplier.
	public void ApplyDamage(
		float amount,
		DamageType type = DamageType.Neutral
	)
	{
		if (amount <= 0.0f || Destroyed)
		{
			return;
		}

		DamageTypeStats stats = DamageRules.GetStats(type);

		float shield = Shield;
		float armour = Armour;
		float hull = Hull;

		ApplyLayer(
			ref shield,
			ref amount,
			stats?.ShieldMultiplier ?? 1.0f
		);

		ApplyLayer(
			ref armour,
			ref amount,
			stats?.ArmourMultiplier ?? 1.0f
		);

		ApplyLayer(
			ref hull,
			ref amount,
			stats?.HullMultiplier ?? 1.0f
		);

		Shield = shield;
		Armour = armour;
		Hull = hull;
	}

	// =========================================================
	// Deducts the base damage consumed by a layer before passing overflow onward.
	private static void ApplyLayer(
		ref float layer,
		ref float remainingDamage,
		float multiplier
	)
	{
		if (layer <= 0.0f || remainingDamage <= 0.0f)
		{
			return;
		}

		multiplier = Mathf.Max(0.0f, multiplier);

		if (multiplier == 0.0f)
		{
			remainingDamage = 0.0f;
			return;
		}

		float damageToBreak = layer / multiplier;

		if (remainingDamage >= damageToBreak)
		{
			layer = 0.0f;

			remainingDamage = Mathf.Max(
				0.0f,
				remainingDamage - damageToBreak
			);
		}
		else
		{
			layer = Mathf.Max(
				0.0f,
				layer - remainingDamage * multiplier
			);

			remainingDamage = 0.0f;
		}
	}

	#endregion

	#region Recovery

	// =========================================================
	// Restores shield up to its maximum and reports whether the value changed.
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

using Godot;

// Loads shared damage balance once and exposes it to defence resolution.
public static class DamageRules
{
	#region Shared Rules

	private static DamageRulesDefinition _definition;

	#endregion

	#region Lookup

	// =========================================================
	// Loads the balance resource once, then returns one type's settings.
	public static DamageTypeStats GetStats(DamageType type)
	{
		if (_definition == null)
		{
			const string path =
				"res://COMBAT/Damage/DefaultDamageRules.tres";

			if (ResourceLoader.Exists(path))
			{
				_definition = GD.Load<DamageRulesDefinition>(path);
			}

			if (_definition == null)
			{
				GD.PushWarning(
					"DefaultDamageRules.tres is missing or invalid. "
					+ "Using the built-in damage multipliers."
				);

				_definition = new DamageRulesDefinition();
			}
		}

		return _definition.GetStats(type);
	}

	#endregion
}
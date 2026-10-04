using Godot;
using System.Collections.Generic;

// Loads and caches shared behaviour settings for each faction.
public static class FactionDoctrines
{
	#region Runtime

	private static readonly Dictionary<Faction, FactionDoctrine> _cache = new();

	#endregion

	#region Lookup

	// =========================================================
	// Loads a faction's doctrine once, using defaults when no resource exists.
	public static FactionDoctrine Get(Faction faction)
	{
		if (_cache.TryGetValue(faction, out FactionDoctrine cached))
			return cached;

		string path = $"res://FACTIONS/Doctrine/{faction}Doctrine.tres";
		FactionDoctrine doctrine = null;

		if (ResourceLoader.Exists(path))
			doctrine = GD.Load<FactionDoctrine>(path);

		if (doctrine == null)
		{
			doctrine = new FactionDoctrine();
			GD.PushWarning(
				$"No faction doctrine found at '{path}'. Using default doctrine."
			);
		}

		_cache[faction] = doctrine;
		return doctrine;
	}

	#endregion
}

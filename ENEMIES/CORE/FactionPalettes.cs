using System.Collections.Generic;
using Godot;

// Loads and caches faction palettes for shared weapon and ship effects.
public static class FactionPalettes
{
	#region Resources

	private static readonly Dictionary<Faction, FactionPalette> _cache = new();

	private static readonly Dictionary<Faction, string> _paths = new()
	{
		{ Faction.Player, "res://ENEMIES/CORE/Factions/PlayerPalette.tres" },
		{ Faction.Rebel, "res://ENEMIES/CORE/Factions/RebelPalette.tres" },
		{ Faction.Corporation, "res://ENEMIES/CORE/Factions/CorporationPalette.tres" },
		{ Faction.Simulant, "res://ENEMIES/CORE/Factions/SimulantPalette.tres" }
	};

	private static FactionPalette _fallback;

	#endregion

	#region Palette Access

	// Loads a faction's palette once, using a fallback when no resource is available.
	public static FactionPalette Get(Faction faction)
	{
		if (_cache.TryGetValue(faction, out FactionPalette cached))
		{
			return cached;
		}

		FactionPalette palette = null;

		if (_paths.TryGetValue(faction, out string path))
		{
			if (ResourceLoader.Exists(path))
			{
				palette = GD.Load<FactionPalette>(path);
			}

			if (palette == null)
			{
				GD.PushError($"Missing faction palette: {path}");
			}
		}

		_fallback ??= new FactionPalette();
		palette ??= _fallback;

		_cache[faction] = palette;
		return palette;
	}

	#endregion
}

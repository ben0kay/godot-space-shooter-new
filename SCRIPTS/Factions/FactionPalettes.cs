using System.Collections.Generic;
using Godot;

public static class FactionPalettes
{
	private static readonly Dictionary<Faction, FactionPalette> _cache = new();

	private static readonly Dictionary<Faction, string> _paths = new()
	{
		{ Faction.Player, "res://RESOURCES/Factions/PlayerPalette.tres" },
		{ Faction.Rebel, "res://RESOURCES/Factions/RebelPalette.tres" },
		{ Faction.Corporation, "res://RESOURCES/Factions/CorporationPalette.tres" },
		{ Faction.Simulant, "res://RESOURCES/Factions/SimulantPalette.tres" }
	};

	private static FactionPalette _fallback;

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

		_fallback ??= new FactionPalette();ffffff
		palette ??= _fallback;

		_cache[faction] = palette;
		return palette;
	}
}

using System.Collections.Generic;
using Godot;

// Loads and caches shared faction colours for ships, weapons and effects.
public static class FactionPalettes
{
	#region Resources

	private static readonly Dictionary<Faction, FactionPalette> _cache = new();
	private static FactionPalette _fallback;

	#endregion

	#region Lookup

	// =========================================================
	// Loads a faction palette once, falling back when none has been created.
	public static FactionPalette Get(Faction faction)
	{
		if (_cache.TryGetValue(faction, out FactionPalette cached))
			return cached;

		string path = $"res://FACTIONS/Palettes/{faction}Palette.tres";
		FactionPalette palette = null;

		if (ResourceLoader.Exists(path))
			palette = GD.Load<FactionPalette>(path);

		_fallback ??= new FactionPalette();
		palette ??= _fallback;

		_cache[faction] = palette;
		return palette;
	}

	#endregion
}

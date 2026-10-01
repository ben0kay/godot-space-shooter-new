using Godot;
using System.Collections.Generic;

// Resolves and caches shared doctrine resources by faction.
public static class FactionDoctrines
{
    #region Runtime

    private static readonly Dictionary<Faction, FactionDoctrine>
        _cache = new();

    #endregion

    #region Lookup

    // =========================================================
    // Returns a faction's shared doctrine, loading it once.
    // Missing resources use passive defaults and produce a warning.
    // =========================================================
    public static FactionDoctrine Get(Faction faction)
    {
        if (_cache.TryGetValue(faction, out FactionDoctrine cached))
        {
            return cached;
        }

        string path =
            $"res://ENEMIES/CORE/Doctrine/{faction}Doctrine.tres";

        FactionDoctrine doctrine = null;

        if (ResourceLoader.Exists(path))
        {
            doctrine = GD.Load<FactionDoctrine>(path);
        }

        if (doctrine == null)
        {
            doctrine = new FactionDoctrine();

            GD.PushWarning(
                $"No faction doctrine found at '{path}'. "
                + "Using default doctrine."
            );
        }

        _cache[faction] = doctrine;

        return doctrine;
    }

    #endregion
}
using Godot;
using System.Collections.Generic;

// Stores the shared enemy registrations used by debug and gameplay spawners.
[GlobalClass]
public partial class EnemyCatalog : Resource
{
	#region Registrations

	[Export]
	public Godot.Collections.Array<EnemyRegistration> Entries = new();

	#endregion

	#region Lookup

	// =========================================================
	// Finds a registered enemy by key when a spawner requests one.
	// This small catalog lookup does not run every frame.
	// =========================================================
	public EnemyRegistration Get(string key)
	{
		foreach (EnemyRegistration entry in Entries)
		{
			if (entry != null && entry.IsValid && entry.Key == key)
			{
				return entry;
			}
		}

		return null;
	}

	// =========================================================
	// Reports incomplete registrations and duplicate keys during setup.
	// =========================================================
	public void Validate()
	{
		HashSet<string> keys = new(System.StringComparer.Ordinal);

		foreach (EnemyRegistration entry in Entries)
		{
			if (entry == null || !entry.IsValid)
			{
				GD.PushError("EnemyCatalog contains an incomplete registration.");
				continue;
			}

			if (!keys.Add(entry.Key))
			{
				GD.PushError($"Duplicate enemy key: '{entry.Key}'.");
			}
		}
	}

	#endregion
}

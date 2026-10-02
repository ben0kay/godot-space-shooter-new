using System.Collections.Generic;
using Godot;

// Indexes shared item definitions by stable key and optional mining resource type.
[GlobalClass]
public partial class ItemCatalog : Resource
{
	#region Definitions

	[Export] public Godot.Collections.Array<ItemDefinition> Items = new();

	#endregion

	#region Runtime

	private readonly Dictionary<string, ItemDefinition> _byKey = new();
	private readonly Dictionary<MiningResourceType, ItemDefinition> _byMining = new();

	private bool _indexed;

	#endregion

	#region Lookup

	// =========================================================
	// Returns the definition belonging to one stable item key.
	public ItemDefinition Get(string key)
	{
		BuildIndex();

		return key != null
			&& _byKey.TryGetValue(key, out ItemDefinition item)
				? item
				: null;
	}

	// =========================================================
	// Resolves an existing mining resource into its general cargo item.
	public ItemDefinition Get(MiningResourceType resource)
	{
		BuildIndex();

		return _byMining.TryGetValue(resource, out ItemDefinition item)
			? item
			: null;
	}

	// =========================================================
	// Validates and indexes definitions once for this catalog instance.
	private void BuildIndex()
	{
		if (_indexed)
		{
			return;
		}

		_indexed = true;

		foreach (ItemDefinition item in Items)
		{
			if (item == null)
			{
				continue;
			}

			if (string.IsNullOrWhiteSpace(item.Key)
				|| item.StackMaximum < 1
				|| !float.IsFinite(item.UnitMass)
				|| item.UnitMass < 0.0f)
			{
				GD.PushError(
					"ItemCatalog contains an item with an invalid "
					+ "key, stack maximum, or unit mass."
				);

				continue;
			}

			if (_byKey.ContainsKey(item.Key))
			{
				GD.PushError($"Duplicate item key: {item.Key}");
				continue;
			}

			if (item.IsMiningResource
				&& _byMining.ContainsKey(item.MiningResource))
			{
				GD.PushError(
					$"Duplicate mining item mapping: {item.MiningResource}"
				);

				continue;
			}

			_byKey.Add(item.Key, item);

			if (item.IsMiningResource)
			{
				_byMining.Add(item.MiningResource, item);
			}
		}
	}

	#endregion
}
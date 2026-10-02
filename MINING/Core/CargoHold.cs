using System;
using System.Collections.Generic;
using Godot;

// Stores slotted cargo with shared item definitions, stack limits, and mass capacity.
public partial class CargoHold : Node
{
	#region Configuration

	[Export] public CargoDefinition Definition;
	[Export] public bool LogHarvest = true;

	#endregion

	#region Public State

	public event Action Changed;

	public int Columns => Definition != null
		? Mathf.Max(1, Definition.Columns)
		: 8;

	public int Rows => Definition != null
		? Mathf.Max(1, Definition.Rows)
		: 5;

	public int SlotCount => _slots.Length;

	public float MaximumMass => Definition != null
		? Mathf.Max(0.0f, Definition.MaximumMass)
		: 0.0f;

	public float UsedMass => _usedMass;
	public float TotalUnits => _totalUnits;

	#endregion

	#region Runtime

	private CargoSlot[] _slots = Array.Empty<CargoSlot>();

	private float _usedMass;
	private float _totalUnits;

	private bool _configured;

	#endregion

	#region Setup

	// =========================================================
	// Creates the ship's cargo slots without enabling per-frame updates.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		if (Definition == null || Definition.Catalog == null)
		{
			GD.PushError(
				"CargoHold requires a CargoDefinition with an ItemCatalog."
			);

			return;
		}

		_slots = new CargoSlot[Columns * Rows];
		_configured = true;
	}

	#endregion

	#region Queries

	// =========================================================
	// Returns a read-only snapshot of one slot or an empty value for invalid indices.
	public CargoSlot GetSlot(int index)
	{
		return index >= 0 && index < _slots.Length
			? _slots[index]
			: default;
	}

	// =========================================================
	// Returns the total amount of an item across all cargo slots.
	public float GetAmount(string itemKey)
	{
		float total = 0.0f;

		foreach (CargoSlot slot in _slots)
		{
			if (!slot.IsEmpty && slot.Item.Key == itemKey)
			{
				total += slot.Amount;
			}
		}

		return total;
	}

	// =========================================================
	// Preserves the existing mining resource lookup.
	public float GetAmount(MiningResourceType resource)
	{
		ItemDefinition item = Definition?.Catalog?.Get(resource);

		return item != null
			? GetAmount(item.Key)
			: 0.0f;
	}

	// =========================================================
	// Returns the amount of an item that both stack space and mass capacity permit.
	public float GetFreeSpace(string itemKey)
	{
		if (!_configured)
		{
			return 0.0f;
		}

		ItemDefinition item = Definition.Catalog.Get(itemKey);

		if (item == null)
		{
			return 0.0f;
		}

		float slotSpace = 0.0f;

		foreach (CargoSlot slot in _slots)
		{
			if (slot.IsEmpty)
			{
				slotSpace += item.StackMaximum;
			}
			else if (slot.Item.Key == item.Key)
			{
				slotSpace += Mathf.Max(
					0.0f,
					item.StackMaximum - slot.Amount
				);
			}
		}

		float massSpace = item.UnitMass > 0.0f
			? Mathf.Max(0.0f, MaximumMass - _usedMass) / item.UnitMass
			: slotSpace;

		float available = Mathf.Min(slotSpace, massSpace);

		return item.AllowFractionalAmounts
			? available
			: Mathf.Floor(available);
	}

	// =========================================================
	// Returns available cargo space for an existing mining resource.
	public float GetFreeSpace(MiningResourceType resource)
	{
		ItemDefinition item = Definition?.Catalog?.Get(resource);

		return item != null
			? GetFreeSpace(item.Key)
			: 0.0f;
	}

	#endregion

	#region Adding Items

	// =========================================================
	// Fills matching stacks before empty slots and returns the amount accepted.
	public float Add(string itemKey, float amount)
	{
		if (!_configured
			|| !float.IsFinite(amount)
			|| amount <= 0.0f)
		{
			return 0.0f;
		}

		ItemDefinition item = Definition.Catalog.Get(itemKey);

		if (item == null)
		{
			return 0.0f;
		}

		float requested = item.AllowFractionalAmounts
			? amount
			: Mathf.Floor(amount);

		float remaining = Mathf.Min(
			requested,
			GetFreeSpace(itemKey)
		);

		float accepted = 0.0f;

		// Fill existing stacks first.
		for (int index = 0;
			index < _slots.Length && remaining > 0.0f;
			index++)
		{
			CargoSlot slot = _slots[index];

			if (slot.IsEmpty || slot.Item.Key != item.Key)
			{
				continue;
			}

			float added = Mathf.Min(
				remaining,
				Mathf.Max(0.0f, item.StackMaximum - slot.Amount)
			);

			if (added <= 0.0f)
			{
				continue;
			}

			_slots[index] = new CargoSlot(
				item,
				slot.Amount + added
			);

			remaining -= added;
			accepted += added;
		}

		// Then occupy empty slots.
		for (int index = 0;
			index < _slots.Length && remaining > 0.0f;
			index++)
		{
			if (!_slots[index].IsEmpty)
			{
				continue;
			}

			float added = Mathf.Min(
				remaining,
				item.StackMaximum
			);

			_slots[index] = new CargoSlot(item, added);

			remaining -= added;
			accepted += added;
		}

		if (accepted > 0.0f)
		{
			_usedMass += accepted * item.UnitMass;
			_totalUnits += accepted;

			Changed?.Invoke();
		}

		return accepted;
	}

	// =========================================================
	// Converts mined material into its item definition and preserves harvest logging.
	public float Add(MiningResourceType resource, float amount)
	{
		ItemDefinition item = Definition?.Catalog?.Get(resource);

		if (item == null)
		{
			return 0.0f;
		}

		float previous = GetAmount(item.Key);
		float accepted = Add(item.Key, amount);

		if (LogHarvest && accepted > 0.0f)
		{
			float current = GetAmount(item.Key);

			if (Mathf.Floor(current) > Mathf.Floor(previous))
			{
				GD.Print($"Cargo — {item.DisplayName}: {current:0.0}");
			}
		}

		return accepted;
	}

	#endregion

	#region Slot Operations

	// =========================================================
	// Moves a stack, merges matching items, or swaps different items.
	public bool MoveSlot(int sourceIndex, int targetIndex)
	{
		if (!_configured
			|| sourceIndex == targetIndex
			|| sourceIndex < 0
			|| targetIndex < 0
			|| sourceIndex >= _slots.Length
			|| targetIndex >= _slots.Length)
		{
			return false;
		}

		CargoSlot source = _slots[sourceIndex];
		CargoSlot target = _slots[targetIndex];

		if (source.IsEmpty)
		{
			return false;
		}

		if (target.IsEmpty)
		{
			_slots[targetIndex] = source;
			_slots[sourceIndex] = default;
		}
		else if (source.Item.Key != target.Item.Key)
		{
			_slots[sourceIndex] = target;
			_slots[targetIndex] = source;
		}
		else
		{
			float moved = Mathf.Min(
				source.Amount,
				Mathf.Max(
					0.0f,
					target.Item.StackMaximum - target.Amount
				)
			);

			if (moved <= 0.0f)
			{
				return false;
			}

			_slots[targetIndex] = new CargoSlot(
				target.Item,
				target.Amount + moved
			);

			float remaining = source.Amount - moved;

			_slots[sourceIndex] = remaining > 0.0f
				? new CargoSlot(source.Item, remaining)
				: default;
		}

		Changed?.Invoke();
		return true;
	}

	// =========================================================
	// Sorts occupied slots by display name while preserving their stack contents.
	public void Sort()
	{
		if (!_configured)
		{
			return;
		}

		List<CargoSlot> occupied = new();

		foreach (CargoSlot slot in _slots)
		{
			if (!slot.IsEmpty)
			{
				occupied.Add(slot);
			}
		}

		occupied.Sort((left, right) =>
			string.Compare(
				left.Item.DisplayName,
				right.Item.DisplayName,
				StringComparison.OrdinalIgnoreCase
			)
		);

		for (int index = 0; index < _slots.Length; index++)
		{
			_slots[index] = index < occupied.Count
				? occupied[index]
				: default;
		}

		Changed?.Invoke();
	}

	// =========================================================
	// Removes a bounded amount from one slot and returns the amount removed.
	public float RemoveFromSlot(int index, float amount)
	{
		if (!_configured
			|| index < 0
			|| index >= _slots.Length
			|| !float.IsFinite(amount)
			|| amount <= 0.0f)
		{
			return 0.0f;
		}

		CargoSlot slot = _slots[index];

		if (slot.IsEmpty)
		{
			return 0.0f;
		}

		float requested = slot.Item.AllowFractionalAmounts
			? amount
			: Mathf.Floor(amount);

		float removed = Mathf.Min(requested, slot.Amount);

		if (removed <= 0.0f)
		{
			return 0.0f;
		}

		float remaining = slot.Amount - removed;

		_slots[index] = remaining > 0.0f
			? new CargoSlot(slot.Item, remaining)
			: default;

		_usedMass = Mathf.Max(
			0.0f,
			_usedMass - removed * slot.Item.UnitMass
		);

		_totalUnits = Mathf.Max(
			0.0f,
			_totalUnits - removed
		);

		Changed?.Invoke();
		return removed;
	}

	#endregion
}
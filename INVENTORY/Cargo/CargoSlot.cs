// Provides a read-only view of one cargo slot.
public readonly struct CargoSlot
{
	public readonly ItemDefinition Item;
	public readonly float Amount;

	public bool IsEmpty => Item == null || Amount <= 0.0f;

	// =========================================================
	// Records one item's definition and current stack amount.
	public CargoSlot(ItemDefinition item, float amount)
	{
		Item = item;
		Amount = amount;
	}
}
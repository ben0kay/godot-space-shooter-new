using Godot;

// Groups a ship's cargo layout, mass capacity, and shared item catalog.
[GlobalClass]
public partial class CargoDefinition : Resource
{
	#region Layout

	[Export] public int Columns = 8;
	[Export] public int Rows = 5;

	#endregion

	#region Capacity

	[Export] public float MaximumMass = 500.0f;

	#endregion

	#region Items

	[Export] public ItemCatalog Catalog;

	#endregion
}
using Godot;

// Describes a cargo item independently of deposits, mining tools, and interface code.
[GlobalClass]
public partial class ItemDefinition : Resource
{
	public enum ManufacturingLayer
	{
		Raw,
		Refined,
		Component,
		Product
	}

	public enum FunctionalType
	{
		Resource,
		Structural,
		Mechanical,
		Electrical,
		Ammunition,
		Equipment,
		Module,
		Drone,
		Weapon,
		Device
	}

	#region Identity

	[ExportGroup("Identity")]

	[Export] public string Key = "";
	[Export] public string DisplayName = "";

	[Export(PropertyHint.MultilineText)]
	public string Description = "";

	[Export] public ManufacturingLayer Layer = ManufacturingLayer.Raw;
	[Export] public FunctionalType Type = FunctionalType.Resource;

	#endregion

	#region Cargo

	[ExportGroup("Cargo")]

	[Export] public float UnitMass = 1.0f;
	[Export] public int StackMaximum = 99;

	// Raw materials accumulate continuously during mining.
	[Export] public bool AllowFractionalAmounts = false;

	#endregion

	#region Mining Connection

	[ExportGroup("Mining Connection")]

	[Export] public bool IsMiningResource = false;
	[Export] public MiningResourceType MiningResource;

	#endregion

	#region Inventory Appearance

	[ExportGroup("Inventory Appearance")]

	[Export] public Texture2D Icon;

	// Used by the procedural icon when no texture is assigned.
	[Export] public Color IconColor = new(0.76f, 0.80f, 0.83f);
	[Export] public Color IconGlow = new(0.33f, 0.45f, 0.52f);

	#endregion
}
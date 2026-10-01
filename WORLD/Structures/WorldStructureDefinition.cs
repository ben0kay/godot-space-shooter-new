using Godot;

// Stores shared identity settings for a world structure type.
[GlobalClass]
public partial class WorldStructureDefinition : Resource
{
	#region Identity

	[ExportGroup("Identity")]

	[Export] public string Key = "";
	[Export] public string DisplayName = "World Structure";
	[Export] public Faction Faction = global::Faction.Neutral;

	#endregion
}

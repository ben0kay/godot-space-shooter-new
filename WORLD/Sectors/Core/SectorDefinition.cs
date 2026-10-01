using Godot;

// Describes a sector without loading its scene or owning its runtime state.
[GlobalClass]
public partial class SectorDefinition : Resource
{
	#region Identity

	[ExportGroup("Identity")]

	[Export] public string Key = "";
	[Export] public string DisplayName = "";

	#endregion

	#region Scene

	[ExportGroup("Scene")]

	[Export(PropertyHint.File, "*.tscn")]
	public string ScenePath = "";

	[Export] public string DefaultArrivalKey = "default";

	#endregion

	#region Routes

	[ExportGroup("Routes")]

	// Connections are destination keys rather than references to other resources.
	[Export] public Godot.Collections.Array<string> Connections = new();

	#endregion

	#region Generation

	[ExportGroup("Generation")]

	// Reserved for repeatable procedural placement when generation is added.
	[Export] public int GenerationSeed = 1;

	#endregion
}
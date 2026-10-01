using Godot;

// Provides shared identity and registration for world structures.
// Optional components will provide facilities, defence, and docking.
public partial class WorldStructure : Node3D
{
	#region Settings

	[ExportGroup("Structure")]

	[Export] public WorldStructureDefinition Definition;
	[Export] public string PersistentId = "";

	#endregion

	#region Setup

	// =========================================================
	// Registers this instance without enabling continuous updates.
	// =========================================================
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		if (Definition == null)
		{
			GD.PushError(
				$"{Name}: assign a WorldStructureDefinition."
			);

			return;
		}

		AddToGroup("world_structures");
	}

	#endregion
}

using Godot;

// Represents one loaded sector and provides arrival and content ownership helpers.
public partial class WorldSector : Node3D
{
	#region Arrivals

	// =========================================================
	// Finds an arrival marker beneath the sector's Arrivals node.
	public Marker3D FindArrival(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}

		Node arrivals = GetNodeOrNull<Node>("Arrivals");

		return arrivals?.GetNodeOrNull<Marker3D>(key);
	}

	#endregion

	#region Content Ownership

	// =========================================================
	// Finds the owning sector, including for a persistent player firing into it.
	public static Node GetContentParent(Node context)
	{
		if (!GodotObject.IsInstanceValid(context)
			|| !context.IsInsideTree())
		{
			return null;s
		}

		Node ancestor = context;

		while (ancestor != null)
		{
			if (ancestor is WorldSector sector)
			{
				return sector;
			}

			ancestor = ancestor.GetParent();
		}

		SectorManager manager =
			context.GetTree().GetFirstNodeInGroup(
				"sector_manager"
			) as SectorManager;

		if (GodotObject.IsInstanceValid(manager?.CurrentSector))
		{
			return manager.CurrentSector;
		}

		// Standalone sandbox scenes continue using their own scene root.
		return context.GetTree().CurrentScene;
	}

	#endregion
}

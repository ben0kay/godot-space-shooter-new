using Godot;

// Shared helpers for finding scene references without fixed parent paths.
public static class NodeHelpers
{
	#region Hierarchy Helpers

	// =========================================================
	// Finds the nearest ancestor of the requested node type.
	// Allows components to live inside organisational containers.
	// =========================================================
	public static T FindAncestor<T>(Node node) where T : Node
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}

		Node ancestor = node.GetParent();

		while (ancestor != null)
		{
			if (ancestor is T match)
			{
				return match;
			}

			ancestor = ancestor.GetParent();
		}

		return null;
	}

	#endregion
}

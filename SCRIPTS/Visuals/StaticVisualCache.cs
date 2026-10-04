using Godot;
using System.Collections.Generic;

// Combines static hull meshes once and shares the result between identical ships.
// Attach beneath a visual root; moving hardpoints stay outside the cached geometry.
public partial class StaticVisualCache : Node
{
	#region Settings

	[Export] public string CacheKey { get; set; } = "";
	[Export] public NodePath GeometryPath { get; set; } = new("..");

	#endregion

	#region Cache

	private sealed class CachedPart
	{
		public ArrayMesh Mesh;
		public GeometryInstance3D.ShadowCastingSetting Shadow;
		public uint Layers;
	}

	private sealed class BuildGroup
	{
		public SurfaceTool Tool;
		public Material Material;
		public GeometryInstance3D.ShadowCastingSetting Shadow;
		public uint Layers;
	}

	private static readonly Dictionary<string, List<CachedPart>> _cache = new();
	private Node _visualRoot;

	#endregion

	#region Setup

	// =========================================================
	// Waits until the parent has finished creating its visual geometry.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		_visualRoot = GetParent();
		_visualRoot.Ready += CacheVisual;
	}

	// =========================================================
	// Disconnects the parent's signal when this helper leaves the tree.
	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_visualRoot))
			_visualRoot.Ready -= CacheVisual;
	}

	#endregion

	#region Baking

	// =========================================================
	// Replaces direct decorative meshes with shared, combined geometry.
	private void CacheVisual()
	{
		Node3D geometry = GetNodeOrNull<Node3D>(GeometryPath);

		if (geometry == null || string.IsNullOrWhiteSpace(CacheKey))
		{
			GD.PushWarning(
				$"StaticVisualCache '{Name}' needs a valid GeometryPath and CacheKey."
			);
			return;
		}

		List<MeshInstance3D> sources = new();

		foreach (Node child in geometry.GetChildren())
		{
			if (child is MeshInstance3D mesh && mesh.Mesh != null)
				sources.Add(mesh);
		}

		if (sources.Count == 0)
			return;

		if (!_cache.TryGetValue(CacheKey, out List<CachedPart> parts))
		{
			parts = BuildParts(sources);
			_cache.Add(CacheKey, parts);
		}

		// Remove the original meshes immediately to prevent duplicate rendering.
		foreach (MeshInstance3D source in sources)
		{
			geometry.RemoveChild(source);
			source.QueueFree();
		}

		Node3D baked = new() { Name = "CachedHull" };
		geometry.AddChild(baked);

		for (int i = 0; i < parts.Count; i++)
		{
			CachedPart part = parts[i];

			baked.AddChild(new MeshInstance3D
			{
				Name = $"HullPart_{i}",
				Mesh = part.Mesh,
				CastShadow = part.Shadow,
				Layers = part.Layers
			});
		}
	}

	// =========================================================
	// Groups matching materials and render settings into combined meshes.
	private static List<CachedPart> BuildParts(List<MeshInstance3D> sources)
	{
		Dictionary<
			(ulong MaterialId, GeometryInstance3D.ShadowCastingSetting Shadow, uint Layers),
			BuildGroup
		> groups = new();

		foreach (MeshInstance3D source in sources)
		{
			for (int surface = 0; surface < source.Mesh.GetSurfaceCount(); surface++)
			{


				Material material = source.GetActiveMaterial(surface);
				ulong materialId = material == null ? 0UL : material.GetInstanceId();
				var key = (materialId, source.CastShadow, source.Layers);

				if (!groups.TryGetValue(key, out BuildGroup group))
				{
					SurfaceTool tool = new();
					tool.Begin(Mesh.PrimitiveType.Triangles);

					group = new BuildGroup
					{
						Tool = tool,
						Material = material,
						Shadow = source.CastShadow,
						Layers = source.Layers
					};

					groups.Add(key, group);
				}

				// Bake each piece's local position, rotation and scale into its vertices.
				group.Tool.AppendFrom(source.Mesh, surface, source.Transform);
			}
		}

		List<CachedPart> parts = new();

		foreach (BuildGroup group in groups.Values)
		{
			ArrayMesh mesh = group.Tool.Commit();
			mesh.SurfaceSetMaterial(0, group.Material);
			group.Tool.Dispose();

			parts.Add(new CachedPart
			{
				Mesh = mesh,
				Shadow = group.Shadow,
				Layers = group.Layers
			});
		}

		return parts;
	}

	#endregion
}
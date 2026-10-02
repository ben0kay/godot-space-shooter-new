using System;
using System.Collections.Generic;
using Godot;

// Shares staggered distance and camera-frustum checks across world objects.
// Registered objects decide which cosmetic behaviours to simplify.
public partial class DistanceDetailManager : Node
{
	#region Detail Types

	public enum DetailTier { Near, Reduced, Distant }

	public readonly struct DetailState
	{
		public readonly DetailTier Tier;
		public readonly bool InCameraView;

		// =========================================================
		// Stores independent distance and camera-view information.
		// =========================================================
		public DetailState(DetailTier tier, bool inCameraView)
		{
			Tier = tier;
			InCameraView = inCameraView;
		}
	}

	private sealed class Entry
	{
		public Node3D Owner;
		public Action<DetailState> Apply;

		public float LocalBoundsRadius;
		public float NearDistance;
		public float DistantDistance;

		public double NextCheck;

		public DetailState State;

		public bool Initialized;
		public bool Active = true;
	}

	#endregion

	#region Runtime

	private readonly List<Entry> _entries = new();

	private readonly Plane[] _frustumPlanes = new Plane[6];

	private double _clock;
	private float _tickRemaining;

	#endregion

	#region Setup

	// =========================================================
	// Makes the shared manager discoverable from any gameplay scene.
	// =========================================================
	public override void _Ready()
	{
		AddToGroup("distance_detail_manager");
		SetPhysicsProcess(false);
	}

	// =========================================================
	// Finds the shared manager without requiring a static singleton.
	// =========================================================
	public static DistanceDetailManager Find(Node context)
	{
		if (!GodotObject.IsInstanceValid(context)
			|| !context.IsInsideTree())
		{
			return null;
		}

		return context.GetTree().GetFirstNodeInGroup(
            "distance_detail_manager"
		) as DistanceDetailManager;
	}

	#endregion

	#region Registration

	// =========================================================
	// Registers an object's bounds, distances and cosmetic callback.
	// The first check is staggered across the initial near interval.
	// =========================================================
	public void Register(
		Node3D owner,
		Action<DetailState> apply,
		float localBoundsRadius = 0.0f,
		float nearDistance = DistanceDetailConfig.NearDistance,
		float distantDistance = DistanceDetailConfig.DistantDistance
	)
	{
		if (!GodotObject.IsInstanceValid(owner) || apply == null)
		{
			return;
		}

		Unregister(owner);

		float near = Mathf.Max(0.0f, nearDistance);

		_entries.Add(new Entry
		{
			Owner = owner,
			Apply = apply,

			LocalBoundsRadius = Mathf.Max(
				0.0f,
				localBoundsRadius
			),

			NearDistance = near,

			DistantDistance = Mathf.Max(
				near
					+ DistanceDetailConfig.DistanceHysteresis * 2.0f,
				distantDistance
			),

			NextCheck = _clock + UpdateStagger.Offset(
				owner,
				DistanceDetailConfig.NearCheckInterval,
				20
			)
		});
	}

	// =========================================================
	// Marks registrations inactive without changing an active batch.
	// Their references are released immediately.
	// =========================================================
	public void Unregister(Node3D owner)
	{
		foreach (Entry entry in _entries)
		{
			if (!entry.Active || entry.Owner != owner)
			{
				continue;
			}

			entry.Active = false;
			entry.Owner = null;
			entry.Apply = null;
		}
	}

	#endregion

	#region Scheduled Updates

		// =========================================================
	// Reuses one camera lookup and one frustum lookup per batch.
	// Only due objects receive distance and visibility calculations.
	// =========================================================
	public override void _Process(double delta)
	{
		_clock += delta;
		_tickRemaining -= (float)delta;

		if (_tickRemaining > 0.0f)
		{
			return;
		}

		_tickRemaining = DistanceDetailConfig.ManagerInterval;

		RemoveInactiveEntries();

		if (_entries.Count == 0)
		{
			return;
		}

		Camera3D camera = GetViewport().GetCamera3D();

		if (!GodotObject.IsInstanceValid(camera))
		{
			return;
		}

		var planes = camera.GetFrustum();

		if (planes.Count != _frustumPlanes.Length)
		{
			return;
		}

		for (int index = 0; index < _frustumPlanes.Length; index++)
		{
			_frustumPlanes[index] = planes[index];
		}

		Vector3 observerPosition =
			camera.GetCameraTransform().Origin;

		// Newly registered objects wait until the next batch.
		int count = _entries.Count;

		for (int index = 0; index < count; index++)
		{
			Entry entry = _entries[index];

			if (!entry.Active || _clock < entry.NextCheck)
			{
				continue;
			}

			if (!GodotObject.IsInstanceValid(entry.Owner)
				|| !entry.Owner.IsInsideTree()
				|| entry.Owner.IsQueuedForDeletion())
			{
				entry.Active = false;
				entry.Owner = null;
				entry.Apply = null;
				continue;
			}

			Evaluate(entry, observerPosition);
		}
	}

	// =========================================================
	// Removes released registrations between update batches.
	// =========================================================
	private void RemoveInactiveEntries()
	{
		for (int index = _entries.Count - 1; index >= 0; index--)
		{
			if (!_entries[index].Active)
			{
				_entries.RemoveAt(index);
			}
		}
	}

	// =========================================================
	// Measures distance using squared values and tests conservative
	// object bounds against the cached camera-frustum planes.
	// =========================================================
	private void Evaluate(
		Entry entry,
		Vector3 observerPosition
	)
	{
		Vector3 position = entry.Owner.GlobalPosition;
		Basis basis = entry.Owner.GlobalBasis;

		float scale = Mathf.Max(
			basis.X.Length(),
			Mathf.Max(
				basis.Y.Length(),
				basis.Z.Length()
			)
		);

		float radius = entry.LocalBoundsRadius * scale;

		float squaredDistance =
			position.DistanceSquaredTo(observerPosition);

		DetailTier tier = SelectTier(
			entry,
			squaredDistance,
			radius
		);

		bool inCameraView = IntersectsCameraView(
			position,
			radius + DistanceDetailConfig.VisibilityPadding
		);

		entry.NextCheck = _clock + GetCheckInterval(tier);

		if (entry.Initialized
			&& entry.State.Tier == tier
			&& entry.State.InCameraView == inCameraView)
		{
			return;
		}

		entry.Initialized = true;
		entry.State = new DetailState(tier, inCameraView);

		entry.Apply(entry.State);
	}

	#endregion

	#region Bounds And Distance

	// =========================================================
	// Selects a distance tier with hysteresis to prevent flickering.
	// Thresholds include the object's conservative world radius.
	// =========================================================
	private static DetailTier SelectTier(
		Entry entry,
		float squaredDistance,
		float radius
	)
	{
		float near = entry.NearDistance;
		float distant = entry.DistantDistance;
		float buffer = DistanceDetailConfig.DistanceHysteresis;

		if (entry.Initialized)
		{
			near += entry.State.Tier == DetailTier.Near
				? buffer
				: -buffer;

			distant += entry.State.Tier == DetailTier.Distant
				? -buffer
				: buffer;
		}

		near = Mathf.Max(0.0f, near) + radius;
		distant = Mathf.Max(0.0f, distant) + radius;

		if (squaredDistance <= near * near)
		{
			return DetailTier.Near;
		}

		return squaredDistance <= distant * distant
			? DetailTier.Reduced
			: DetailTier.Distant;
	}

	// =========================================================
	// Rejects a sphere only when it lies entirely outside a plane.
	// This is a conservative view test, not an occlusion test.
	// =========================================================
	private bool IntersectsCameraView(
		Vector3 position,
		float radius
	)
	{
		for (int index = 0; index < _frustumPlanes.Length; index++)
		{
			if (_frustumPlanes[index].DistanceTo(position) > radius)
			{
				return false;
			}
		}

		return true;
	}

	// =========================================================
	// Reduces check frequency as cosmetic detail becomes less relevant.
	// =========================================================
	private static float GetCheckInterval(DetailTier tier)
	{
		return tier switch
		{
			DetailTier.Near =>
				DistanceDetailConfig.NearCheckInterval,

			DetailTier.Reduced =>
				DistanceDetailConfig.ReducedCheckInterval,

			_ => DistanceDetailConfig.DistantCheckInterval
		};
	}

	#endregion
}

using Godot;

// Handles one projectile's target acquisition and steering in three dimensions.
public sealed class ProjectileGuidance
{
	#region References

	private readonly Node3D _projectile;
	private readonly CollisionObject3D _source;
	private readonly Faction _sourceFaction;
	private readonly ProjectileGuidanceSettings _settings;

	#endregion

	#region Runtime

	private Node3D _target;
	private float _delayRemaining;
	private float _searchTimer;

	#endregion

	#region Setup

	// =========================================================
// Connects guidance settings and staggers target searches.
// =========================================================
public ProjectileGuidance(
    Node3D projectile,
    CollisionObject3D source,
    Faction sourceFaction,
    ProjectileGuidanceSettings settings
)
{
    _projectile = projectile;
    _source = source;
    _sourceFaction = sourceFaction;
    _settings = settings;

    _delayRemaining = Mathf.Max(
        0.0f,
        settings.GuidanceDelaySeconds
    );

    _searchTimer = UpdateStagger.Offset(
        projectile,
        Mathf.Max(0.02f, settings.ReacquireIntervalSeconds),
        10
    );
}

	// =========================================================
	// Accepts the firing ship's target without requiring an acquisition cone check.
	public void SetTarget(Node3D target)
	{
		if (IsValidTarget(target))
		{
			_target = target;
		}
	}

	#endregion

	#region Guidance Update

	// =========================================================
	// Updates targeting and turns the velocity without changing its speed.
	public Vector3 Update(Vector3 velocity, float seconds)
	{
		if (velocity.LengthSquared() < 0.0001f)
		{
			return velocity;
		}

		if (_delayRemaining > 0.0f)
		{
			_delayRemaining = Mathf.Max(
				0.0f,
				_delayRemaining - seconds
			);

			return velocity;
		}

		if (!IsValidTarget(_target))
		{
			_target = null;
		}

		_searchTimer -= seconds;

		if (_searchTimer <= 0.0f)
		{
			_searchTimer = Mathf.Max(
				0.02f,
				_settings.ReacquireIntervalSeconds
			);

			if (_target == null || !_settings.RetainAssignedTarget)
			{
				_target = FindTarget(velocity.Normalized());
			}
		}

		if (_target == null)
		{
			return velocity;
		}

		float speed = velocity.Length();

		Vector3 aimPosition = GetAimPosition(_target, speed);
		Vector3 offset = aimPosition - _projectile.GlobalPosition;

		if (offset.LengthSquared() < 0.0001f)
		{
			return velocity;
		}

		float turnStep = Mathf.DegToRad(
			Mathf.Max(0.0f, _settings.TurnSpeedDegrees)
		) * Mathf.Max(0.0f, _settings.GuidanceStrength) * seconds;

		Vector3 direction = TurnToward(
			velocity / speed,
			offset.Normalized(),
			turnStep
		);

		return direction * speed;
	}

	// =========================================================
	// Predicts a moving target's position using the configured lead strength.
	private Vector3 GetAimPosition(Node3D target, float speed)
	{
		Vector3 position = target.GlobalPosition;

		if (_settings.LeadStrength <= 0.0f
			|| target is not CharacterBody3D movingTarget)
		{
			return position;
		}

		float distance = _projectile.GlobalPosition.DistanceTo(position);

		float leadTime = Mathf.Clamp(
			distance / Mathf.Max(0.01f, speed)
				* _settings.LeadStrength,
			0.0f,
			Mathf.Max(0.0f, _settings.MaxLeadSeconds)
		);

		return position + movingTarget.Velocity * leadTime;
	}

	// =========================================================
	// Rotates toward a direction with a strict angular limit, including 180° turns.
	private static Vector3 TurnToward(
		Vector3 current,
		Vector3 desired,
		float maximumAngle
	)
	{
		float angle = Mathf.Acos(
			Mathf.Clamp(current.Dot(desired), -1.0f, 1.0f)
		);

		if (angle <= maximumAngle || angle < 0.0001f)
		{
			return desired;
		}

		if (maximumAngle <= 0.0f)
		{
			return current;
		}

		Vector3 axis = current.Cross(desired);

		if (axis.LengthSquared() < 0.0001f)
		{
			// Opposite directions need a stable perpendicular turning axis.
			Vector3 reference =
				Mathf.Abs(current.Dot(Vector3.Up)) < 0.99f
					? Vector3.Up
					: Vector3.Right;

			axis = current.Cross(reference);
		}

		return current.Rotated(
			axis.Normalized(),
			maximumAngle
		).Normalized();
	}

	#endregion

	#region Target Acquisition

	// =========================================================
	// Finds the nearest hostile combat target inside the acquisition cone.
	private Node3D FindTarget(Vector3 forward)
	{
		Node3D nearest = null;

		float range = Mathf.Max(0.0f, _settings.AcquireRange);
		float nearestDistanceSquared = range * range;

		float coneAngle = Mathf.Clamp(
			_settings.LockAngleDegrees,
			0.0f,
			360.0f
		);

		float minimumAlignment = Mathf.Cos(
			Mathf.DegToRad(coneAngle * 0.5f)
		);

		foreach (
			Node node in _projectile.GetTree()
				.GetNodesInGroup("combat_targets")
		)
		{
			if (node is not Node3D candidate
				|| !IsValidTarget(candidate))
			{
				continue;
			}

			Vector3 offset =
				candidate.GlobalPosition - _projectile.GlobalPosition;

			float distanceSquared = offset.LengthSquared();

			if (distanceSquared >= nearestDistanceSquared)
			{
				continue;
			}

			if (coneAngle < 360.0f
				&& distanceSquared > 0.0001f
				&& forward.Dot(offset.Normalized()) < minimumAlignment)
			{
				continue;
			}

			nearest = candidate;
			nearestDistanceSquared = distanceSquared;
		}

		return nearest;
	}

	// =========================================================
	// Rejects the owner, allies, dead targets, and targets outside guidance range.
	private bool IsValidTarget(Node3D target)
	{
		if (!GodotObject.IsInstanceValid(target)
			|| target.IsQueuedForDeletion()
			|| target == _source
			|| target is not ICombatTarget combatTarget
			|| !combatTarget.IsCombatTargetable)
		{
			return false;
		}

		if (FactionHostility.Get(
			_sourceFaction,
			combatTarget.CombatFaction
		) <= 0.0f)
		{
			return false;
		}

		float range = Mathf.Max(0.0f, _settings.AcquireRange);

		return _projectile.GlobalPosition.DistanceSquaredTo(
			target.GlobalPosition
		) <= range * range;
	}

	#endregion
}

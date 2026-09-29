using Godot;

public partial class EnemyTargeting : Node
{
	private EnemyShip _ship;
	private float _searchTimer;

	public Node3D Target { get; private set; }

	public void Initialize(EnemyShip ship)
	{
		_ship = ship;

		_searchTimer =
			(ship.GetInstanceId() % 10) / 10.0f
			* EnemyUpdateConfig.TargetSearchInterval;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_ship?.Definition?.Ranges == null)
		{
			return;
		}

		EnemyRangeStats ranges = _ship.Definition.Ranges;

		if (IsValidTarget(Target)
			&& _ship.GlobalPosition.DistanceSquaredTo(Target.GlobalPosition)
				<= ranges.Forget * ranges.Forget)
		{
			return;
		}

		Target = null;
		_searchTimer -= (float)delta;

		if (_searchTimer > 0.0f)
		{
			return;
		}

		_searchTimer = EnemyUpdateConfig.TargetSearchInterval;
		float nearestDistanceSquared = ranges.Detection * ranges.Detection;

		foreach (Node node in GetTree().GetNodesInGroup("combat_targets"))
		{
			if (node is not Node3D candidate || !IsValidTarget(candidate))
			{
				continue;
			}

			float distanceSquared =
				_ship.GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);

			if (distanceSquared >= nearestDistanceSquared)
			{
				continue;
			}

			Target = candidate;
			nearestDistanceSquared = distanceSquared;
		}
	}

	private bool IsValidTarget(Node3D candidate)
	{
		if (!GodotObject.IsInstanceValid(candidate)
			|| candidate == _ship
			|| candidate.IsQueuedForDeletion()
			|| candidate is not ICombatTarget combatTarget
			|| !combatTarget.IsCombatTargetable)
		{
			return false;
		}

		return FactionHostility.Get(
			_ship.Definition.Faction,
			combatTarget.CombatFaction
		) > 0.0f;
	}
}

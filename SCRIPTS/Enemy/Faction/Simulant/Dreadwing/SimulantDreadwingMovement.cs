using Godot;

public partial class SimulantDreadwingMovement : EnemyMovementController
{
	private PlayerShip _target;
	private float _targetSearchTimer;

	protected override Vector3 GetDesiredVelocity(float seconds)
	{
		UpdateTarget(seconds);

		if (_target == null || Ship.Definition.Ranges == null)
		{
			return Vector3.Zero;
		}

		Vector3 toTarget = _target.GlobalPosition - Ship.GlobalPosition;
		float distance = toTarget.Length();

		if (distance < 0.001f)
		{
			return Vector3.Zero;
		}

		EnemyRangeStats ranges = Ship.Definition.Ranges;
		float speed = Ship.Definition.Handling.MaxSpeed;

		if (distance > ranges.Combat)
		{
			return toTarget.Normalized() * speed;
		}

		if (distance < ranges.BackAway)
		{
			return -toTarget.Normalized() * speed * 0.5f;
		}

		return Vector3.Zero;
	}

	protected override Vector3 GetFacingDirection()
	{
		return _target == null
			? Vector3.Zero
			: _target.GlobalPosition - Ship.GlobalPosition;
	}

	private void UpdateTarget(float seconds)
	{
		EnemyRangeStats ranges = Ship.Definition.Ranges;

		if (IsTargetValid(_target)
			&& Ship.GlobalPosition.DistanceTo(_target.GlobalPosition) <= ranges.Forget)
		{
			return;
		}

		_target = null;
		_targetSearchTimer -= seconds;

		if (_targetSearchTimer > 0.0f)
		{
			return;
		}

		_targetSearchTimer = EnemyUpdateConfig.TargetSearchInterval;

		foreach (Node node in GetTree().GetNodesInGroup("player_ship"))
		{
			if (node is not PlayerShip player || !IsTargetValid(player))
			{
				continue;
			}

			if (Ship.GlobalPosition.DistanceTo(player.GlobalPosition) <= ranges.Detection)
			{
				_target = player;
				return;
			}
		}
	}

	private bool IsTargetValid(PlayerShip player)
	{
		return GodotObject.IsInstanceValid(player)
			&& !player.IsQueuedForDeletion()
			&& player.Defence != null
			&& !player.Defence.Destroyed;
	}
}

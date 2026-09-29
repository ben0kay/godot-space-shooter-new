using Godot;

public partial class SimulantDreadwingMovement : EnemyMovementController
{
	private const float LOS_CHECK_INTERVAL = 0.2f;
	private const float FLANK_CLEARANCE = 8.0f;

	private float _losTimer;
	private bool _lineOfSightClear = true;
	private Vector3 _flankPoint;
	private Asteroid _blockingAsteroid;
	private int _flankSide;

	protected override Vector3 GetDesiredVelocity(float seconds)
	{
		Node3D target = Ship.Targeting?.Target;

		if (!GodotObject.IsInstanceValid(target)
			|| Ship.Definition.Ranges == null)
		{
			_blockingAsteroid = null;
			return Vector3.Zero;
		}

		Vector3 toTarget = target.GlobalPosition - Ship.GlobalPosition;
		float distance = toTarget.Length();

		if (distance < 0.001f)
		{
			return Vector3.Zero;
		}

		EnemyRangeStats ranges = Ship.Definition.Ranges;
		float speed = Ship.Definition.Handling.MaxSpeed;

		_losTimer -= seconds;

		if (_losTimer <= 0.0f)
		{
			_losTimer = LOS_CHECK_INTERVAL;
			UpdateLineOfSight(target);
		}

		// A blocked shot needs movement even inside combat range.
		if (!_lineOfSightClear
			&& GodotObject.IsInstanceValid(_blockingAsteroid))
		{
			Vector3 toFlank = _flankPoint - Ship.GlobalPosition;

			if (toFlank.LengthSquared() > 1.0f)
			{
				return toFlank.Normalized() * speed;
			}

			// Keep moving around the obstruction if the first flank
			// position does not yet provide a clear shot.
			Vector3 tangent = (
				_flankPoint - _blockingAsteroid.GlobalPosition
			).Normalized();

			return tangent * speed * 0.5f;
		}

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
		Node3D target = Ship.Targeting?.Target;

		return GodotObject.IsInstanceValid(target)
			? target.GlobalPosition - Ship.GlobalPosition
			: Vector3.Zero;
	}

	private void UpdateLineOfSight(Node3D target)
	{
		Asteroid blocker = FindBlockingAsteroid(
			Ship.GlobalPosition,
			target.GlobalPosition
		);

		_lineOfSightClear = blocker == null;

		if (blocker == null)
		{
			_blockingAsteroid = null;
			return;
		}

		if (blocker != _blockingAsteroid)
		{
			_blockingAsteroid = blocker;
			_flankSide = 0;
		}

		Vector3 towardTarget =
			(target.GlobalPosition - Ship.GlobalPosition).Normalized();

		Vector3 side = towardTarget.Cross(Vector3.Up).Normalized();

		// Directly above or below the target, choose another axis.
		if (side.LengthSquared() < 0.001f)
		{
			side = towardTarget.Cross(Vector3.Right).Normalized();
		}

		float asteroidRadius = GetAsteroidRadius(blocker);
		float shipHalfWidth = Ship.Definition.CollisionSize.X * 0.5f;

		Node3D visual = Ship.GetNodeOrNull<Node3D>("Visual");

		if (visual != null)
		{
			shipHalfWidth *= visual.Scale.X;
		}

		float offset = asteroidRadius + shipHalfWidth + FLANK_CLEARANCE;

		Vector3 left = blocker.GlobalPosition - side * offset;
		Vector3 right = blocker.GlobalPosition + side * offset;

		if (_flankSide == 0)
		{
			bool leftClear =
				FindBlockingAsteroid(left, target.GlobalPosition) == null;

			bool rightClear =
				FindBlockingAsteroid(right, target.GlobalPosition) == null;

			if (leftClear != rightClear)
			{
				_flankSide = leftClear ? -1 : 1;
			}
			else
			{
				float leftDistance =
					Ship.GlobalPosition.DistanceSquaredTo(left);

				float rightDistance =
					Ship.GlobalPosition.DistanceSquaredTo(right);

				_flankSide = leftDistance <= rightDistance ? -1 : 1;
			}
		}

		_flankPoint = _flankSide < 0 ? left : right;
	}

	private Asteroid FindBlockingAsteroid(Vector3 start, Vector3 end)
	{
		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(start, end);

		query.Exclude = new Godot.Collections.Array<Rid>
		{
			Ship.GetRid()
		};

		var hit = Ship.GetWorld3D().DirectSpaceState.IntersectRay(query);

		return hit.Count > 0
			? hit["collider"].AsGodotObject() as Asteroid
			: null;
	}

private float GetAsteroidRadius(Asteroid asteroid)
{
	MeshInstance3D mesh =
		asteroid.GetNodeOrNull<MeshInstance3D>("Visual");

	if (mesh?.Mesh == null)
	{
		return 5.0f;
	}

	Vector3 scale = asteroid.GlobalBasis.Scale;
	float largestScale = Mathf.Max(
		scale.X,
		Mathf.Max(scale.Y, scale.Z)
	);

	return mesh.Mesh.GetAabb().Size.Length()
		* 0.5f
		* largestScale;
}

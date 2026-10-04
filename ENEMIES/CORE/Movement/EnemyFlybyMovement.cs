using Godot;

// Makes repeated offset attack runs using turn-limited three-dimensional flight.
public partial class EnemyFlybyMovement : EnemyMovementController
{
	#region Configuration

	[ExportGroup("Attack Runs")]
	[Export] public Vector2 PassOffsetRange = new(18, 32);
	[Export] public float ExitDistance = 120.0f;
	[Export] public float ArrivalRadius = 16.0f;
	[Export] public float TurnaroundSeconds = 0.4f;
	[Export] public float MaximumRunSeconds = 12.0f;
	[Export] public bool AlternateSide = true;

	#endregion

	#region Runtime

	private readonly RandomNumberGenerator _random = new();
	private Node3D _runTarget;
	private Vector3 _destination;
	private float _runSeconds;
	private float _turnaround;
	private int _side = 1;
	private bool _active;

	#endregion

	#region Setup

	// =========================================================
	// Caches the visual through the shared controller and varies initial run side.
	public override void _Ready()
	{
		base._Ready();
		_random.Randomize();
		_side = _random.RandiRange(0, 1) == 0 ? -1 : 1;
	}

	#endregion

	#region Flight

	// =========================================================
	// Turns the hull through 3D space and accelerates along its actual facing.
	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(Ship)
			|| !Ship.IsCombatTargetable
			|| Ship.Definition?.Handling == null)
			return;

		float seconds = (float)delta;
		EnemyHandlingStats handling = Ship.Definition.Handling;

		Vector3 requested = GetDesiredVelocity(seconds);
		requested = ApplyAsteroidResponse(requested, seconds);

		bool moving = requested.LengthSquared() > 0.001f;
		float bank = 0.0f;

		if (moving)
		{
			Vector3 direction = requested.Normalized();
			Basis current = Ship.GlobalBasis.Orthonormalized();

			// Bank into the requested turn while keeping pitch on the hull.
			float lateral = direction.Dot(current.X);
			bank = -lateral * Mathf.DegToRad(handling.MaxBankDegrees);

			Vector3 up = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.98f
				? current.X : Vector3.Up;

			Quaternion from = current.GetRotationQuaternion();
			Quaternion to = Basis.LookingAt(direction, up)
				.GetRotationQuaternion();

			float angle = from.AngleTo(to);
			float maximumStep = Mathf.DegToRad(
				Mathf.Max(0.0f, handling.TurnSpeedDegrees)
			) * seconds;

			float weight = angle > 0.0001f
				? Mathf.Min(1.0f, maximumStep / angle) : 1.0f;

			Ship.GlobalBasis = new Basis(from.Slerp(to, weight));
		}

		Vector3 velocity = moving
			? -Ship.GlobalBasis.Z.Normalized() * requested.Length()
			: Vector3.Zero;

		Ship.Velocity = Ship.Velocity.MoveToward(
			velocity,
			Mathf.Max(0.0f, moving
				? handling.Acceleration : handling.Braking) * seconds
		);

		Ship.MoveAndSlide();

		if (GodotObject.IsInstanceValid(Visual))
		{
			Vector3 rotation = Visual.Rotation;
			rotation.X = 0.0f;
			rotation.Z = Mathf.Lerp(
				rotation.Z, bank,
				1.0f - Mathf.Exp(
					-Mathf.Max(0.0f, handling.BankResponse) * seconds
				)
			);
			Visual.Rotation = rotation;
		}
	}

	// =========================================================
	// Commits to a pass destination rather than following every target movement.
	protected override Vector3 GetDesiredVelocity(float seconds)
	{
		Node3D target = Ship.Targeting?.Target;

		if (!GodotObject.IsInstanceValid(target)
			|| target.IsQueuedForDeletion())
		{
			_active = false;
			_runTarget = null;
			_turnaround = 0.0f;
			return Vector3.Zero;
		}

		if (target != _runTarget)
		{
			_active = false;
			_turnaround = 0.0f;
			_runTarget = target;
		}

		if (!_active)
		{
			_turnaround = Mathf.Max(0.0f, _turnaround - seconds);

			if (_turnaround > 0.0f)
			{
				// Coast through the brief interval between attack runs.
				return -Ship.GlobalBasis.Z.Normalized()
					* Ship.Definition.Handling.MaxSpeed;
			}

			BeginRun(target);
		}

		_runSeconds += seconds;
		Vector3 offset = _destination - Ship.GlobalPosition;
		float radius = Mathf.Max(1.0f, ArrivalRadius);

		if (offset.LengthSquared() <= radius * radius
			|| _runSeconds >= Mathf.Max(1.0f, MaximumRunSeconds))
		{
			_active = false;
			_turnaround = Mathf.Max(0.0f, TurnaroundSeconds);

			return -Ship.GlobalBasis.Z.Normalized()
				* Ship.Definition.Handling.MaxSpeed;
		}

		return offset.Normalized()
			* Mathf.Max(0.0f, Ship.Definition.Handling.MaxSpeed);
	}

	// =========================================================
	// Chooses a destination beyond and beside the target for this entire pass.
	private void BeginRun(Node3D target)
	{
		Vector3 toward = target.GlobalPosition - Ship.GlobalPosition;

		if (toward.LengthSquared() < 0.001f)
			toward = -Ship.GlobalBasis.Z;

		toward = toward.Normalized();

		Vector3 reference = Mathf.Abs(toward.Dot(Vector3.Up)) > 0.98f
			? Vector3.Right : Vector3.Up;

		Vector3 side = toward.Cross(reference).Normalized();

		float minimum = Mathf.Max(
			0.0f, Mathf.Min(PassOffsetRange.X, PassOffsetRange.Y)
		);
		float maximum = Mathf.Max(
			minimum, Mathf.Max(PassOffsetRange.X, PassOffsetRange.Y)
		);

		_destination = target.GlobalPosition
			+ toward * Mathf.Max(1.0f, ExitDistance)
			+ side * _side * _random.RandfRange(minimum, maximum);

		_runSeconds = 0.0f;
		_active = true;

		_side = AlternateSide
			? -_side : (_random.RandiRange(0, 1) == 0 ? -1 : 1);
	}

	#endregion
}

using Godot;

public partial class EnemyMovementController : Node
{
	#region References

	protected EnemyShip Ship;
	protected Node3D Visual;
	private float _asteroidProbeTimer;
	private Vector3 _asteroidSteering;

	#endregion

	#region Setup

	// Stores a reference to the enemy ship controlled by this component.
	public void Initialize(EnemyShip ship)
	{
		Ship = ship;

		// Different ships begin their probes at different points in the interval.
		_asteroidProbeTimer =
			(ship.GetInstanceId() % 10) / 10.0f
			* EnemyUpdateConfig.AsteroidProbeInterval;
	}

	// Finds the ship's visual node so it can pitch and bank independently.
	public override void _Ready()
	{
		Visual = Ship.GetNodeOrNull<Node3D>("Visual");
	}

	#endregion

	#region Physics

	// Updates the ship's facing, movement, and visual tilt every physics frame.
	public override void _PhysicsProcess(double delta)
	{
		if (Ship == null || Ship.Definition?.Handling == null)
		{
			return;
		}

		float seconds = (float)delta;
		Vector3 desiredVelocity = GetDesiredVelocity(seconds);
		Vector3 facingDirection = GetFacingDirection();

		desiredVelocity = ApplyAsteroidResponse(desiredVelocity, seconds);

		TurnToward(facingDirection, seconds);
		Fly(desiredVelocity, seconds);
		TiltVisual(facingDirection, seconds);
	}

	#endregion

	#region Movement Behaviour

	// Returns the velocity requested by a specific enemy movement behaviour.
	// Derived controllers override this to determine where and how to fly.
	protected virtual Vector3 GetDesiredVelocity(float seconds)
	{
		return Vector3.Zero;
	}

	// Returns the direction the ship should face.
	// Derived controllers override this to aim toward a target or destination.
	protected virtual Vector3 GetFacingDirection()
	{
		return Vector3.Zero;
	}

	#endregion

	#region Rotation

	// Gradually rotates the ship toward the requested horizontal direction.
	// Turning speed is limited by the enemy's handling definition.
	private void TurnToward(Vector3 direction, float seconds)
	{
		Vector2 horizontal = new Vector2(direction.X, direction.Z);

		if (horizontal.LengthSquared() < 0.001f)
		{
			return;
		}

		// Ships face local -Z.
		float desiredYaw = Mathf.Atan2(-direction.X, -direction.Z);
		float currentYaw = Ship.Rotation.Y;
		float difference = Mathf.Wrap(
			desiredYaw - currentYaw,
			-Mathf.Pi,
			Mathf.Pi
		);

		float turnStep = Mathf.DegToRad(
			Ship.Definition.Handling.TurnSpeedDegrees
		) * seconds;

		Ship.Rotation = new Vector3(
			Ship.Rotation.X,
			currentYaw + Mathf.Clamp(difference, -turnStep, turnStep),
			Ship.Rotation.Z
		);
	}

	#endregion

	#region Flight

	// Accelerates the ship toward its desired velocity.
	// Uses braking when no movement is requested and resolves collisions.
	private void Fly(Vector3 desiredVelocity, float seconds)
	{
		EnemyHandlingStats handling = Ship.Definition.Handling;

		float response = desiredVelocity.LengthSquared() < 0.001f
			? handling.Braking
			: handling.Acceleration;

		Ship.Velocity = Ship.Velocity.MoveToward(
			desiredVelocity,
			response * seconds
		);

		Ship.MoveAndSlide();
	}

	#endregion

	#region Visual Tilt

	// Tilts the visual model vertically based on climbing or descending.
	// Banks the model during turns without rotating the ship's physics body.
	private void TiltVisual(Vector3 facingDirection, float seconds)
{
	if (Visual == null)
	{
		return;
	}

	EnemyHandlingStats handling = Ship.Definition.Handling;

	float horizontalDistance = new Vector2(
		facingDirection.X,
		facingDirection.Z
	).Length();

	float targetPitch = 0.0f;
	float targetBank = 0.0f;

	if (facingDirection.LengthSquared() > 0.001f)
	{
		// Positive pitch raises the nose of a ship facing local -Z.
		targetPitch = Mathf.Clamp(
			Mathf.Atan2(facingDirection.Y, horizontalDistance),
			-Mathf.DegToRad(handling.MaxPitchDegrees),
			Mathf.DegToRad(handling.MaxPitchDegrees)
		);

		if (horizontalDistance > 0.001f)
		{
			float desiredYaw = Mathf.Atan2(
				-facingDirection.X,
				-facingDirection.Z
			);

			float yawError = Mathf.Wrap(
				desiredYaw - Ship.Rotation.Y,
				-Mathf.Pi,
				Mathf.Pi
			);

			targetBank = Mathf.Clamp(
				-yawError,
				-Mathf.DegToRad(handling.MaxBankDegrees),
				Mathf.DegToRad(handling.MaxBankDegrees)
			);
		}
	}

	Vector3 rotation = Visual.Rotation;

	rotation.X = Mathf.MoveToward(
		rotation.X,
		targetPitch,
		Mathf.DegToRad(handling.PitchSpeedDegrees) * seconds
	);

	rotation.Z = Mathf.MoveToward(
		rotation.Z,
		targetBank,
		handling.BankResponse * seconds
	);

	Visual.Rotation = rotation;
}

	#endregion

	#region Asteroid Avoidance

	private Vector3 ApplyAsteroidResponse(Vector3 desiredVelocity, float seconds)
	{
		if (Ship.Definition.AsteroidResponse != AsteroidResponse.Avoid
			|| desiredVelocity.LengthSquared() < 0.001f)
		{
			_asteroidSteering = Vector3.Zero;
			return desiredVelocity;
		}

		_asteroidProbeTimer -= seconds;

		if (_asteroidProbeTimer <= 0.0f)
		{
			_asteroidProbeTimer = EnemyUpdateConfig.AsteroidProbeInterval;
			_asteroidSteering = FindAsteroidSteering(desiredVelocity);
		}

		if (_asteroidSteering == Vector3.Zero)
		{
			return desiredVelocity;
		}

		// Keep moving generally toward the destination while giving the
		// obstacle a strong steering influence.
		Vector3 direction = (
			desiredVelocity.Normalized() * 0.35f
			+ _asteroidSteering * 1.5f
		).Normalized();

		return direction * desiredVelocity.Length();
	}

	private Vector3 FindAsteroidSteering(Vector3 desiredVelocity)
	{
		Vector3 direction = desiredVelocity.Normalized();
		float lookAhead = Mathf.Max(
			EnemyUpdateConfig.AsteroidMinimumLookAhead,
			Ship.Velocity.Length() * EnemyUpdateConfig.AsteroidLookAheadSeconds
		);

		// Probe the centre and both sides of the wide hull.
		float halfWidth = Ship.Definition.CollisionSize.X * 0.4f;
		Node3D visual = Ship.GetNodeOrNull<Node3D>("Visual");

		if (visual != null)
		{
			halfWidth *= visual.Scale.X;
		}

		Vector3 side = Ship.GlobalBasis.X;
		Vector3[] offsets =
		{
			Vector3.Zero,
			side * halfWidth,
			-side * halfWidth
		};

		foreach (Vector3 offset in offsets)
		{
			Vector3 start = Ship.GlobalPosition + offset;
			Vector3 end = start + direction * lookAhead;

			PhysicsRayQueryParameters3D query =
				PhysicsRayQueryParameters3D.Create(start, end);

			query.Exclude = new Godot.Collections.Array<Rid>
			{
				Ship.GetRid()
			};

			var hit = Ship.GetWorld3D().DirectSpaceState.IntersectRay(query);

			if (hit.Count == 0
				|| hit["collider"].AsGodotObject() is not Asteroid asteroid)
			{
				continue;
			}

			// Find a direction away from the asteroid that does not simply
			// push backward along our current flight path.
			Vector3 away = Ship.GlobalPosition - asteroid.GlobalPosition;
			Vector3 lateral = away - direction * away.Dot(direction);

			if (lateral.LengthSquared() < 0.01f)
			{
				// A perfectly centred obstacle needs a consistent tie breaker.
				float sign = Ship.GetInstanceId() % 2 == 0 ? 1.0f : -1.0f;
				lateral = Ship.GlobalBasis.X * sign + Vector3.Up * 0.35f;
			}

			return lateral.Normalized();
		}

		return Vector3.Zero;
	}

	#endregion
}

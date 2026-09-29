
using Godot;

public partial class EnemyMovementController : Node
{
	#region References

	protected EnemyShip Ship;
	protected Node3D Visual;

	#endregion

	#region Setup

	// Stores a reference to the enemy ship controlled by this component.
	public void Initialize(EnemyShip ship)
	{
		Ship = ship;
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
}

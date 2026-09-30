using Godot;

public partial class EnemyHardpoint : Node3D
{
	#region Configuration

	[ExportGroup("Identity")]
	[Export] public string Key = "";
	[Export] public string Group = "cannons";

	[ExportGroup("Aiming")]
	[Export] public HardpointRotationStats RotationStats = new();

	#endregion

	#region References

	private EnemyShip _ship;
	private Node3D _mountParent;
	private Basis _restBasis;
	private Vector3 _restScale;

	public Marker3D Muzzle { get; private set; }

	public string MountKey =>
		string.IsNullOrWhiteSpace(Key) ? Name.ToString() : Key;

	public Vector3 MuzzlePosition => Muzzle.GlobalPosition;
	public Vector3 MuzzleDirection => -Muzzle.GlobalBasis.Z.Normalized();

	#endregion

	#region Runtime

	private float _yaw;
	private float _pitch;

	public bool TargetWithinLimits { get; private set; }

	#endregion

	#region Setup

	// Caches the authored mount orientation and existing muzzle marker.
	public override void _Ready()
	{
		_mountParent = GetParent() as Node3D;
		_restScale = Scale;
		_restBasis = Basis.Orthonormalized();
		Muzzle = GetNodeOrNull<Marker3D>("Muzzle");

		// Aim after the ship's movement has updated its visual orientation.
		ProcessPhysicsPriority = 10;

		if (_mountParent == null || Muzzle == null)
		{
			GD.PushError($"{Name} needs a Node3D parent and a Muzzle Marker3D.");
			SetPhysicsProcess(false);
		}
	}

	// Connects this scene-placed mount to its owning ship.
	public void Initialize(EnemyShip ship)
	{
		_ship = ship;
	}

	#endregion

	#region Aiming

	// Tracks the ship's selected target within the configured aiming limits.
	public override void _PhysicsProcess(double delta)
	{
		TargetWithinLimits = false;

		if (!GodotObject.IsInstanceValid(_ship)
			|| !_ship.IsCombatTargetable
			|| Muzzle == null)
		{
			return;
		}

		HardpointRotationStats rotation = RotationStats;

		if (rotation == null || rotation.Mode == HardpointRotationMode.Fixed)
		{
			_yaw = 0.0f;
			_pitch = 0.0f;
			ApplyAim();
			return;
		}

		Node3D target = _ship.Targeting?.Target;

		bool hasTarget =
			GodotObject.IsInstanceValid(target)
			&& !target.IsQueuedForDeletion();

		float desiredYaw = 0.0f;
		float desiredPitch = 0.0f;

		if (hasTarget)
		{
			// Measure the target direction relative to the resting mount.
			Vector3 direction = _restBasis.Inverse() * (
				_mountParent.ToLocal(target.GlobalPosition) - Position
			);

			if (direction.LengthSquared() < 0.0001f)
			{
				return;
			}

			float horizontalLength =
				new Vector2(direction.X, direction.Z).Length();

			desiredYaw = horizontalLength > 0.001f
				? Mathf.Atan2(-direction.X, -direction.Z)
				: _yaw;

			desiredPitch = Mathf.Atan2(direction.Y, horizontalLength);

			float yawLimit = Mathf.DegToRad(
				Mathf.Clamp(rotation.YawLimitDegrees, 0.0f, 180.0f)
			);

			float pitchUp = Mathf.DegToRad(
				Mathf.Clamp(rotation.PitchUpDegrees, 0.0f, 89.0f)
			);

			float pitchDown = Mathf.DegToRad(
				Mathf.Clamp(rotation.PitchDownDegrees, 0.0f, 89.0f)
			);

			TargetWithinLimits =
				Mathf.Abs(desiredYaw) <= yawLimit
				&& desiredPitch <= pitchUp
				&& desiredPitch >= -pitchDown;

			desiredYaw = Mathf.Clamp(desiredYaw, -yawLimit, yawLimit);
			desiredPitch = Mathf.Clamp(desiredPitch, -pitchDown, pitchUp);
		}
		else if (!rotation.ReturnToRest)
		{
			return;
		}

		float turnStep =
			Mathf.DegToRad(Mathf.Max(0.0f, rotation.TurnSpeedDegrees))
			* (float)delta;

		_yaw = Mathf.MoveToward(_yaw, desiredYaw, turnStep);
		_pitch = Mathf.MoveToward(_pitch, desiredPitch, turnStep);

		ApplyAim();
	}

	// Applies yaw and pitch while preserving the mount's authored scale.
	private void ApplyAim()
	{
		Basis aim = _restBasis * Basis.FromEuler(
			new Vector3(_pitch, _yaw, 0.0f)
		);

		Basis = new Basis(
			aim.X * _restScale.X,
			aim.Y * _restScale.Y,
			aim.Z * _restScale.Z
		);
	}

	#endregion
}

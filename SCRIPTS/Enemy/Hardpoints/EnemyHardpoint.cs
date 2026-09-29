using Godot;

public partial class EnemyHardpoint : Node3D
{
	#region References

	private EnemyShip _ship;
	private Node3D _mountParent;
	private Basis _restBasis;

	public HardpointDefinition Definition { get; private set; }
	public Marker3D Muzzle { get; private set; }

	#endregion

	#region Runtime

	private float _yaw;
	private float _pitch;

	public bool TargetWithinLimits { get; private set; }

	public Vector3 MuzzlePosition => Muzzle.GlobalPosition;
	public Vector3 MuzzleDirection => -Muzzle.GlobalBasis.Z.Normalized();

	#endregion

	#region Setup

	public void Initialize(EnemyShip ship, HardpointDefinition definition)
	{
		_ship = ship;
		Definition = definition;

		Name = definition.Key;
		Position = definition.Position;

		Vector3 restRadians = new(
			Mathf.DegToRad(definition.RestRotationDegrees.X),
			Mathf.DegToRad(definition.RestRotationDegrees.Y),
			Mathf.DegToRad(definition.RestRotationDegrees.Z)
		);

		_restBasis = Basis.FromEuler(restRadians);
		Basis = _restBasis;

		// Aim after the movement controller has updated the visual tilt.
		ProcessPhysicsPriority = 10;
	}

	public override void _Ready()
	{
		_mountParent = GetParent<Node3D>();

		Muzzle = new Marker3D
		{
			Name = "Muzzle",
			Position = Definition.MuzzleOffset
		};

		AddChild(Muzzle);

		if (Definition.VisualScene != null)
		{
			AddChild(Definition.VisualScene.Instantiate<Node3D>());
		}
		else if (Definition.ShowPlaceholderBarrel)
		{
			CreatePlaceholderBarrel();
		}
	}

	#endregion

	#region Aiming

	public override void _PhysicsProcess(double delta)
	{
		if (!_ship.IsCombatTargetable)
		{
			return;
		}

		HardpointRotationStats rotation = Definition.RotationStats;

		if (rotation == null
			|| rotation.Mode == HardpointRotationMode.Fixed)
		{
			_yaw = 0.0f;
			_pitch = 0.0f;
			Basis = _restBasis;
			TargetWithinLimits = false;
			return;
		}

		float desiredYaw = 0.0f;
		float desiredPitch = 0.0f;

		Node3D target = _ship.Targeting?.Target;

		bool hasTarget =
			GodotObject.IsInstanceValid(target)
			&& !target.IsQueuedForDeletion();

		TargetWithinLimits = false;

		if (hasTarget)
		{
			// Convert into the visual's space, then the mount's resting space.
			Vector3 localDirection = _restBasis.Inverse() * (
				_mountParent.ToLocal(target.GlobalPosition) - Position
			);

			if (localDirection.LengthSquared() < 0.001f)
			{
				return;
			}

			float horizontalLength = new Vector2(
				localDirection.X,
				localDirection.Z
			).Length();

			desiredYaw = horizontalLength > 0.001f
				? Mathf.Atan2(-localDirection.X, -localDirection.Z)
				: _yaw;

			desiredPitch = Mathf.Atan2(
				localDirection.Y,
				horizontalLength
			);

			float yawLimit = Mathf.DegToRad(rotation.YawLimitDegrees);
			float pitchUp = Mathf.DegToRad(rotation.PitchUpDegrees);
			float pitchDown = Mathf.DegToRad(rotation.PitchDownDegrees);

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

		Basis = _restBasis * Basis.FromEuler(
			new Vector3(_pitch, _yaw, 0.0f)
		);
	}

	#endregion

	#region Placeholder Visual

	private void CreatePlaceholderBarrel()
	{
		FactionPalette palette = FactionPalettes.Get(_ship.CombatFaction);

		StandardMaterial3D barrelMaterial = new()
		{
			AlbedoColor = palette.Metal,
			Metallic = 0.45f,
			Roughness = 0.38f
		};

		MeshInstance3D barrel = new()
		{
			Name = "Barrel",
			Position = Definition.BarrelOffset,
			Mesh = new BoxMesh { Size = Definition.BarrelSize },
			MaterialOverride = barrelMaterial
		};

		AddChild(barrel);

		StandardMaterial3D muzzleMaterial = new()
		{
			AlbedoColor = palette.Energy,
			EmissionEnabled = true,
			Emission = palette.Energy,
			EmissionEnergyMultiplier = 3.0f
		};

		MeshInstance3D aperture = new()
		{
			Name = "MuzzleAperture",
			Position = Definition.MuzzleOffset,
			Mesh = new BoxMesh
			{
				Size = new Vector3(
					Definition.BarrelSize.X * 0.7f,
					Definition.BarrelSize.Y * 0.7f,
					0.04f
				)
			},
			MaterialOverride = muzzleMaterial
		};

		AddChild(aperture);
	}

	#endregion
}

using Godot;

// Adds cosmetic ship pitch and banking, plus smooth movement-based camera offsets.
public partial class PlayerFlightVisuals : Node
{
#region Ship Settings

[Export] public float PitchTiltDegrees = 6.0f;
[Export] public float TurnBankDegrees = 8.0f;
[Export] public float StrafeBankDegrees = 10.0f;
[Export] public float RollTiltDegrees = 14.0f;
[Export] public float ShipResponse = 8.0f;

#endregion

	#region Camera Settings

	[Export] public float MovingCameraBack = 0.6f;
	[Export] public float BoostCameraBack = 2.1f;
	[Export] public float CameraResponse = 5.0f;

	[Export] public float CameraSideSway = 0.12f;
	[Export] public float CameraPitchSway = 0.08f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private Node3D _visualPivot;
	private Camera3D _camera;

	private Vector3 _cameraRestPosition;
	private Vector3 _visualAngles;

	#endregion

	#region Setup


	// =========================================================
// Groups the ship visuals and both weapon mounts, then caches the camera.
public override void _Ready()
{
	_ship = GetParent() as PlayerShip;

	if (_ship == null)
	{
		GD.PushError("PlayerFlightVisuals must be a child of PlayerShip.");
		SetProcess(false);
		return;
	}

	_visualPivot = new Node3D
	{
		Name = "FlightVisualPivot"
	};

	_ship.AddChild(_visualPivot);

	MoveUnderPivot("Cyan_Interceptor_Mk2");
	MoveUnderPivot("PrimaryWeapon");
	MoveUnderPivot("SecondaryWeapon");
	MoveUnderPivot("PlayerThrusters");
	MoveUnderPivot("WingTrails");

	_camera = _ship.GetNodeOrNull<Camera3D>("Camera3D");

	if (_camera != null)
	{
		_cameraRestPosition = _camera.Position;
	}
}

	// Moves a visual component while preserving its existing placement.
	private void MoveUnderPivot(string nodeName)
	{
		Node3D component = _ship.GetNodeOrNull<Node3D>(nodeName);

		if (component != null)
		{
			component.Reparent(_visualPivot, true);
		}
	}

	#endregion

	#region Presentation

	// Smooths cosmetic ship motion and the three camera-distance states.
	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_ship))
		{
			return;
		}

		float seconds = (float)delta;

		UpdateShipVisuals(seconds);
		UpdateCamera(seconds);
	}

	// Adds smooth visual pitch and banking for turns, strafing, and manual rolls.
private void UpdateShipVisuals(float seconds)
{
	float bankDegrees =
		_ship.YawInput * TurnBankDegrees
		- _ship.StrafeInput * StrafeBankDegrees
		- _ship.RollInput * RollTiltDegrees;

	Vector3 desiredAngles = new(
		Mathf.DegToRad(_ship.PitchInput * PitchTiltDegrees),
		0.0f,
		Mathf.DegToRad(bankDegrees)
	);

	float blend = 1.0f - Mathf.Exp(
		-Mathf.Max(0.0f, ShipResponse) * seconds
	);

	_visualAngles = _visualAngles.Lerp(desiredAngles, blend);
	_visualPivot.Rotation = _visualAngles;
}

	// Pulls back with actual movement, then adds the boost distance and mild sway.
	private void UpdateCamera(float seconds)
	{
		if (!GodotObject.IsInstanceValid(_camera))
		{
			return;
		}

		float movement = Mathf.Clamp(
			_ship.Velocity.Length() / Mathf.Max(0.01f, _ship.ForwardSpeed),
			0.0f,
			1.0f
		);

		float normalBack = movement * MovingCameraBack;

		float back = Mathf.Lerp(
			normalBack,
			BoostCameraBack,
			_ship.BoostAmount
		);

		Vector3 desiredPosition = _cameraRestPosition + new Vector3(
			-_ship.YawInput * CameraSideSway,
			-_ship.PitchInput * CameraPitchSway,
			back
		);

		float blend = 1.0f - Mathf.Exp(
			-Mathf.Max(0.0f, CameraResponse) * seconds
		);

		_camera.Position = _camera.Position.Lerp(
			desiredPosition,
			blend
		);
	}

	#endregion
}

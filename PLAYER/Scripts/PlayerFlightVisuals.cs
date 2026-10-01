using Godot;

// Handles cosmetic ship movement and toggles one camera between chase and cockpit views.
public partial class PlayerFlightVisuals : Node
{
	#region Ship Settings

	[ExportGroup("Ship Visuals")]

	[Export] public float PitchTiltDegrees = 6.0f;
	[Export] public float TurnBankDegrees = 8.0f;
	[Export] public float StrafeBankDegrees = 10.0f;
	[Export] public float RollTiltDegrees = 14.0f;
	[Export] public float ShipResponse = 8.0f;

	#endregion

	#region Third Person Settings

	[ExportGroup("Third Person Camera")]

	[Export] public float MovingCameraBack = 0.6f;
	[Export] public float BoostCameraBack = 2.1f;
	[Export] public float CameraResponse = 5.0f;

	[Export] public float CameraSideSway = 0.12f;
	[Export] public float CameraPitchSway = 0.08f;

	[ExportSubgroup("Movement Follow")]

/// <summary>
/// Maximum sideways camera displacement caused by player strafing.
/// </summary>
[Export] public float CameraStrafeOffset = 0.8f;

/// <summary>
/// Maximum vertical camera displacement caused by ascending or descending.
/// </summary>
[Export] public float CameraVerticalOffset = 0.45f;

/// <summary>
/// How quickly the camera catches up to movement-based offsets.
/// Higher values make the camera follow more tightly.
/// </summary>
[Export] public float MovementFollowResponse = 4.0f;

	#endregion

	#region Cockpit Settings

	[ExportGroup("Cockpit Camera")]

	[Export] public Key ToggleCameraKey = Key.V;
	[Export] public bool StartInCockpit = false;

	[Export] public float CockpitFov = 90.0f;
	[Export] public float CockpitNear = 0.03f;
	[Export] public float CockpitSway = 0.015f;
	[Export] public float CockpitTiltDegrees = 0.5f;
	[Export] public float CockpitBoostFov = 3.0f;

	[ExportGroup("Cockpit Placeholder")]

	[Export] public bool BuildCockpitPlaceholder = true;

	#endregion

	#region References And Runtime

	private PlayerShip _ship;
	private Node3D _visualPivot;
	private Camera3D _camera;
	private Marker3D _cockpitView;
	private Node3D _cockpitInterior;

	private Transform3D _thirdPersonRest;
	private float _thirdPersonFov;
	private float _thirdPersonNear;

	private Vector3 _visualAngles;

	public bool IsFirstPerson { get; private set; }

	#endregion

	#region Setup

		// =========================================================
	// Waits until the scene finishes entering the tree before regrouping visuals.
	public override void _Ready()
	{
		SetProcess(false);
		SetProcessInput(false);

		CallDeferred(nameof(InitializeVisuals));
	}

	// =========================================================
	// Creates the cosmetic pivot after the parent finishes setting up its children.
	private void InitializeVisuals()
	{
		if (IsQueuedForDeletion())
		{
			return;
		}

		_ship = GetParent() as PlayerShip;

		if (_ship == null)
		{
			GD.PushError(
				"PlayerFlightVisuals must be a child of PlayerShip."
			);

			return;
		}

		_camera = _ship.GetNodeOrNull<Camera3D>("Camera3D");
		_cockpitView = _ship.GetNodeOrNull<Marker3D>("CockpitView");

		if (_camera == null)
		{
			GD.PushError(
				"PlayerShip needs a Camera3D named Camera3D."
			);

			return;
		}

		_visualPivot = _ship.GetNodeOrNull<Node3D>(
			"FlightVisualPivot"
		);

		if (_visualPivot == null)
		{
			_visualPivot = new Node3D
			{
				Name = "FlightVisualPivot"
			};

			_ship.AddChild(_visualPivot);
		}

		MoveUnderPivot("Cyan_Interceptor_Mk2");
		MoveUnderPivot("PrimaryWeapon");
		MoveUnderPivot("SecondaryWeapon");
		MoveUnderPivot("PlayerThrusters");
		MoveUnderPivot("WingTrails");

		_thirdPersonRest = _camera.Transform;
		_thirdPersonFov = _camera.Fov;
		_thirdPersonNear = _camera.Near;

		if (_cockpitView != null && BuildCockpitPlaceholder)
		{
			CreateCockpitPlaceholder();
		}

		SetCameraMode(StartInCockpit);

		SetProcess(true);
		SetProcessInput(true);
	}

	// =========================================================
	// Moves a visual component while preserving its authored placement.
	private void MoveUnderPivot(string nodeName)
	{
		Node3D component = _ship.GetNodeOrNull<Node3D>(nodeName);

		if (component != null)
		{
			component.Reparent(_visualPivot, true);
		}
	}

	#endregion

	#region Camera Input

	// =========================================================
// Toggles camera view and returns cockpit interaction to flight when leaving it.
public override void _Input(InputEvent inputEvent)
{
	if (inputEvent is not InputEventKey key
		|| !key.Pressed
		|| key.Echo
		|| key.PhysicalKeycode != ToggleCameraKey)
	{
		return;
	}

	if (!GodotObject.IsInstanceValid(_ship)
		|| !_ship.IsCombatTargetable)
	{
		return;
	}

	if (Input.MouseMode != Input.MouseModeEnum.Captured
		&& !_ship.CockpitInteractionActive)
	{
		return;
	}

	if (_ship.CockpitInteractionActive)
	{
		_ship.SetCockpitInteraction(false);
	}

	SetCameraMode(!IsFirstPerson);
	GetViewport().SetInputAsHandled();
}

	// =========================================================
	// Switches instantly between the authored chase view and cockpit view.
	public void SetCameraMode(bool firstPerson)
	{
		if (!GodotObject.IsInstanceValid(_camera))
		{
			return;
		}

		if (firstPerson && !GodotObject.IsInstanceValid(_cockpitView))
		{
			GD.PushWarning(
				"Add a CockpitView Marker3D under PlayerShip."
			);

			return;
		}

		IsFirstPerson = firstPerson;

		if (_cockpitInterior != null)
		{
			_cockpitInterior.Visible = firstPerson;
		}

		_camera.Near = firstPerson
			? Mathf.Max(0.01f, CockpitNear)
			: _thirdPersonNear;

		UpdateCamera(0.0f, true);
	}

	#endregion

	#region Presentation

	// =========================================================
	// Updates cosmetic ship movement and the selected camera view.
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

	// =========================================================
	// Smooths cosmetic pitch, turning bank, strafe bank, and manual roll tilt.
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

	// =========================================================
// Follows cockpit motion directly or lets the ship shift within
// third-person framing using bounded movement offsets.
// =========================================================
private void UpdateCamera(float seconds, bool instant = false)
{
	if (!GodotObject.IsInstanceValid(_camera))
	{
		return;
	}

	float blend = instant
		? 1.0f
		: 1.0f - Mathf.Exp(
			-Mathf.Max(0.0f, CameraResponse) * seconds
		);

	Vector3 desiredPosition;
	Basis desiredBasis;
	float desiredFov;

	bool cockpitActive =
		IsFirstPerson
		&& GodotObject.IsInstanceValid(_cockpitView)
		&& GodotObject.IsInstanceValid(_visualPivot);

	if (cockpitActive)
	{
		Transform3D eyeRest =
			_ship.GlobalTransform.AffineInverse()
			* _cockpitView.GlobalTransform;

		Transform3D cockpitTransform =
			_visualPivot.Transform * eyeRest;

		Basis cockpitBasis =
			cockpitTransform.Basis.Orthonormalized();

		Vector3 sway = new(
			-_ship.YawInput * CockpitSway,
			-_ship.PitchInput * CockpitSway,
			0.0f
		);

		desiredPosition =
			cockpitTransform.Origin + cockpitBasis * sway;

		Vector3 tilt = new(
			Mathf.DegToRad(
				-_ship.PitchInput * CockpitTiltDegrees
			),
			0.0f,
			Mathf.DegToRad(
				-_ship.YawInput * CockpitTiltDegrees
			)
		);

		desiredBasis =
			cockpitBasis * Basis.FromEuler(tilt);

		desiredFov = Mathf.Clamp(
			CockpitFov
				+ _ship.BoostAmount * CockpitBoostFov,
			20.0f,
			120.0f
		);
	}
	else
	{
		// Convert world velocity into the ship's local axes.
        Vector3 localVelocity =
            _ship.GlobalBasis.Orthonormalized().Inverse()
            * _ship.Velocity;

        float sideways = Mathf.Clamp(
            localVelocity.X
                / Mathf.Max(0.01f, _ship.StrafeSpeed),
            -1.0f,
            1.0f
        );

        float vertical = Mathf.Clamp(
            localVelocity.Y
                / Mathf.Max(0.01f, _ship.VerticalSpeed),
            -1.0f,
            1.0f
        );

        float movement = Mathf.Clamp(
            _ship.Velocity.Length()
                / Mathf.Max(0.01f, _ship.ForwardSpeed),
            0.0f,
            1.0f
        );

        float normalBack = movement * MovingCameraBack;

        float back = Mathf.Lerp(
            normalBack,
            BoostCameraBack,
            _ship.BoostAmount
        );

        // Move the camera opposite lateral travel so the ship
        // shifts towards its direction of movement on screen.
        desiredPosition =
            _thirdPersonRest.Origin + new Vector3(
                -sideways * Mathf.Max(0.0f, CameraStrafeOffset)
                    - _ship.YawInput * CameraSideSway,
                -vertical * Mathf.Max(0.0f, CameraVerticalOffset)
                    - _ship.PitchInput * CameraPitchSway,
                back
            );

        desiredBasis =
            _thirdPersonRest.Basis.Orthonormalized();

        desiredFov = _thirdPersonFov;
    }

    float positionBlend = cockpitActive || instant
        ? 1.0f
        : 1.0f - Mathf.Exp(
            -Mathf.Max(0.0f, MovementFollowResponse) * seconds
        );

    Vector3 position = _camera.Position.Lerp(
        desiredPosition,
        positionBlend
    );

    // Keep pullback controlled by the existing camera response.
    if (!cockpitActive)
    {
        position.Z = Mathf.Lerp(
            _camera.Position.Z,
            desiredPosition.Z,
            blend
        );
    }

    float rotationBlend = cockpitActive ? 1.0f : blend;

    Basis rotation = _camera.Basis.Orthonormalized().Slerp(
        desiredBasis,
        rotationBlend
    );

    _camera.Transform = new Transform3D(rotation, position);

    _camera.Fov = Mathf.Lerp(
        _camera.Fov,
        desiredFov,
        blend
    );
}

	#endregion

	#region Cockpit Placeholder

		// =========================================================
	// Builds the cockpit at the eye marker, then attaches it to the ship's visual pivot.
	private void CreateCockpitPlaceholder()
	{
		_cockpitInterior = new Node3D
		{
			Name = "CockpitInterior",
			Visible = false
		};

		_cockpitView.AddChild(_cockpitInterior);

		StandardMaterial3D metal = new()
		{
			AlbedoColor = new Color(0.025f, 0.04f, 0.055f),
			Metallic = 0.45f,
			Roughness = 0.65f
		};

		StandardMaterial3D display = new()
		{
			AlbedoColor = new Color(0.1f, 0.65f, 0.8f),
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			EmissionEnabled = true,
			Emission = new Color(0.1f, 0.65f, 0.8f),
			EmissionEnergyMultiplier = 1.0f
		};

		AddCockpitBox(
			"Dashboard",
			new Vector3(1.05f, 0.16f, 0.28f),
			new Vector3(0.0f, -0.38f, -0.65f),
			metal
		);

		AddCockpitBox(
			"Display",
			new Vector3(0.3f, 0.065f, 0.012f),
			new Vector3(0.0f, -0.33f, -0.50f),
			display
		);

		AddCockpitBox(
			"LeftFrame",
			new Vector3(0.025f, 0.6f, 0.035f),
			new Vector3(-0.48f, -0.04f, -0.55f),
			metal
		);

		AddCockpitBox(
			"RightFrame",
			new Vector3(0.025f, 0.6f, 0.035f),
			new Vector3(0.48f, -0.04f, -0.55f),
			metal
		);

		AddCockpitBox(
			"TopFrame",
			new Vector3(0.985f, 0.025f, 0.035f),
			new Vector3(0.0f, 0.26f, -0.55f),
			metal
		);

		// Follow the same cosmetic pitch and bank as the hull and guns.
		_cockpitInterior.Reparent(_visualPivot, true);
	}

	// =========================================================
	// Adds one visual-only cockpit part without introducing collision.
	private void AddCockpitBox(
		string name,
		Vector3 size,
		Vector3 position,
		Material material
	)
	{
		_cockpitInterior.AddChild(new MeshInstance3D
		{
			Name = name,
			Position = position,
			Mesh = new BoxMesh
			{
				Size = size
			},
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		});
	}

	#endregion
}

using System;
using System.Collections.Generic;
using Godot;

// Loads connected sectors while preserving the Level's existing player instance.
public partial class SectorManager : Node
{
	#region Configuration

	[ExportGroup("Level References")]

	[Export] public PlayerShip Player;
	[Export] public Node3D SectorContainer;

	[ExportGroup("Sector Registry")]

	[Export] public Godot.Collections.Array<SectorDefinition>
		Sectors = new();

	[Export] public string StartingSectorKey = "hq";

	[ExportGroup("Travel Presentation")]

	[Export] public float FadeSeconds = 0.45f;

	[ExportGroup("Temporary Testing")]

	[Export] public bool EnableDebugTravel = true;
	[Export] public Key DebugTravelKey = Key.J;

	#endregion

	#region Public State

	public WorldSector CurrentSector { get; private set; }
	public SectorDefinition CurrentDefinition { get; private set; }

	public string PreviousSectorKey { get; private set; } = "";

	public bool IsTravelling => _phase != TravelPhase.Idle;

	public event Action<SectorDefinition> SectorChanged;

	#endregion

	#region Runtime

	private enum TravelPhase
	{
		Idle,
		FadingOut,
		Loading,
		Settling,
		FadingIn
	}

	private readonly Dictionary<string, SectorDefinition>
		_definitions = new();

	private TravelPhase _phase;

	private SectorDefinition _destination;
	private string _requestedArrivalKey;

	private ColorRect _cover;
	private Label _travelLabel;
	private float _opacity = 1.0f;
	private int _settleFrames;

	private Node.ProcessModeEnum _playerProcessMode;
	private Node.ProcessModeEnum _sectorProcessMode;

	private Input.MouseModeEnum _previousMouseMode;
	private bool _previousCockpitInteraction;
	private Vector3 _previousVelocity;

	private bool _hasFocus = true;
	private bool _travelSucceeded;

	#endregion

	#region Setup

	// =========================================================
	// Validates Level references, registers sector definitions, and starts loading.
	public override void _Ready()
	{
		if (Player == null || SectorContainer == null)
		{
			GD.PushError(
				"SectorManager needs Player and SectorContainer assigned."
			);

			SetProcess(false);
			SetProcessInput(false);
			return;
		}

		AddToGroup("sector_manager");
		BuildTravelOverlay();

		foreach (SectorDefinition definition in Sectors)
		{
			if (definition == null
				|| string.IsNullOrWhiteSpace(definition.Key)
				|| string.IsNullOrWhiteSpace(definition.ScenePath))
			{
				GD.PushError(
					"Every sector definition needs a Key and ScenePath."
				);

				continue;
			}

			if (!_definitions.TryAdd(definition.Key, definition))
			{
				GD.PushError(
					$"Duplicate sector key: {definition.Key}"
				);
			}
		}

		foreach (SectorDefinition definition in _definitions.Values)
		{
			foreach (string connection in definition.Connections)
			{
				if (!_definitions.ContainsKey(connection))
				{
					GD.PushError(
						$"Sector '{definition.Key}' connects to "
						+ $"unregistered sector '{connection}'."
					);
				}
			}
		}

		CallDeferred(nameof(LoadStartingSector));
	}

	// =========================================================
	// Starts the first sector after Level finishes entering the scene tree.
	private void LoadStartingSector()
	{
		if (!_definitions.TryGetValue(
			StartingSectorKey,
			out SectorDefinition definition
		))
		{
			StartupFailed(
				$"Starting sector '{StartingSectorKey}' is not registered."
			);

			return;
		}

		if (!BeginTravel(definition, ""))
		{
			StartupFailed("Could not start loading the starting sector.");
		}
	}

	// =========================================================
	// Creates presentation that remains alive while sectors are replaced.
	private void BuildTravelOverlay()
	{
		CanvasLayer overlay = new()
		{
			Name = "SectorTravelOverlay",
			Layer = 1000
		};

		AddChild(overlay);

		_cover = new ColorRect
		{
			Color = Colors.Black,
			MouseFilter = Control.MouseFilterEnum.Stop
		};

		overlay.AddChild(_cover);

		_cover.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_travelLabel = new Label
		{
			Text = "INITIALIZING SECTOR",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		_travelLabel.AddThemeColorOverride(
			"font_color",
			new Color(0.2f, 0.85f, 1.0f)
		);

		_travelLabel.AddThemeFontSizeOverride("font_size", 24);

		_cover.AddChild(_travelLabel);

		_travelLabel.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);
	}

	// =========================================================
	// Keeps an invalid initial setup covered and prevents flight without a sector.
	private void StartupFailed(string message)
	{
		GD.PushError(message);

		Player.ProcessMode = Node.ProcessModeEnum.Disabled;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_travelLabel.Text = "SECTOR SETUP ERROR — CHECK OUTPUT";
		SetCoverOpacity(1.0f);
	}

	#endregion

	#region Travel Requests

	// =========================================================
	// Checks whether a destination is connected to the currently loaded sector.
	public bool CanTravelTo(string destinationKey)
	{
		return !IsTravelling
			&& Player.IsCombatTargetable
			&& CurrentDefinition != null
			&& destinationKey != CurrentDefinition.Key
			&& CurrentDefinition.Connections.Contains(destinationKey)
			&& _definitions.ContainsKey(destinationKey);
	}

	// =========================================================
	// Requests travel through a valid connection with an optional arrival override.
	public bool TravelTo(
		string destinationKey,
		string arrivalKey = ""
	)
	{
		if (!CanTravelTo(destinationKey))
		{
			GD.PushWarning(
				$"Cannot currently travel to '{destinationKey}'."
			);

			return false;
		}

		return BeginTravel(
			_definitions[destinationKey],
			arrivalKey
		);
	}

	// =========================================================
	// Starts background loading and temporarily suspends flight and sector updates.
	private bool BeginTravel(
		SectorDefinition destination,
		string arrivalKey
	)
	{
		Error error = ResourceLoader.LoadThreadedRequest(
			destination.ScenePath,
			"PackedScene"
		);

		if (error != Error.Ok)
		{
			GD.PushError(
				$"Cannot load '{destination.ScenePath}': {error}"
			);

			return false;
		}

		_destination = destination;
		_requestedArrivalKey = arrivalKey;
		_travelSucceeded = false;

		_playerProcessMode = Player.ProcessMode;
		_sectorProcessMode = SectorContainer.ProcessMode;

		_previousMouseMode = Input.MouseMode;
		_previousCockpitInteraction = Player.CockpitInteractionActive;
		_previousVelocity = Player.Velocity;

		Player.SetCockpitInteraction(false);

		Player.GetNodeOrNull<PlayerDash>("Dash")?.EndDash();

		PlayerWeaponInput weapons =
			Player.GetNodeOrNull<PlayerWeaponInput>(
				"Weapons/PlayerWeaponMount"
			);

		weapons?.PrimaryWeapon?.SetTriggerHeld(false);
		weapons?.SecondaryWeapon?.SetTriggerHeld(false);

		Player.Velocity = Vector3.Zero;
		Player.ProcessMode = Node.ProcessModeEnum.Disabled;
		SectorContainer.ProcessMode = Node.ProcessModeEnum.Disabled;

		Input.MouseMode = Input.MouseModeEnum.Visible;

		_cover.Visible = true;
		_travelLabel.Text = $"TRAVELLING TO {destination.DisplayName}";

		_phase = _opacity >= 1.0f
			? TravelPhase.Loading
			: TravelPhase.FadingOut;

		return true;
	}

	#endregion

	#region Transition Updates

	// =========================================================
	// Advances fades and loading without a blocking polling loop.
	public override void _Process(double delta)
	{
		float step = (float)delta
			/ Mathf.Max(0.01f, FadeSeconds);

		switch (_phase)
		{
			case TravelPhase.FadingOut:
				SetCoverOpacity(
					Mathf.MoveToward(_opacity, 1.0f, step)
				);

				if (_opacity >= 1.0f)
				{
					_phase = TravelPhase.Loading;
				}

				break;

			case TravelPhase.Loading:
				UpdateLoading();
				break;

			case TravelPhase.Settling:
				// Allow deferred sector construction to run behind the cover.
				_settleFrames--;

				if (_settleFrames <= 0)
				{
					_phase = TravelPhase.FadingIn;
				}

				break;

			case TravelPhase.FadingIn:
				SetCoverOpacity(
					Mathf.MoveToward(_opacity, 0.0f, step)
				);

				if (_opacity <= 0.0f)
				{
					FinishTravel();
				}

				break;
		}
	}

	// =========================================================
	// Retrieves the destination only after threaded loading has completed.
	private void UpdateLoading()
	{
		ResourceLoader.ThreadLoadStatus status =
			ResourceLoader.LoadThreadedGetStatus(
				_destination.ScenePath
			);

		if (status == ResourceLoader.ThreadLoadStatus.InProgress)
		{
			return;
		}

		if (status != ResourceLoader.ThreadLoadStatus.Loaded)
		{
			TravelFailed(
				$"Loading failed for '{_destination.ScenePath}'."
			);

			return;
		}

		PackedScene scene = ResourceLoader.LoadThreadedGet(
			_destination.ScenePath
		) as PackedScene;

		if (scene == null)
		{
			TravelFailed("Destination resource is not a PackedScene.");
			return;
		}

		InstallSector(scene);
	}

	// =========================================================
	// Validates the destination and arrival before removing the current sector.
	private void InstallSector(PackedScene scene)
	{
		Node instance = scene.Instantiate();

		if (instance is not WorldSector next)
		{
			instance.Free();

			TravelFailed(
				"Destination root needs the WorldSector.cs script."
			);

			return;
		}

		Marker3D arrival = null;

		if (!string.IsNullOrWhiteSpace(_requestedArrivalKey))
		{
			arrival = next.FindArrival(_requestedArrivalKey);
		}
		else
		{
			if (CurrentDefinition != null)
			{
				arrival = next.FindArrival(
					"from_" + CurrentDefinition.Key
				);
			}

			arrival ??= next.FindArrival(
				_destination.DefaultArrivalKey
			);
		}

		if (arrival == null)
		{
			next.Free();

			TravelFailed(
				$"Sector '{_destination.Key}' has no valid arrival marker."
			);

			return;
		}

		WorldSector old = CurrentSector;

		if (GodotObject.IsInstanceValid(old))
		{
			SectorContainer.RemoveChild(old);
			old.QueueFree();
		}

		PreviousSectorKey = CurrentDefinition?.Key ?? "";

		CurrentSector = next;
		CurrentDefinition = _destination;

		SectorContainer.AddChild(next);

		Player.GlobalTransform = new Transform3D(
			arrival.GlobalBasis.Orthonormalized(),
			arrival.GlobalPosition
		);

		Player.Velocity = Vector3.Zero;
		Player.ResetPhysicsInterpolation();

		_travelSucceeded = true;
		_settleFrames = 2;
		_phase = TravelPhase.Settling;
	}

	// =========================================================
	// Restores the existing player and resumes the loaded sector.
	private void FinishTravel()
	{
		Player.ProcessMode = _playerProcessMode;
		SectorContainer.ProcessMode = _sectorProcessMode;

		Player.Velocity = _travelSucceeded
			? Vector3.Zero
			: _previousVelocity;

		Player.SetCockpitInteraction(
			_previousCockpitInteraction
		);

		Input.MouseMode = _hasFocus && Player.IsCombatTargetable
			? _previousMouseMode
			: Input.MouseModeEnum.Visible;

		_cover.Visible = false;
		_phase = TravelPhase.Idle;

		if (_travelSucceeded)
		{
			GD.Print(
				$"Entered sector: {CurrentDefinition.DisplayName} "
				+ $"({CurrentDefinition.Key})"
			);

			SectorChanged?.Invoke(CurrentDefinition);
		}
	}

	// =========================================================
	// Returns to the existing sector when loading or validation fails.
	private void TravelFailed(string message)
	{
		GD.PushError(message);

		if (CurrentSector == null)
		{
			_phase = TravelPhase.Idle;
			StartupFailed(message);
			return;
		}

		_travelLabel.Text = "TRAVEL FAILED — RETURNING";
		_phase = TravelPhase.FadingIn;
	}

	// =========================================================
	// Applies the fade without rebuilding the overlay.
	private void SetCoverOpacity(float opacity)
	{
		_opacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
		_cover.Color = new Color(0.0f, 0.0f, 0.0f, _opacity);
	}

	#endregion

	#region Input

	// =========================================================
	// Uses a temporary key to test travel through the first available connection.
	public override void _Input(InputEvent inputEvent)
	{
		if (!EnableDebugTravel
			|| !_hasFocus
			|| IsTravelling
			|| CurrentDefinition == null
			|| inputEvent is not InputEventKey key
			|| !key.Pressed
			|| key.Echo
			|| key.PhysicalKeycode != DebugTravelKey)
		{
			return;
		}

		foreach (string connection in CurrentDefinition.Connections)
		{
			if (CanTravelTo(connection))
			{
				TravelTo(connection);
				GetViewport().SetInputAsHandled();
				return;
			}
		}
	}

	// =========================================================
	// Prevents travel completion from capturing the pointer while unfocused.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			_hasFocus = false;
		}
		else if (what == NotificationApplicationFocusIn)
		{
			_hasFocus = true;
		}
	}

	#endregion
}
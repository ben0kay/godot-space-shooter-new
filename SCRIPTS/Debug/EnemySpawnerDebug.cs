using Godot;

// Builds a paused F1 enemy-spawner menu for Level or standalone test scenes.
public partial class EnemySpawnerDebug : CanvasLayer
{
	#region Configuration

	[Export] public Key ToggleKey = Key.F1;

	// Add future enemy scenes here through the Inspector.
	[Export] public EnemyCatalog Catalog;

	#endregion

	#region Runtime

	private Control _overlay;
	private VBoxContainer _enemyButtons;
	private EnemyRegistration _selectedEnemy;
	private Button _firstEnemyButton;
	private readonly ButtonGroup _enemyButtonGroup = new();
	private SpinBox _count;
	private SpinBox _distance;
	private SpinBox _spacing;
	private Label _status;

	private bool _open;
	private Input.MouseModeEnum _previousMouseMode;

	#endregion

	#region Setup

	// =========================================================
	// Builds the interface once and keeps this menu usable while paused.
	// =========================================================
	public override void _Ready()
	{
		ProcessMode = Node.ProcessModeEnum.Always;
		Layer = 100;
		SetProcess(false);
		SetPhysicsProcess(false);

		BuildMenu();
		PopulateEnemies();
	}

	// =========================================================
// Builds the paused debug panel with a scrollable enemy-button list.
// =========================================================
private void BuildMenu()
{
	_overlay = new Control
	{
		Visible = false,
		MouseFilter = Control.MouseFilterEnum.Stop
	};

	AddChild(_overlay);
	_overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

	ColorRect backdrop = new()
	{
		Color = new Color(0.005f, 0.01f, 0.025f, 0.8f),
		MouseFilter = Control.MouseFilterEnum.Stop
	};

	_overlay.AddChild(backdrop);
	backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

	CenterContainer centre = new()
	{
		MouseFilter = Control.MouseFilterEnum.Ignore
	};

	_overlay.AddChild(centre);
	centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

	PanelContainer panel = new()
	{
		CustomMinimumSize = new Vector2(480, 0)
	};

	StyleBoxFlat style = new()
	{
		BgColor = new Color(0.025f, 0.045f, 0.065f, 0.96f),
		BorderColor = new Color(0.2f, 0.8f, 0.95f, 0.8f),
		BorderWidthLeft = 1,
		BorderWidthRight = 1,
		BorderWidthTop = 1,
		BorderWidthBottom = 1,
		ContentMarginLeft = 24,
		ContentMarginRight = 24,
		ContentMarginTop = 24,
		ContentMarginBottom = 24
	};

	panel.AddThemeStyleboxOverride("panel", style);
	centre.AddChild(panel);

	VBoxContainer column = new();
	column.AddThemeConstantOverride("separation", 12);
	panel.AddChild(column);

	Label title = new() { Text = "DEBUG / ENEMY SPAWNER" };
	title.AddThemeFontSizeOverride("font_size", 24);
	title.AddThemeColorOverride(
		"font_color", new Color(0.3f, 0.9f, 1.0f)
	);
	column.AddChild(title);

	column.AddChild(new Label { Text = "GAME PAUSED — F1 TO RESUME" });
	column.AddChild(new HSeparator());
	column.AddChild(new Label { Text = "Registered enemies" });

	ScrollContainer scroll = new()
	{
		CustomMinimumSize = new Vector2(420, 160),
		HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
	};
	column.AddChild(scroll);

	_enemyButtons = new VBoxContainer
	{
		SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
	};
	_enemyButtons.AddThemeConstantOverride("separation", 6);
	scroll.AddChild(_enemyButtons);

	_count = AddNumber(column, "Amount", 1, 20, 1, 1);
	_distance = AddNumber(column, "Distance ahead", 30, 1000, 10, 180);
	_spacing = AddNumber(column, "Spacing between ships", 30, 200, 5, 50);

	Button spawn = new() { Text = "SPAWN SELECTED ENEMY" };
	spawn.Pressed += SpawnEnemies;
	column.AddChild(spawn);

	_status = new Label
	{
		Text = "Select an enemy above.",
		AutowrapMode = TextServer.AutowrapMode.WordSmart,
		CustomMinimumSize = new Vector2(420, 48)
	};
	column.AddChild(_status);

	Button resume = new() { Text = "RESUME" };
	resume.Pressed += CloseMenu;
	column.AddChild(resume);
}

	// =========================================================
	// Adds one labelled numeric setting and returns its input control.
	// =========================================================
	private SpinBox AddNumber(
		VBoxContainer parent,
		string label,
		double minimum,
		double maximum,
		double step,
		double value
	)
	{
		HBoxContainer row = new();
		parent.AddChild(row);

		row.AddChild(new Label
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		});

		SpinBox input = new()
		{
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			Value = value,
			CustomMinimumSize = new Vector2(140, 0)
		};

		row.AddChild(input);
		return input;
	}

	// =========================================================
// Generates one selection button per valid registered enemy.
// Runs once during setup, without folder scans or per-frame checks.
// =========================================================
private void PopulateEnemies()
{
	if (Catalog == null)
	{
		_status.Text = "Assign the shared EnemyCatalog resource.";
		return;
	}

	Catalog.Validate();

	System.Collections.Generic.HashSet<string> keys = new();

	foreach (EnemyRegistration entry in Catalog.Entries)
	{
		if (entry == null || !entry.IsValid || !keys.Add(entry.Key))
		{
			continue;
		}

		EnemyRegistration registration = entry;

		Button button = new()
		{
			Text = registration.DisplayName,
			TooltipText = registration.Key,
			ToggleMode = true,
			ButtonGroup = _enemyButtonGroup,
			Alignment = HorizontalAlignment.Left
		};

		button.Pressed += () =>
		{
			_selectedEnemy = registration;
			_status.Text = $"Selected: {registration.DisplayName}";
		};

		_enemyButtons.AddChild(button);

		if (_selectedEnemy == null)
		{
			_selectedEnemy = registration;
			_firstEnemyButton = button;
			button.ButtonPressed = true;
		}
	}

	_status.Text = _selectedEnemy == null
		? "No valid enemies are registered."
		: $"Selected: {_selectedEnemy.DisplayName}";
}

	#endregion

	#region Menu Control

	// =========================================================
	// Toggles the menu before gameplay receives the F1 key event.
	// =========================================================
	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey key
			|| !key.Pressed
			|| key.Echo
			|| key.PhysicalKeycode != ToggleKey)
		{
			return;
		}

		GetViewport().SetInputAsHandled();

		if (_open)
		{
			CloseMenu();
		}
		else
		{
			OpenMenu();
		}
	}

	// =========================================================
	// Pauses an active game while preserving its previous mouse mode.
	// Does not take ownership of an existing pause or sector transition.
	// =========================================================
	private void OpenMenu()
	{
		SectorManager manager = GetTree().GetFirstNodeInGroup(
			"sector_manager"
		) as SectorManager;

		if (GetTree().Paused
			|| (GodotObject.IsInstanceValid(manager) && manager.IsTravelling))
		{
			return;
		}

		_previousMouseMode = Input.MouseMode;

		_open = true;
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_overlay.Visible = true;

		_firstEnemyButton?.GrabFocus();
	}

	// =========================================================
	// Restores gameplay and the mouse mode used before opening this menu.
	// =========================================================
	private void CloseMenu()
	{
		if (!_open)
		{
			return;
		}

		_open = false;
		_overlay.Visible = false;
		GetTree().Paused = false;
		Input.MouseMode = _previousMouseMode;
	}

	// =========================================================
	// Releases this menu's pause if Level is removed while the menu is open.
	// =========================================================
	public override void _ExitTree()
	{
		if (_open)
		{
			GetTree().Paused = false;
			Input.MouseMode = _previousMouseMode;
		}
	}

	#endregion

	#region Spawning

	// =========================================================
// Creates the selected registered enemy ahead of the camera.
// The active sector owns spawned ships and removes them on travel.
// =========================================================
private void SpawnEnemies()
{
	if (!_open || _selectedEnemy == null)
	{
		return;
	}

	Camera3D camera = GetViewport().GetCamera3D();
	Node parent = WorldSector.GetContentParent(this);

	if (!GodotObject.IsInstanceValid(camera)
		|| parent is not Node3D spatialParent)
	{
		_status.Text = "No valid camera or spatial sector root.";
		return;
	}

	int amount = (int)_count.Value;
	float distance = (float)_distance.Value;
	float spacing = (float)_spacing.Value;

	Basis cameraBasis = camera.GlobalBasis.Orthonormalized();
	Vector3 right = cameraBasis.X;
	Vector3 centre = camera.GlobalPosition - cameraBasis.Z * distance;

	PlayerShip player = GetTree().GetFirstNodeInGroup(
		"player_ship"
	) as PlayerShip;

	Vector3 target = GodotObject.IsInstanceValid(player)
		? player.GlobalPosition
		: camera.GlobalPosition;

	int spawned = 0;

	for (int number = 0; number < amount; number++)
	{
		EnemyShip enemy = _selectedEnemy.CreateInstance();

		if (enemy == null)
		{
			_status.Text = $"Could not create {_selectedEnemy.DisplayName}.";
			return;
		}

		float offset = (number - (amount - 1) * 0.5f) * spacing;
		Vector3 position = centre + right * offset;
		Vector3 direction = target - position;

		Transform3D worldTransform = new(Basis.Identity, position);

		if (direction.LengthSquared() > 0.001f)
		{
			Vector3 up = Mathf.Abs(
				direction.Normalized().Dot(Vector3.Up)
			) > 0.99f ? Vector3.Right : Vector3.Up;

			worldTransform = worldTransform.LookingAt(target, up);
		}

		// Set placement before Ready initializes the enemy's components.
		enemy.Transform =
			spatialParent.GlobalTransform.AffineInverse() * worldTransform;

		enemy.ProcessMode = Node.ProcessModeEnum.Inherit;
		spatialParent.AddChild(enemy);
		spawned++;
	}

	_status.Text =
		$"Spawned {spawned} × {_selectedEnemy.DisplayName}. F1 to resume.";
}

	#endregion
}

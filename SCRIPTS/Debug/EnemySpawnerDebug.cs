using Godot;

// Builds a paused F1 enemy-spawner menu for Level or standalone test scenes.
public partial class EnemySpawnerDebug : CanvasLayer
{
	#region Configuration

	[Export] public Key ToggleKey = Key.F1;

	// Add future enemy scenes here through the Inspector.
	[Export] public Godot.Collections.Array<PackedScene> EnemyScenes = new();

	#endregion

	#region Runtime

	private Control _overlay;
	private OptionButton _enemyChoice;
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
	// Creates a centred panel and a full-screen mouse-blocking backdrop.
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
		column.AddThemeConstantOverride("separation", 14);
		panel.AddChild(column);

		Label title = new() { Text = "DEBUG / ENEMY SPAWNER" };
		title.AddThemeFontSizeOverride("font_size", 24);
		title.AddThemeColorOverride(
			"font_color", new Color(0.3f, 0.9f, 1.0f)
		);
		column.AddChild(title);

		column.AddChild(new Label
		{
			Text = "GAME PAUSED — F1 TO RESUME"
		});

		column.AddChild(new HSeparator());

		column.AddChild(new Label { Text = "Enemy scene" });

		_enemyChoice = new OptionButton();
		column.AddChild(_enemyChoice);

		_count = AddNumber(column, "Amount", 1, 20, 1, 1);
		_distance = AddNumber(column, "Distance ahead", 30, 1000, 10, 180);
		_spacing = AddNumber(column, "Spacing between ships", 30, 200, 5, 50);

		Button spawn = new() { Text = "SPAWN ENEMIES" };
		spawn.Pressed += SpawnEnemies;
		column.AddChild(spawn);

		_status = new Label
		{
			Text = "Enemies spawn ahead of the current camera.",
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
	// Displays the configured enemy scenes without loading a separate registry.
	// =========================================================
	private void PopulateEnemies()
	{
		_enemyChoice.Clear();

		for (int index = 0; index < EnemyScenes.Count; index++)
		{
			PackedScene scene = EnemyScenes[index];

			if (scene == null)
			{
				continue;
			}

			string name = scene.ResourcePath.GetFile().GetBaseName();

			if (string.IsNullOrWhiteSpace(name))
			{
				name = $"Enemy {index + 1}";
			}

			_enemyChoice.AddItem(name, index);
		}

		if (_enemyChoice.ItemCount == 0)
		{
			_status.Text = "Assign EnemyScenes to this debug menu.";
		}
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

		_enemyChoice.GrabFocus();
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
	// Spawns a centred row of enemies ahead of the active camera.
	// The current sector owns them, so sector travel removes them normally.
	// =========================================================
	private void SpawnEnemies()
	{
		if (!_open || _enemyChoice.ItemCount == 0)
		{
			return;
		}

		int index = _enemyChoice.GetSelectedId();

		if (index < 0 || index >= EnemyScenes.Count)
		{
			return;
		}

		PackedScene scene = EnemyScenes[index];
		Camera3D camera = GetViewport().GetCamera3D();
		Node parent = WorldSector.GetContentParent(this);

		if (scene == null
			|| !GodotObject.IsInstanceValid(camera)
			|| !GodotObject.IsInstanceValid(parent))
		{
			_status.Text = "No valid enemy scene, camera, or sector.";
			return;
		}

		int amount = (int)_count.Value;
		float distance = (float)_distance.Value;
		float spacing = (float)_spacing.Value;

		Basis cameraBasis = camera.GlobalBasis.Orthonormalized();
		Vector3 forward = -cameraBasis.Z;
		Vector3 right = cameraBasis.X;
		Vector3 centre = camera.GlobalPosition + forward * distance;

		PlayerShip player = GetTree().GetFirstNodeInGroup(
			"player_ship"
		) as PlayerShip;

		int spawned = 0;

		for (int number = 0; number < amount; number++)
		{
			Node instance = scene.Instantiate();

			if (instance is not EnemyShip enemy || enemy.Definition == null)
			{
				instance.Free();
				_status.Text = "Selected scene needs EnemyShip with a Definition.";
				return;
			}

			// Enemy gameplay inherits the sector's paused state,
			// rather than this menu's Always process mode.
			enemy.ProcessMode = Node.ProcessModeEnum.Inherit;

			float offset = (number - (amount - 1) * 0.5f) * spacing;
			Vector3 position = centre + right * offset;

			parent.AddChild(enemy);
			enemy.GlobalPosition = position;

			Vector3 target = GodotObject.IsInstanceValid(player)
				? player.GlobalPosition
				: camera.GlobalPosition;

			Vector3 direction = target - position;

			if (direction.LengthSquared() > 0.001f)
			{
				Vector3 up = Mathf.Abs(
					direction.Normalized().Dot(Vector3.Up)
				) > 0.99f ? Vector3.Right : Vector3.Up;

				enemy.LookAt(target, up);
			}

			spawned++;
		}

		_status.Text = $"Spawned {spawned}. Press F1 to resume.";
	}

	#endregion
}

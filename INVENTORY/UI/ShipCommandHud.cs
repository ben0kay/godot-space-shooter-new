using Godot;

// Opens the Ship Command inventory and temporarily gives it ownership of input.
public partial class ShipCommandHud : CanvasLayer
{
	#region Configuration

	[Export] public NodePath PlayerPath = new("../PlayerShip");
	[Export] public Key ToggleKey = Key.E;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private CargoHold _cargo;
	private ShipCommandView _view;
	private ColorRect _backdrop;

	private bool _open;
	private bool _previousInteraction;
	private Input.MouseModeEnum _previousMouseMode;

	private int _pressedSlot = -1;
	private Vector2 _pressPosition;
	private bool _dragging;

	#endregion

	#region Setup

	// =========================================================
	// Creates the overlay and connects it to the persistent player's cargo.
	public override void _Ready()
	{
		Layer = 30;

		_ship = GetNodeOrNull<PlayerShip>(PlayerPath);
		_cargo = _ship?.GetNodeOrNull<CargoHold>("Cargo");

		if (_ship == null || _cargo == null)
		{
			GD.PushError(
				"ShipCommandHud requires a PlayerShip with a Cargo node."
			);

			SetProcessInput(false);
			return;
		}

		_backdrop = new ColorRect
		{
			Color = new Color(0.0f, 0.015f, 0.025f, 0.20f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false
		};

		AddChild(_backdrop);

		_view = new ShipCommandView
		{
			Cargo = _cargo,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		AddChild(_view);

		_cargo.Changed += OnCargoChanged;
		GetViewport().SizeChanged += UpdateLayout;

		UpdateLayout();
	}

	// =========================================================
	// Fits the original inventory proportions within the current viewport.
	private void UpdateLayout()
	{
		if (_view == null)
		{
			return;
		}

		Vector2 viewport = GetViewport().GetVisibleRect().Size;
		Vector2 design = new(1760.0f, 940.0f);

		float scale = Mathf.Max(
			0.01f,
			Mathf.Min(
				(viewport.X - 32.0f) / design.X,
				(viewport.Y - 32.0f) / design.Y
			)
		);

		_backdrop.Size = viewport;

		_view.Size = design;
		_view.Scale = Vector2.One * scale;
		_view.Position = (viewport - design * scale) * 0.5f;

		_view.QueueRedraw();
	}

	// =========================================================
	// Disconnects events and restores input if this overlay is removed.
	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_cargo))
		{
			_cargo.Changed -= OnCargoChanged;
		}

		GetViewport().SizeChanged -= UpdateLayout;

		if (_open && GodotObject.IsInstanceValid(_ship))
		{
			SetOpen(false);
		}
	}

	#endregion

	#region Input

	// =========================================================
	// Toggles the screen and consumes gameplay input while the inventory is open.
	public override void _Input(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_ship) || _view == null)
		{
			return;
		}

		if (inputEvent is InputEventKey key
			&& key.Pressed
			&& !key.Echo)
		{
			if (key.PhysicalKeycode == ToggleKey)
			{
				SetOpen(!_open);
				GetViewport().SetInputAsHandled();
				return;
			}

			if (_open && key.PhysicalKeycode == Key.Escape)
			{
				SetOpen(false);
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (!_open)
		{
			return;
		}

		if (inputEvent is InputEventMouseMotion)
		{
			UpdatePointer();
		}
		else if (inputEvent is InputEventMouseButton button
			&& button.ButtonIndex == MouseButton.Left)
		{
			if (button.Pressed)
			{
				PressPointer();
			}
			else
			{
				ReleasePointer();
			}
		}

		// Input is handled here rather than forwarded to cockpit screens.
		GetViewport().SetInputAsHandled();
	}

	// =========================================================
	// Selects tabs and slots, sorts cargo, or releases a selected stack into space.
	private void PressPointer()
	{
		Vector2 pointer = _view.GetLocalMousePosition();

		_pressedSlot = -1;
		_dragging = false;
		_view.DragSlot = -1;

		int tab = _view.GetTabAt(pointer);

		if (tab >= 0)
		{
			_view.ActiveTab = tab;
			_view.QueueRedraw();
			return;
		}

		if (_view.ActiveTab != 0)
		{
			return;
		}

		if (ShipCommandView.SortBounds.HasPoint(pointer))
		{
			_cargo.Sort();
			_view.SelectedSlot = -1;
			_view.QueueRedraw();
			return;
		}

		if (ShipCommandView.DropBounds.HasPoint(pointer))
		{
			if (CargoPickup.DropFromCargo(
				_ship,
				_cargo,
				_view.SelectedSlot
			))
			{
				_view.SelectedSlot = -1;
			}

			_view.QueueRedraw();
			return;
		}

		int slot = _view.GetSlotAt(pointer);

		_view.SelectedSlot = slot;
		_pressedSlot = slot;
		_pressPosition = pointer;

		_view.QueueRedraw();
	}

	// =========================================================
	// Starts dragging only after the pointer moves away from a nonempty slot.
	private void UpdatePointer()
	{
		Vector2 pointer = _view.GetLocalMousePosition();

		if (_pressedSlot >= 0
			&& !_cargo.GetSlot(_pressedSlot).IsEmpty
			&& pointer.DistanceTo(_pressPosition) >= 8.0f)
		{
			_dragging = true;
		}

		_view.DragSlot = _dragging ? _pressedSlot : -1;
		_view.DragPosition = pointer;
		_view.QueueRedraw();
	}

	// =========================================================
	// Moves, merges, or swaps a dragged stack when released over another slot.
	private void ReleasePointer()
	{
		if (_dragging)
		{
			int target = _view.GetSlotAt(
				_view.GetLocalMousePosition()
			);

			if (target >= 0
				&& _cargo.MoveSlot(_pressedSlot, target))
			{
				_view.SelectedSlot = target;
			}
		}

		_pressedSlot = -1;
		_dragging = false;
		_view.DragSlot = -1;

		_view.QueueRedraw();
	}

	#endregion

	#region Visibility

	// =========================================================
	// Transfers control to the inventory and restores the previous control mode.
	private void SetOpen(bool open)
	{
		if (_open == open)
		{
			return;
		}

		if (open && !_ship.IsCombatTargetable)
		{
			return;
		}

		if (open)
		{
			_previousInteraction = _ship.CockpitInteractionActive;
			_previousMouseMode = Input.MouseMode;

			_ship.SetCockpitInteraction(true);

			PlayerWeaponInput weapons =
				_ship.GetNodeOrNull<PlayerWeaponInput>(
					"PlayerWeaponMount"
				);

			weapons?.PrimaryWeapon?.SetTriggerHeld(false);
			weapons?.SecondaryWeapon?.SetTriggerHeld(false);
		}
		else
		{
			_ship.SetCockpitInteraction(_previousInteraction);

			Input.MouseMode = _ship.IsCombatTargetable
				? _previousMouseMode
				: Input.MouseModeEnum.Visible;
		}

		_open = open;
		_backdrop.Visible = open;
		_view.Visible = open;

		_pressedSlot = -1;
		_dragging = false;
		_view.DragSlot = -1;

		if (open)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			_view.QueueRedraw();
		}
	}

	// =========================================================
	// Requests a redraw only when cargo contents actually change.
	private void OnCargoChanged()
	{
		if (_open)
		{
			_view.QueueRedraw();
		}
	}

	#endregion
}
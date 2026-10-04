using System.Collections.Generic;
using Godot;

// Displays connected destinations and handles keyboard-driven jump selection.
public partial class SectorTravelSelector : CanvasLayer
{
	#region Configuration

	[Export] public SectorManager Manager;
	[Export] public Key SelectionKey = Key.J;

	#endregion

	#region Runtime

	private readonly List<SectorDefinition> _routes = new();

	private PanelContainer _panel;
	private Label _destination;
	private Label _fuel;
	private Label _status;
	private Label _counter;

	private int _selected;
	private float _refreshTimer;
	private bool _open;

	#endregion

	#region Setup

	// =========================================================
	// Creates the selector once and leaves updates disabled until opened.
	public override void _Ready()
	{
		Layer = 60;

		Manager ??= GetTree().GetFirstNodeInGroup(
			"sector_manager"
		) as SectorManager;

		if (!GodotObject.IsInstanceValid(Manager))
		{
			GD.PushError("SectorTravelSelector needs a SectorManager.");
			SetProcess(false);
			SetProcessInput(false);
			return;
		}

		BuildPanel();
		SetProcess(false);
	}

	// =========================================================
	// Builds a compact translucent navigation panel without taking mouse focus.
	private void BuildPanel()
	{
		Control root = new()
		{
			Name = "TravelSelector",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_panel = new PanelContainer
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		root.AddChild(_panel);
		_panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		_panel.OffsetLeft = -230;
		_panel.OffsetRight = 230;
		_panel.OffsetTop = 90;
		_panel.OffsetBottom = 260;

		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.015f, 0.035f, 0.055f, 0.92f),
			BorderColor = new Color(0.20f, 0.65f, 0.80f, 0.75f),
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			BorderWidthTop = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 18,
			ContentMarginRight = 18,
			ContentMarginTop = 12,
			ContentMarginBottom = 12
		};
		_panel.AddThemeStyleboxOverride("panel", style);

		VBoxContainer content = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		content.AddThemeConstantOverride("separation", 5);
		_panel.AddChild(content);

		AddLabel(content, "HYPERDRIVE / DESTINATION", 13,
			new Color(0.45f, 0.70f, 0.80f));

		_destination = AddLabel(content, "", 22,
			new Color(0.65f, 0.95f, 1.0f));

		_counter = AddLabel(content, "", 12,
			new Color(0.50f, 0.65f, 0.72f));

		_fuel = AddLabel(content, "", 14, Colors.White);
		_status = AddLabel(content, "", 14, Colors.White);

		AddLabel(content, "J  NEXT     ENTER  JUMP     ESC  CANCEL",
			12, new Color(0.55f, 0.75f, 0.82f));
	}

	// =========================================================
	// Creates a consistently styled label that cannot intercept mouse input.
	private Label AddLabel(
		VBoxContainer parent, string text, int size, Color color
	)
	{
		Label label = new()
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		parent.AddChild(label);
		return label;
	}

	#endregion

	#region Selection

	// =========================================================
	// Opens the current sector's valid routes without filtering out low-fuel routes.
	private void OpenSelector()
	{
		_routes.Clear();
		_selected = 0;

		if (Manager.CurrentDefinition == null)
			return;

		foreach (string key in Manager.CurrentDefinition.Connections)
		{
			SectorDefinition definition = Manager.FindDefinition(key);

			if (definition != null
				&& definition.Key != Manager.CurrentDefinition.Key
				&& !_routes.Contains(definition))
				_routes.Add(definition);
		}

		_open = true;
		_panel.Visible = true;
		_refreshTimer = 0.0f;
		SetProcess(true);
		RefreshPanel();
	}

	// =========================================================
	// Closes selection without changing flight or mouse capture.
	private void CloseSelector()
	{
		_open = false;
		_panel.Visible = false;
		SetProcess(false);
	}

	// =========================================================
	// Refreshes the selected destination and its current jump eligibility.
	private void RefreshPanel()
	{
		if (_routes.Count == 0)
		{
			_destination.Text = "NO CONNECTED DESTINATIONS";
			_counter.Text = "";
			_fuel.Text = "";
			SetStatus("No routes available.", false);
			return;
		}

		SectorDefinition destination = _routes[_selected];

		_destination.Text = destination.DisplayName.ToUpperInvariant();
		_counter.Text = $"ROUTE {_selected + 1} / {_routes.Count}";

		float fuel = Manager.Player?.Resources?.Fuel ?? 0.0f;
		_fuel.Text = $"FUEL COST {Manager.GetJumpFuelCost():0.#}"
			+ $"   /   AVAILABLE {fuel:0.#}";

		bool allowed = Manager.CheckTravelTo(
			destination.Key, out string reason
		);
		SetStatus(allowed ? "READY TO JUMP" : reason, allowed);
	}

	// =========================================================
	// Displays readiness or a blocking reason using distinct colours.
	private void SetStatus(string text, bool ready)
	{
		_status.Text = text;
		_status.AddThemeColorOverride("font_color", ready
			? new Color(0.40f, 0.95f, 0.75f)
			: new Color(1.0f, 0.55f, 0.35f));
	}

	// =========================================================
	// Confirms the selected route and closes only when departure starts.
	private void ConfirmSelection()
	{
		if (_routes.Count == 0)
			return;

		string key = _routes[_selected].Key;

		if (!Manager.CheckTravelTo(key, out string reason))
		{
			SetStatus(reason, false);
			return;
		}

		if (Manager.TravelTo(key))
			CloseSelector();
		else
			SetStatus("Could not start jump. Check Output.", false);
	}

	#endregion

	#region Updates and Input

	// =========================================================
	// Checks changing fuel at a low frequency while the selector is visible.
	public override void _Process(double delta)
	{
		if (Manager.IsTravelling || !Manager.Player.IsCombatTargetable)
		{
			CloseSelector();
			return;
		}

		_refreshTimer -= (float)delta;

		if (_refreshTimer > 0.0f)
			return;

		_refreshTimer = 0.2f;
		RefreshPanel();
	}

	// =========================================================
	// Opens or cycles with J, confirms with Enter and cancels with Escape.
	public override void _Input(InputEvent inputEvent)
	{
		if (GetTree().Paused
			|| Manager.IsTravelling
			|| !Manager.Player.IsCombatTargetable
			|| inputEvent is not InputEventKey key
			|| !key.Pressed || key.Echo)
			return;

		Key pressed = key.PhysicalKeycode != Key.None
			? key.PhysicalKeycode : key.Keycode;

		if (pressed == SelectionKey)
		{
			if (!_open)
				OpenSelector();
			else if (_routes.Count > 0)
			{
				_selected = (_selected + 1) % _routes.Count;
				RefreshPanel();
			}

			GetViewport().SetInputAsHandled();
		}
		else if (_open
			&& (pressed == Key.Enter || pressed == Key.KpEnter))
		{
			ConfirmSelection();
			GetViewport().SetInputAsHandled();
		}
		else if (_open && pressed == Key.Escape)
		{
			CloseSelector();
			GetViewport().SetInputAsHandled();
		}
	}

	#endregion
}
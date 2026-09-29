using Godot;

public partial class PlayerDefenceHud : CanvasLayer
{
	#region Runtime

	private PlayerShip _ship;
	private ProgressBar _shieldBar;
	private ProgressBar _armourBar;
	private ProgressBar _hullBar;

	#endregion

	#region Godot Events

	// Builds the bars and subscribes to player defence changes.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		if (_ship == null || _ship.Defence == null)
		{
			GD.PushError("PlayerDefenceHud needs a PlayerShip parent with defence.");
			return;
		}

		VBoxContainer layout = new VBoxContainer();
		layout.Position = new Vector2(20, 20);
		AddChild(layout);

		_shieldBar = CreateBar(layout, "SHIELD", new Color(0.25f, 0.70f, 1.0f));
		_armourBar = CreateBar(layout, "ARMOUR", new Color(0.90f, 0.65f, 0.25f));
		_hullBar = CreateBar(layout, "HULL", new Color(0.85f, 0.25f, 0.25f));

		_ship.DefenceChanged += Refresh;
		Refresh();
	}

	// Removes the subscription when the HUD leaves the scene.
	public override void _ExitTree()
	{
		if (_ship != null)
		{
			_ship.DefenceChanged -= Refresh;
		}
	}

	#endregion

	#region Display

	// Creates one labelled progress bar.
	private ProgressBar CreateBar(
		VBoxContainer layout,
		string title,
		Color color
	)
	{
		HBoxContainer row = new HBoxContainer();
		layout.AddChild(row);

		Label label = new Label();
		label.Text = title;
		label.CustomMinimumSize = new Vector2(70, 0);
		label.AddThemeColorOverride("font_color", color);
		row.AddChild(label);

		ProgressBar bar = new ProgressBar();
		bar.CustomMinimumSize = new Vector2(220, 20);
		bar.ShowPercentage = true;
		row.AddChild(bar);

		return bar;
	}

	// Copies the current shield, armour, and hull into the bars.
	private void Refresh()
	{
		ShipDefence defence = _ship.Defence;

		UpdateBar(_shieldBar, defence.Shield, defence.MaxShield);
		UpdateBar(_armourBar, defence.Armour, defence.MaxArmour);
		UpdateBar(_hullBar, defence.Hull, defence.MaxHull);
	}

	// Updates one bar while keeping its maximum above zero.
	private void UpdateBar(ProgressBar bar, float current, float maximum)
	{
		bar.MaxValue = Mathf.Max(1.0f, maximum);
		bar.Value = current;
	}

	#endregion
}

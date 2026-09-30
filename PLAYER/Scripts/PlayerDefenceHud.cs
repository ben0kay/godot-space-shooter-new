using Godot;

// Displays player defence and flight information as a compact vector HUD.
// Uses the existing PlayerShip defence event and draws without UI textures.
public partial class PlayerDefenceHud : CanvasLayer
{
	#region Display Settings

	[Export] public Color AccentColor { get; set; } =
		new Color(0.20f, 0.80f, 1.00f);

	[Export] public Color ShieldColor { get; set; } =
		new Color(0.20f, 0.80f, 1.00f);

	[Export] public Color ArmourColor { get; set; } =
		new Color(0.95f, 0.68f, 0.28f);

	[Export] public Color HullColor { get; set; } =
		new Color(0.95f, 0.35f, 0.35f);

	[Export] public float EdgeMargin { get; set; } = 24.0f;
	[Export] public float HudScale { get; set; } = 1.0f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private HudCanvas _canvas;
	private float _refreshRemaining;

	#endregion

	#region Godot Events

	// =========================================================
	// Creates the drawing surface and connects defence updates.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		if (_ship == null || _ship.Defence == null)
		{
			GD.PushError(
				"PlayerDefenceHud needs a PlayerShip parent with defence."
			);

			SetProcess(false);
			return;
		}

		_canvas = new HudCanvas();
		_canvas.Hud = this;
		_canvas.MouseFilter = Control.MouseFilterEnum.Ignore;

		AddChild(_canvas);

		_canvas.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_ship.DefenceChanged += Refresh;
		Refresh();
	}

	// =========================================================
	// Refreshes flight information ten times per second.
	public override void _Process(double delta)
	{
		_refreshRemaining -= (float)delta;

		if (_refreshRemaining > 0.0f)
		{
			return;
		}

		_refreshRemaining = 0.1f;
		Refresh();
	}

	// =========================================================
	// Disconnects the defence event when the HUD is removed.
	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_ship))
		{
			_ship.DefenceChanged -= Refresh;
		}
	}

	#endregion

	#region Drawing

	// =========================================================
	// Requests a redraw without rebuilding any HUD nodes.
	private void Refresh()
	{
		if (GodotObject.IsInstanceValid(_canvas))
		{
			_canvas.QueueRedraw();
		}
	}

	// =========================================================
	// Draws defence and flight panels relative to the viewport.
	private void DrawHud(Control surface)
	{
		if (!GodotObject.IsInstanceValid(_ship))
		{
			return;
		}

		float scale = Mathf.Clamp(HudScale, 0.5f, 2.0f);
		float margin = Mathf.Max(0.0f, EdgeMargin);
		Vector2 viewport = surface.Size;

		Vector2 defenceOrigin = new Vector2(
			margin,
			viewport.Y - margin - 142.0f * scale
		);

		surface.DrawSetTransform(
			defenceOrigin,
			0.0f,
			Vector2.One * scale
		);

		DrawDefencePanel(surface);

		Vector2 flightOrigin = new Vector2(
			margin + 352.0f * scale,
			viewport.Y - margin - 54.0f * scale
		);

		surface.DrawSetTransform(
			flightOrigin,
			0.0f,
			Vector2.One * scale
		);

		DrawFlightPanel(surface);

		surface.DrawSetTransform(
			Vector2.Zero,
			0.0f,
			Vector2.One
		);
	}

	// =========================================================
	// Draws the three defence layers and their current values.
	private void DrawDefencePanel(Control surface)
	{
		DrawFrame(surface, new Vector2(328.0f, 142.0f));

		DrawText(
			surface,
			"DEFENCE",
			new Vector2(14.0f, 23.0f),
			AccentColor,
			13
		);

		Color divider = AccentColor;
		divider.A = 0.25f;

		surface.DrawLine(
			new Vector2(14.0f, 33.0f),
			new Vector2(314.0f, 33.0f),
			divider,
			1.0f,
			true
		);

		ShipDefence defence = _ship.Defence;

		DrawDefenceRow(
			surface, "SHIELD", 45.0f,
			defence.Shield, defence.MaxShield, ShieldColor
		);

		DrawDefenceRow(
			surface, "ARMOUR", 76.0f,
			defence.Armour, defence.MaxArmour, ArmourColor
		);

		DrawDefenceRow(
			surface, "HULL", 107.0f,
			defence.Hull, defence.MaxHull, HullColor
		);
	}

	// =========================================================
	// Draws one labelled defence bar with a numeric readout.
	private void DrawDefenceRow(
		Control surface,
		string title,
		float y,
		float current,
		float maximum,
		Color color
	)
	{
		float fraction = maximum > 0.0f
			? Mathf.Clamp(current / maximum, 0.0f, 1.0f)
			: 0.0f;

		DrawText(
			surface,
			title,
			new Vector2(14.0f, y + 11.0f),
			color,
			12
		);

		string values = maximum > 0.0f
			? $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}"
			: "—";

		DrawText(
			surface,
			values,
			new Vector2(314.0f, y + 11.0f),
			color,
			12,
			true
		);

		Rect2 track = new Rect2(
			14.0f, y + 17.0f, 300.0f, 5.0f
		);

		Color trackColor = color;
		trackColor.A = 0.12f;
		surface.DrawRect(track, trackColor);

		if (fraction > 0.0f)
		{
			Rect2 fill = new Rect2(
				track.Position,
				new Vector2(track.Size.X * fraction, track.Size.Y)
			);

			surface.DrawRect(fill, color);
		}
	}

	// =========================================================
	// Draws movement speed and the current flight status.
	private void DrawFlightPanel(Control surface)
	{
		DrawFrame(surface, new Vector2(260.0f, 54.0f));

		DrawText(
			surface,
			"SPEED",
			new Vector2(14.0f, 20.0f),
			AccentColor,
			11
		);

		DrawText(
			surface,
			$"{_ship.Velocity.Length():0.0}",
			new Vector2(14.0f, 42.0f),
			Colors.White,
			18
		);

		string status;

		if (_ship.Defence.Destroyed)
		{
			status = "OFFLINE";
		}
		else if (_ship.IsDashing)
		{
			status = "DASH";
		}
		else if (_ship.IsBoosting)
		{
			status = "BOOST";
		}
		else
		{
			status = "CRUISE";
		}

		DrawText(
			surface,
			status,
			new Vector2(246.0f, 34.0f),
			AccentColor,
			13,
			true
		);
	}

	// =========================================================
	// Draws a transparent panel with clipped corner brackets.
	private void DrawFrame(Control surface, Vector2 size)
	{
		surface.DrawRect(
			new Rect2(Vector2.Zero, size),
			new Color(0.01f, 0.025f, 0.04f, 0.35f)
		);

		Color faint = AccentColor;
		faint.A = 0.18f;

		surface.DrawLine(
			new Vector2(12.0f, 0.0f),
			new Vector2(size.X - 12.0f, 0.0f),
			faint,
			1.0f,
			true
		);

		surface.DrawLine(
			new Vector2(12.0f, size.Y),
			new Vector2(size.X - 12.0f, size.Y),
			faint,
			1.0f,
			true
		);

		for (int corner = 0; corner < 4; corner++)
		{
			float x = (corner & 1) == 0 ? 0.0f : size.X;
			float y = (corner & 2) == 0 ? 0.0f : size.Y;
			float directionX = x == 0.0f ? 1.0f : -1.0f;
			float directionY = y == 0.0f ? 1.0f : -1.0f;

			Vector2[] points =
			{
				new Vector2(x, y + directionY * 17.0f),
				new Vector2(x, y + directionY * 5.0f),
				new Vector2(x + directionX * 5.0f, y),
				new Vector2(x + directionX * 25.0f, y)
			};

			surface.DrawPolyline(points, AccentColor, 1.0f, true);
		}
	}

	// =========================================================
	// Draws text using Godot's default font and optional right alignment.
	private void DrawText(
		Control surface,
		string text,
		Vector2 position,
		Color color,
		int fontSize,
		bool alignRight = false
	)
	{
		Font font = ThemeDB.FallbackFont;

		if (alignRight)
		{
			position.X -= font.GetStringSize(
				text,
				HorizontalAlignment.Left,
				-1,
				fontSize
			).X;
		}

		surface.DrawString(
			font,
			position,
			text,
			HorizontalAlignment.Left,
			-1,
			fontSize,
			color
		);
	}

	#endregion

	#region Drawing Surface

	// Forwards Godot drawing and resize notifications to the HUD.
	private partial class HudCanvas : Control
	{
		public PlayerDefenceHud Hud { get; set; }

		// =========================================================
		// Draws the HUD using this control's canvas.
		public override void _Draw()
		{
			Hud?.DrawHud(this);
		}

		// =========================================================
		// Redraws the anchored panels when the viewport changes size.
		public override void _Notification(int what)
		{
			if (what == NotificationResized)
			{
				QueueRedraw();
			}
		}
	}

	#endregion
}

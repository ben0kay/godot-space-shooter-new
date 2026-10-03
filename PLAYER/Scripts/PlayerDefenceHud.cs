using Godot;

// Draws thin third-person vector HUD bars for progression, navigation and resources.
// Uses cached references and refreshes ten times per second without rebuilding nodes.
public partial class PlayerDefenceHud : CanvasLayer
{
	#region Appearance

	[Export] public Color AccentColor = new(0.20f, 0.85f, 1.0f);
	[Export] public Color PanelColor = new(0.01f, 0.025f, 0.04f, 0.65f);
	[Export] public float EdgeMargin = 16.0f;

	// Controls the vertical size of both HUD bars without changing their width.
[Export(PropertyHint.Range, "0.4,1.0,0.05")]
public float BarHeightScale { get; set; } = 0.65f;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private PlayerFlightVisuals _flight;
	private SectorManager _sectors;
	private CargoHold _cargo;
	private HudCanvas _canvas;
	private float _refreshRemaining;

	private const float DESIGN_WIDTH = 1280.0f;
	private const float BAR_HEIGHT = 58.0f;

	#endregion

	#region Setup

	// =========================================================
	// Creates the drawing surface after the player's runtime has been initialized.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		if (_ship == null || _ship.Defence == null)
		{
			GD.PushError("PlayerDefenceHud requires a PlayerShip parent.");
			SetProcess(false);
			return;
		}

		Layer = 5;
		_cargo = _ship.GetNodeOrNull<CargoHold>("Cargo");

		_canvas = new HudCanvas
		{
			Hud = this,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		AddChild(_canvas);
		_canvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		// FlightVisuals may be created later in the parent's _Ready method.
		CallDeferred(nameof(CacheReferences));
	}

	// =========================================================
	// Caches presentation and sector references once the scene is ready.
	private void CacheReferences()
	{
		_flight = _ship.GetNodeOrNull<PlayerFlightVisuals>("FlightVisuals");
		_sectors = GetTree().GetFirstNodeInGroup("sector_manager") as SectorManager;
		_canvas.QueueRedraw();
	}

	#endregion

	#region Updates

	// =========================================================
	// Hides the third-person HUD in cockpit view and limits redraw frequency.
	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_canvas)) return;

		bool visible = !GodotObject.IsInstanceValid(_flight)
			|| !_flight.IsFirstPerson;

		if (_canvas.Visible != visible)
		{
			_canvas.Visible = visible;
			if (visible) _canvas.QueueRedraw();
		}

		if (!visible) return;

		_refreshRemaining -= (float)delta;
		if (_refreshRemaining > 0.0f) return;

		_refreshRemaining = 0.1f;
		_canvas.QueueRedraw();
	}

	#endregion

	#region Layout

	// =========================================================
// Positions both HUD bars and compresses their height for a slimmer layout.
private void DrawHud(Control surface)
{
	if (!GodotObject.IsInstanceValid(_ship)) return;

	// Lower this for thinner bars; 1.0 restores the original height.
	const float heightScale = 0.65f;

	float margin = Mathf.Max(0.0f, EdgeMargin);
	float availableWidth = surface.Size.X - margin * 2.0f;
	if (availableWidth <= 0.0f) return;

	float scale = availableWidth / DESIGN_WIDTH;
	Vector2 drawScale = new Vector2(scale, scale * heightScale);
	float barHeight = BAR_HEIGHT * drawScale.Y;

	surface.DrawSetTransform(
		new Vector2(margin, margin),
		0.0f,
		drawScale
	);
	DrawTopBar(surface);

	surface.DrawSetTransform(
		new Vector2(margin, surface.Size.Y - margin - barHeight),
		0.0f,
		drawScale
	);
	DrawBottomBar(surface);

	surface.DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
}

	// =========================================================
	// Draws credit and XP totals, centred sector navigation, and speed.
	private void DrawTopBar(Control surface)
	{
		DrawFrame(surface);

		DrawText(surface, "CREDITS", new Vector2(22, 20), AccentColor, 11);
		DrawText(
			surface, _ship.Progression.Credits.ToString("N0"),
			new Vector2(22, 43), Colors.White, 19
		);

		DrawDivider(surface, 185);

		DrawText(surface, "XP", new Vector2(205, 20), AccentColor, 11);
		DrawText(
			surface, _ship.Progression.Experience.ToString("N0"),
			new Vector2(205, 43), Colors.White, 19
		);

		string sector = GodotObject.IsInstanceValid(_sectors)
			? _sectors.CurrentDefinition?.DisplayName : null;

		if (string.IsNullOrWhiteSpace(sector))
			sector = "SANDBOX";

		DrawText(
			surface, "SECTOR: " + sector.ToUpperInvariant(),
			new Vector2(640, 21), AccentColor, 14, true
		);

		Vector3 forward = -_ship.GlobalBasis.Z;
		float horizontal = forward.X * forward.X + forward.Z * forward.Z;

		string heading = horizontal > 0.0001f
			? $"HDG {Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(forward.X, -forward.Z)), 360.0f):000}°"
			: "HDG —";

		DrawText(
			surface, heading, new Vector2(640, 43),
			Colors.White, 12, true
		);

		DrawDivider(surface, 1095);
		DrawText(surface, "SPEED", new Vector2(1120, 20), AccentColor, 11);
		DrawText(
			surface, $"{_ship.Velocity.Length():0.0} m/s",
			new Vector2(1120, 43), Colors.White, 18
		);
	}

	// =========================================================
	// Draws defence, ammunition status, energy, fuel and cargo mass.
	private void DrawBottomBar(Control surface)
	{
		DrawFrame(surface);

		const float cellWidth = DESIGN_WIDTH / 7.0f;
		ShipDefence defence = _ship.Defence;
		PlayerResources resources = _ship.Resources;

		DrawMeter(
			surface, 0, cellWidth, "SHIELD",
			defence.Shield, defence.MaxShield, AccentColor
		);
		DrawMeter(
			surface, 1, cellWidth, "ARMOUR",
			defence.Armour, defence.MaxArmour,
			new Color(0.95f, 0.72f, 0.35f)
		);
		DrawMeter(
			surface, 2, cellWidth, "HULL",
			defence.Hull, defence.MaxHull,
			new Color(1.0f, 0.45f, 0.40f)
		);

		float bulletsX = cellWidth * 3 + 16;
		DrawText(
			surface, "BULLETS", new Vector2(bulletsX, 20),
			AccentColor, 11
		);

		// Ammunition consumption is not implemented yet.
		DrawText(
			surface, "∞", new Vector2(bulletsX, 44),
			Colors.White, 21
		);

		DrawMeter(
			surface, 4, cellWidth, "ENERGY",
			resources?.Energy ?? 0.0f,
			resources?.MaximumEnergy ?? 0.0f, AccentColor
		);
		DrawMeter(
			surface, 5, cellWidth, "FUEL",
			resources?.Fuel ?? 0.0f,
			resources?.MaximumFuel ?? 0.0f, AccentColor
		);
		DrawMeter(
			surface, 6, cellWidth, "CARGO",
			_cargo?.UsedMass ?? 0.0f,
			_cargo?.MaximumMass ?? 0.0f, AccentColor, true
		);

		for (int index = 1; index < 7; index++)
			DrawDivider(surface, index * cellWidth);
	}

	#endregion

	#region Vector Drawing

	// =========================================================
	// Draws one compact segmented meter with actual current and maximum values.
	private void DrawMeter(
		Control surface, int index, float width, string label,
		float current, float maximum, Color color, bool cargo = false
	)
	{
		float x = index * width + 16.0f;
		float fraction = maximum > 0.0f
			? Mathf.Clamp(current / maximum, 0.0f, 1.0f) : 0.0f;

		DrawText(surface, label, new Vector2(x, 20), color, 11);

		string values = maximum > 0.0f
			? cargo
				? $"{current:0.#}/{maximum:0.#} kg"
				: $"{current:0}/{maximum:0}"
			: "—";

		DrawText(
			surface, values, new Vector2(x, 43), Colors.White, 12
		);

		const int segments = 8;
		const float segmentWidth = 5.0f;
		const float gap = 2.0f;
		float meterX = (index + 1) * width - 16.0f
			- segments * (segmentWidth + gap);

		for (int segment = 0; segment < segments; segment++)
		{
			Rect2 rect = new(
				meterX + segment * (segmentWidth + gap),
				31.0f, segmentWidth, 12.0f
			);

			Color dim = color;
			dim.A = 0.16f;
			surface.DrawRect(rect, dim);

			float fill = Mathf.Clamp(fraction * segments - segment, 0.0f, 1.0f);
			if (fill <= 0.0f) continue;

			surface.DrawRect(
				new Rect2(
					rect.Position, new Vector2(segmentWidth * fill, rect.Size.Y)
				),
				color
			);
		}
	}

	// =========================================================
	// Draws a translucent bar with angled ends and short bright accents.
	private void DrawFrame(Control surface)
	{
		Vector2[] outline =
		{
			new(0, 0),
			new(DESIGN_WIDTH, 0),
			new(DESIGN_WIDTH - 22, BAR_HEIGHT),
			new(22, BAR_HEIGHT),
			new(0, 0)
		};

		Vector2[] fill =
		{
			new(0, 0), new(DESIGN_WIDTH, 0),
			new(DESIGN_WIDTH - 22, BAR_HEIGHT), new(22, BAR_HEIGHT)
		};

		surface.DrawColoredPolygon(fill, PanelColor);

		Color edge = AccentColor;
		edge.A = 0.45f;
		surface.DrawPolyline(outline, edge, 1.0f, true);

		surface.DrawLine(
			new Vector2(12, 3), new Vector2(70, 3),
			AccentColor, 2.0f, true
		);
		surface.DrawLine(
			new Vector2(DESIGN_WIDTH - 70, 3),
			new Vector2(DESIGN_WIDTH - 12, 3),
			AccentColor, 2.0f, true
		);
	}

	// =========================================================
	// Separates readouts with a faint vertical line.
	private void DrawDivider(Control surface, float x)
	{
		Color color = AccentColor;
		color.A = 0.25f;

		surface.DrawLine(
			new Vector2(x, 13), new Vector2(x, BAR_HEIGHT - 13),
			color, 1.0f, true
		);
	}

	// =========================================================
	// Draws default-font text with optional horizontal centring.
	private void DrawText(
		Control surface, string text, Vector2 position,
		Color color, int size, bool centred = false
	)
	{
		Font font = ThemeDB.FallbackFont;

		if (centred)
		{
			position.X -= font.GetStringSize(
				text, HorizontalAlignment.Left, -1, size
			).X * 0.5f;
		}

		surface.DrawString(
			font, position, text, HorizontalAlignment.Left, -1, size, color
		);
	}

	#endregion

	#region Drawing Surface

	private partial class HudCanvas : Control
	{
		public PlayerDefenceHud Hud;

		// =========================================================
		// Forwards canvas drawing to the HUD.
		public override void _Draw()
		{
			Hud?.DrawHud(this);
		}

		// =========================================================
		// Refreshes bar placement after viewport resizing.
		public override void _Notification(int what)
		{
			if (what == NotificationResized) QueueRedraw();
		}
	}

	#endregion
}

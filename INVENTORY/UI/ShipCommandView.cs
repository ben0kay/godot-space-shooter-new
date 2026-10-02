using Godot;

// Draws the vector Ship Command interface using the original inventory proportions.
public partial class ShipCommandView : Control
{
	#region Public State

	public CargoHold Cargo;
	public int ActiveTab;
	public int SelectedSlot = -1;
	public int DragSlot = -1;
	public Vector2 DragPosition;

	public static readonly Rect2 SortBounds =
		new(50.0f, 805.0f, 180.0f, 44.0f);

	#endregion

	#region Appearance

	private static readonly Color Background = new("#020a0e");
	private static readonly Color Panel = new("#041015");
	private static readonly Color PanelLight = new("#0a262d");
	private static readonly Color Outline = new("#005b69");
	private static readonly Color Accent = new("#00e8f2");
	private static readonly Color Core = new("#beffff");
	private static readonly Color TextColor = new("#91dae0");
	private static readonly Color Muted = new("#3c8088");
	private static readonly Color CargoColor = new("#2de1cd");

	private static readonly string[] Tabs =
	{
		"CARGO",
		"EQUIPMENT",
		"SYSTEMS",
		"UPGRADES",
		"NAVIGATION",
		"LOG",
		"STATISTICS"
	};

	private Font _font;

	#endregion

	#region Setup And Drawing

	// =========================================================
	// Uses Godot's default font without enabling per-frame processing.
	public override void _Ready()
	{
		_font = ThemeDB.FallbackFont;

		SetProcess(false);
		SetPhysicsProcess(false);
	}

	// =========================================================
	// Draws the common frame and the currently selected inventory page.
	public override void _Draw()
	{
		if (_font == null || Cargo == null)
		{
			return;
		}

		DrawRect(new Rect2(Vector2.Zero, Size), Background);
		DrawRect(new Rect2(1, 1, 1758, 938), Outline, false, 2);

		DrawLine(new Vector2(50, 118), new Vector2(1710, 118), Outline);

		Write("SHIP COMMAND", new Vector2(54, 52), 30, Core);
		Write(
			"PERSONAL VESSEL  /  " + Tabs[ActiveTab],
			new Vector2(54, 80),
			15,
			Muted
		);

		for (int index = 0; index < Tabs.Length; index++)
		{
			Rect2 bounds = TabBounds(index);
			bool selected = index == ActiveTab;

			DrawRect(bounds, selected ? PanelLight : Panel);
			DrawRect(
				bounds,
				selected ? Accent : Outline,
				false,
				selected ? 2.0f : 1.0f
			);

			WriteCentered(
				Tabs[index],
				bounds,
				16,
				selected ? Core : Muted
			);
		}

		if (ActiveTab == 0)
		{
			DrawCargoPage();
		}
		else
		{
			Write(
				Tabs[ActiveTab],
				new Vector2(50, 185),
				24,
				Core
			);

			Write(
				"INTERFACE NOT INSTALLED",
				new Vector2(50, 235),
				18,
				Muted
			);
		}

		Write(
			"E / ESC  CLOSE     •     DRAG STACKS TO MOVE",
			new Vector2(50, 905),
			16,
			Muted
		);

		if (DragSlot >= 0)
		{
			DrawDraggedItem();
		}
	}

	// =========================================================
	// Draws cargo slots, the inspector, capacity, and available actions.
	private void DrawCargoPage()
	{
		Write("CARGO HOLD", new Vector2(50, 176), 20, Core);

		for (int index = 0; index < Cargo.SlotCount; index++)
		{
			DrawSlot(index);
		}

		DrawInspector();

		float capacity = Cargo.MaximumMass;
		float fraction = capacity > 0.0f
			? Mathf.Clamp(Cargo.UsedMass / capacity, 0.0f, 1.0f)
			: 0.0f;

		Write(
			$"CAPACITY  {Cargo.UsedMass:0.0} / {capacity:0.0}",
			new Vector2(50, 729),
			17,
			TextColor
		);

		DrawRect(new Rect2(50, 750, 792, 12), PanelLight);
		DrawRect(new Rect2(50, 750, 792 * fraction, 12), CargoColor);
		DrawRect(new Rect2(50, 750, 792, 12), Outline, false);

		DrawButton(SortBounds, "SORT", true);
		DrawButton(new Rect2(242, 805, 180, 44), "TRANSFER", false);
		DrawButton(new Rect2(434, 805, 180, 44), "DROP", false);

		Write(
			"TRANSFER / DROP REQUIRE STORAGE AND WORLD PICKUPS",
			new Vector2(890, 805),
			15,
			Muted
		);
	}

	// =========================================================
	// Draws one cargo slot with its item icon, label, and stored amount.
	private void DrawSlot(int index)
	{
		Rect2 bounds = SlotBounds(index);
		CargoSlot slot = Cargo.GetSlot(index);
		bool selected = index == SelectedSlot;

		DrawRect(bounds, selected ? PanelLight : Panel);
		DrawRect(
			bounds,
			selected ? Accent : Outline,
			false,
			selected ? 2.0f : 1.0f
		);

		if (slot.IsEmpty)
		{
			Vector2 centre = bounds.GetCenter();

			DrawLine(
				centre - new Vector2(5, 0),
				centre + new Vector2(5, 0),
				Muted
			);

			DrawLine(
				centre - new Vector2(0, 5),
				centre + new Vector2(0, 5),
				Muted
			);

			return;
		}

		float alpha = index == DragSlot ? 0.25f : 1.0f;

		string title = slot.Item.IsMiningResource
			? slot.Item.MiningResource.ToString().ToUpperInvariant()
			: slot.Item.DisplayName.ToUpperInvariant();

		WriteCentered(
			title,
			new Rect2(bounds.Position + new Vector2(4, 5), new Vector2(84, 18)),
			12,
			new Color(TextColor, alpha)
		);

		DrawItemIcon(
			slot.Item,
			bounds.Position + new Vector2(46, 49),
			24.0f,
			alpha
		);

		string amount = FormatAmount(slot.Amount);

		Vector2 textSize = _font.GetStringSize(
			amount,
			HorizontalAlignment.Left,
			-1,
			15
		);

		Write(
			amount,
			bounds.Position + new Vector2(84 - textSize.X, 81),
			15,
			new Color(Core, alpha)
		);
	}

	// =========================================================
	// Displays the selected item's identity, description, and cargo properties.
	private void DrawInspector()
	{
		Rect2 bounds = new(890, 165, 820, 550);

		DrawRect(bounds, Panel);
		DrawRect(bounds, Outline, false);

		Write("ITEM INSPECTOR", new Vector2(908, 195), 19, Core);
		DrawLine(new Vector2(908, 210), new Vector2(1692, 210), Outline);

		CargoSlot slot = Cargo.GetSlot(SelectedSlot);

		if (slot.IsEmpty)
		{
			Write(
				"NO ITEM SELECTED",
				new Vector2(908, 260),
				24,
				Muted
			);

			Write(
				"Select a stored item to inspect its properties.",
				new Vector2(908, 300),
				18,
				TextColor
			);

			return;
		}

		ItemDefinition item = slot.Item;

		DrawItemIcon(item, new Vector2(965, 280), 42.0f);

		Write(item.DisplayName, new Vector2(1030, 260), 27, Core);
		Write(
			$"{item.Layer} / {item.Type}".ToUpperInvariant(),
			new Vector2(1030, 291),
			16,
			Muted
		);

		Write("DESCRIPTION", new Vector2(908, 360), 16, Accent);
		WriteWrapped(item.Description, new Vector2(908, 394), 760.0f);

		Write("CARGO DETAILS", new Vector2(908, 510), 16, Accent);

		Write("Unit mass", new Vector2(908, 548), 18, TextColor);
		Write($"{item.UnitMass:0.##}", new Vector2(1510, 548), 18, Core);

		Write("Stack limit", new Vector2(908, 580), 18, TextColor);
		Write($"{item.StackMaximum:0.##}", new Vector2(1510, 580), 18, Core);

		Write("Stored amount", new Vector2(908, 612), 18, TextColor);
		Write($"{slot.Amount:0.##}", new Vector2(1510, 612), 18, Core);

		Write("Stack mass", new Vector2(908, 644), 18, TextColor);
		Write(
			$"{slot.Amount * item.UnitMass:0.##}",
			new Vector2(1510, 644),
			18,
			Core
		);
	}

	// =========================================================
	// Draws a floating stack preview while the player drags an item.
	private void DrawDraggedItem()
	{
		CargoSlot slot = Cargo.GetSlot(DragSlot);

		if (slot.IsEmpty)
		{
			return;
		}

		DrawCircle(DragPosition, 34.0f, PanelLight);
		DrawArc(DragPosition, 34, 0, Mathf.Tau, 40, Accent, 1.5f, true);

		DrawItemIcon(slot.Item, DragPosition, 24.0f);

		Write(
			FormatAmount(slot.Amount),
			DragPosition + new Vector2(22, 32),
			16,
			Core
		);
	}

	#endregion

	#region Hit Testing

	// =========================================================
	// Returns the cargo slot beneath a point, excluding gaps between slots.
	public int GetSlotAt(Vector2 point)
	{
		if (ActiveTab != 0)
		{
			return -1;
		}

		for (int index = 0; index < Cargo.SlotCount; index++)
		{
			if (SlotBounds(index).HasPoint(point))
			{
				return index;
			}
		}

		return -1;
	}

	// =========================================================
	// Returns the navigation tab beneath a point.
	public int GetTabAt(Vector2 point)
	{
		for (int index = 0; index < Tabs.Length; index++)
		{
			if (TabBounds(index).HasPoint(point))
			{
				return index;
			}
		}

		return -1;
	}

	// =========================================================
	// Maps a cargo index to the original eight-column slot layout.
	private Rect2 SlotBounds(int index)
	{
		return new Rect2(
			50 + index % Cargo.Columns * 100,
			195 + index / Cargo.Columns * 100,
			92,
			92
		);
	}

	// =========================================================
	// Returns one navigation tab's fixed design-space rectangle.
	private Rect2 TabBounds(int index)
	{
		return new Rect2(510 + index * 168, 82, 160, 42);
	}

	#endregion

	#region Drawing Helpers

	// =========================================================
	// Draws an assigned texture or a temporary vector mineral icon.
	private void DrawItemIcon(
		ItemDefinition item,
		Vector2 centre,
		float radius,
		float alpha = 1.0f
	)
	{
		if (item.Icon != null)
		{
			DrawTextureRect(
				item.Icon,
				new Rect2(
					centre - Vector2.One * radius,
					Vector2.One * radius * 2.0f
				),
				false,
				new Color(1, 1, 1, alpha)
			);

			return;
		}

		Vector2[] points =
		{
			centre + new Vector2(-0.8f, -0.35f) * radius,
			centre + new Vector2(-0.15f, -0.9f) * radius,
			centre + new Vector2(0.7f, -0.55f) * radius,
			centre + new Vector2(0.95f, 0.2f) * radius,
			centre + new Vector2(0.2f, 0.85f) * radius,
			centre + new Vector2(-0.7f, 0.6f) * radius
		};

		Color colour = new(item.IconColor, alpha);
		DrawColoredPolygon(points, new Color(item.IconColor.Darkened(0.65f), alpha));

		for (int index = 0; index < points.Length; index++)
		{
			DrawLine(
				points[index],
				points[(index + 1) % points.Length],
				colour,
				1.5f,
				true
			);
		}

		DrawLine(points[0], centre, colour, 1, true);
		DrawLine(points[2], centre, colour, 1, true);
		DrawLine(points[4], centre, colour, 1, true);
	}

	// =========================================================
	// Draws a vector action button with an explicit enabled appearance.
	private void DrawButton(Rect2 bounds, string title, bool enabled)
	{
		DrawRect(bounds, enabled ? PanelLight : Panel);
		DrawRect(bounds, enabled ? Outline : Muted.Darkened(0.65f), false);

		WriteCentered(
			title,
			bounds,
			17,
			enabled ? Core : Muted.Darkened(0.35f)
		);
	}

	// =========================================================
	// Draws text using a baseline position.
	private void Write(string text, Vector2 position, int size, Color colour)
	{
		DrawString(
			_font,
			position,
			text,
			HorizontalAlignment.Left,
			-1,
			size,
			colour
		);
	}

	// =========================================================
	// Centres a single line of text inside a rectangle.
	private void WriteCentered(
		string text,
		Rect2 bounds,
		int size,
		Color colour
	)
	{
		Vector2 measured = _font.GetStringSize(
			text,
			HorizontalAlignment.Left,
			-1,
			size
		);

		Write(
			text,
			new Vector2(
				bounds.Position.X + (bounds.Size.X - measured.X) * 0.5f,
				bounds.Position.Y + (bounds.Size.Y - measured.Y) * 0.5f
					+ _font.GetAscent(size)
			),
			size,
			colour
		);
	}

	// =========================================================
	// Wraps description text to the inspector's available width.
	private void WriteWrapped(string text, Vector2 position, float width)
	{
		string line = "";
		float y = position.Y;

		foreach (string word in (text ?? "").Split(' '))
		{
			string candidate = line.Length == 0 ? word : line + " " + word;

			if (line.Length > 0
				&& _font.GetStringSize(
					candidate,
					HorizontalAlignment.Left,
					-1,
					18
				).X > width)
			{
				Write(line, new Vector2(position.X, y), 18, TextColor);
				y += 25;
				line = word;
			}
			else
			{
				line = candidate;
			}
		}

		Write(line, new Vector2(position.X, y), 18, TextColor);
	}

	// =========================================================
	// Shows whole mined units while keeping sub-unit stacks distinguishable.
	private string FormatAmount(float amount)
	{
		return amount < 1.0f
			? $"×{amount:0.##}"
			: $"×{Mathf.Floor(amount):0}";
	}

	#endregion
}
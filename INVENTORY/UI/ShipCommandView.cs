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

    	// Remains unassigned until a real wallet supplies the balance.
	public long? Credits;

	public static readonly Rect2 SortBounds =
		new(50.0f, 805.0f, 180.0f, 44.0f);

        	public static readonly Rect2 DropBounds =
		new(434.0f, 805.0f, 180.0f, 44.0f);

	#endregion

	#region Appearance

	private static readonly Color Background =
		new(0.008f, 0.028f, 0.04f, 0.76f);

	private static readonly Color Panel =
		new(0.015f, 0.045f, 0.06f, 0.56f);

	private static readonly Color PanelLight =
		new(0.025f, 0.15f, 0.18f, 0.70f);
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
	// Draws a translucent command frame, navigation tabs, and the selected page.
	public override void _Draw()
	{
		if (_font == null || Cargo == null)
		{
			return;
		}

		DrawCutPanel(
			new Rect2(3, 3, 1754, 934),
			Background,
			new Color(TextColor, 0.65f),
			32.0f
		);

		DrawCutPanel(
			new Rect2(12, 12, 1736, 916),
			new Color(0, 0, 0, 0),
			new Color(Accent, 0.28f),
			28.0f
		);

		DrawCornerBrackets(
			new Rect2(37, 37, 1686, 866),
			new Color(Core, 0.65f),
			22.0f
		);

		// Small header icon and title.
		DrawCutPanel(
			new Rect2(50, 28, 44, 44),
			PanelLight,
			Accent,
			9.0f
		);

		WriteCentered("+", new Rect2(50, 28, 44, 44), 26, Core);
		Write("SHIP COMMAND", new Vector2(110, 53), 30, Core);

		Write(
			"PERSONAL VESSEL  /  " + Tabs[ActiveTab],
			new Vector2(110, 79),
			15,
			TextColor
		);

		Write("CREDITS", new Vector2(1500, 40), 13, Muted);
		Write(
			Credits.HasValue ? Credits.Value.ToString("N0") : "—",
			new Vector2(1500, 65),
			22,
			Core
		);

		for (int index = 0; index < Tabs.Length; index++)
		{
			Rect2 bounds = TabBounds(index);
			bool selected = index == ActiveTab;

			DrawCutPanel(
				bounds,
				selected ? PanelLight : Panel,
				selected ? Accent : Outline,
				8.0f,
				selected
			);

			WriteCentered(
				Tabs[index],
				bounds,
				16,
				selected ? Core : TextColor
			);
		}

		DrawLine(
			new Vector2(50, 140),
			new Vector2(1710, 140),
			new Color(Accent, 0.35f),
			1,
			true
		);

		DrawLine(
			new Vector2(50, 140),
			new Vector2(310, 140),
			Accent,
			2,
			true
		);

		if (ActiveTab == 0)
		{
			DrawCargoPage();
		}
		else
		{
			Write(Tabs[ActiveTab], new Vector2(50, 185), 24, Core);

			Write(
				"INTERFACE NOT INSTALLED",
				new Vector2(50, 235),
				18,
				Muted
			);
		}

		DrawLine(
			new Vector2(50, 875),
			new Vector2(1710, 875),
			Outline
		);

		Write(
			"E / ESC  CLOSE     •     DRAG STACKS TO MOVE",
			new Vector2(50, 905),
			16,
			TextColor
		);

		if (DragSlot >= 0)
		{
			DrawDraggedItem();
		}
	}

		// =========================================================
	// Draws cargo contents, capacity, and actions available for the selected stack.
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
			: Cargo.UsedMass > 0.0f ? 1.0f : 0.0f;

		Color capacityColour = Cargo.IsOverCapacity
			? new Color(1.0f, 0.55f, 0.15f)
			: CargoColor;

		string capacityTitle = Cargo.IsOverCapacity
			? "OVER CAPACITY"
			: "CAPACITY";

		Write(
			$"{capacityTitle}  {Cargo.UsedMass:0.0} / {capacity:0.0}",
			new Vector2(50, 729),
			17,
			capacityColour
		);

		DrawRect(new Rect2(50, 750, 792, 12), PanelLight);
		DrawRect(new Rect2(50, 750, 792 * fraction, 12), capacityColour);
		DrawRect(new Rect2(50, 750, 792, 12), Outline, false);

		bool canDrop = !Cargo.GetSlot(SelectedSlot).IsEmpty;

		DrawButton(SortBounds, "SORT", true);
		DrawButton(new Rect2(242, 805, 180, 44), "TRANSFER", false);
		DrawButton(DropBounds, "DROP", canDrop);

		Write(
			"DROP RELEASES THE SELECTED STACK INTO SPACE",
			new Vector2(890, 805),
			15,
			Muted
		);
	}

	// =========================================================
	// Draws clipped cargo cells with illuminated selection and coloured item icons.
	private void DrawSlot(int index)
	{
		Rect2 bounds = SlotBounds(index);
		CargoSlot slot = Cargo.GetSlot(index);
		bool selected = index == SelectedSlot;

		DrawCutPanel(
			bounds,
			selected ? PanelLight : Panel,
			selected ? Accent : Outline,
			7.0f,
			selected
		);

		if (selected)
		{
			DrawCornerBrackets(
				new Rect2(
					bounds.Position + new Vector2(3, 3),
					bounds.Size - new Vector2(6, 6)
				),
				Core,
				10.0f
			);
		}

		if (slot.IsEmpty)
		{
			Vector2 centre = bounds.GetCenter();
			Color emptyColour = new(Muted, 0.45f);

			DrawLine(
				centre - new Vector2(6, 0),
				centre + new Vector2(6, 0),
				emptyColour
			);

			DrawLine(
				centre - new Vector2(0, 6),
				centre + new Vector2(0, 6),
				emptyColour
			);

			return;
		}

		float alpha = index == DragSlot ? 0.25f : 1.0f;

		string title = slot.Item.IsMiningResource
			? slot.Item.MiningResource.ToString().ToUpperInvariant()
			: slot.Item.DisplayName.ToUpperInvariant();

		WriteCentered(
			title,
			new Rect2(
				bounds.Position + new Vector2(4, 5),
				new Vector2(84, 18)
			),
			12,
			new Color(TextColor, alpha)
		);

		DrawItemIcon(
			slot.Item,
			bounds.Position + new Vector2(46, 49),
			24.0f,
			alpha
		);

		// Small item-coloured identification strip.
		DrawLine(
			bounds.Position + new Vector2(10, 88),
			bounds.Position + new Vector2(34, 88),
			new Color(slot.Item.IconColor, alpha),
			2,
			true
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
	// Presents the selected item in a technical preview with cargo information.
	private void DrawInspector()
	{
		Rect2 bounds = new(890, 165, 820, 550);

		DrawCutPanel(bounds, Panel, Outline, 16.0f);

		Write("ITEM DETAILS", new Vector2(908, 195), 19, Core);
		DrawLine(new Vector2(908, 210), new Vector2(1692, 210), Outline);

		Rect2 preview = new(908, 226, 230, 198);

		DrawCutPanel(
			preview,
			new Color(0.01f, 0.04f, 0.06f, 0.35f),
			Outline,
			12.0f
		);

		DrawTechnicalGrid(new Rect2(922, 240, 202, 170));

		DrawCornerBrackets(
			new Rect2(918, 236, 210, 178),
			new Color(Accent, 0.45f),
			12.0f
		);

		CargoSlot slot = Cargo.GetSlot(SelectedSlot);

		if (slot.IsEmpty)
		{
			WriteCentered("—", preview, 40, Muted);

			Write(
				"NO ITEM SELECTED",
				new Vector2(1160, 280),
				24,
				Muted
			);

			WriteWrapped(
				"Select a stored item to inspect its properties.",
				new Vector2(1160, 320),
				510.0f
			);

			return;
		}

		ItemDefinition item = slot.Item;

		DrawItemIcon(item, preview.GetCenter(), 68.0f);

		Write(item.DisplayName, new Vector2(1160, 265), 25, Core);

		Write(
			$"{item.Layer} / {item.Type}".ToUpperInvariant(),
			new Vector2(1160, 295),
			15,
			Accent
		);

		WriteWrapped(
			item.Description,
			new Vector2(1160, 335),
			510.0f
		);

		Write("CARGO DETAILS", new Vector2(908, 470), 16, Accent);
		DrawLine(new Vector2(908, 485), new Vector2(1692, 485), Outline);

		Write("Unit mass", new Vector2(908, 520), 18, TextColor);
		Write($"{item.UnitMass:0.##}", new Vector2(1510, 520), 18, Core);

		Write("Stack limit", new Vector2(908, 552), 18, TextColor);
		Write($"{item.StackMaximum:0.##}", new Vector2(1510, 552), 18, Core);

		Write("Stored amount", new Vector2(908, 584), 18, TextColor);
		Write($"{slot.Amount:0.##}", new Vector2(1510, 584), 18, Core);

		Write("Stack mass", new Vector2(908, 616), 18, TextColor);
		Write(
			$"{slot.Amount * item.UnitMass:0.##}",
			new Vector2(1510, 616),
			18,
			Core
		);

		float fullness = Mathf.Clamp(
			slot.Amount / Mathf.Max(1.0f, item.StackMaximum),
			0.0f,
			1.0f
		);

		Write("STACK UTILIZATION", new Vector2(908, 664), 13, Muted);

		DrawRect(new Rect2(908, 678, 784, 6), PanelLight);
		DrawRect(
			new Rect2(908, 678, 784 * fullness, 6),
			item.IconColor
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
	// Draws a clipped action button with a distinct disabled appearance.
	private void DrawButton(Rect2 bounds, string title, bool enabled)
	{
		DrawCutPanel(
			bounds,
			enabled ? PanelLight : Panel,
			enabled ? Outline : new Color(Muted, 0.35f),
			8.0f
		);

		WriteCentered(
			title,
			bounds,
			17,
			enabled ? Core : Muted
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

    	#region Sci-Fi Frames

	// =========================================================
	// Draws a translucent panel with clipped corners and an optional soft outline.
	private void DrawCutPanel(
		Rect2 bounds,
		Color fill,
		Color border,
		float cut = 12.0f,
		bool highlighted = false
	)
	{
		float left = bounds.Position.X;
		float top = bounds.Position.Y;
		float right = left + bounds.Size.X;
		float bottom = top + bounds.Size.Y;

		cut = Mathf.Clamp(
			cut,
			0.0f,
			Mathf.Min(bounds.Size.X, bounds.Size.Y) * 0.5f
		);

		Vector2[] points =
		{
			new(left + cut, top),
			new(right - cut, top),
			new(right, top + cut),
			new(right, bottom - cut),
			new(right - cut, bottom),
			new(left + cut, bottom),
			new(left, bottom - cut),
			new(left, top + cut)
		};

		DrawColoredPolygon(points, fill);

		for (int index = 0; index < points.Length; index++)
		{
			Vector2 start = points[index];
			Vector2 end = points[(index + 1) % points.Length];

			if (highlighted)
			{
				DrawLine(start, end, new Color(border, 0.05f), 9, true);
				DrawLine(start, end, new Color(border, 0.12f), 4, true);
			}

			DrawLine(start, end, border, highlighted ? 1.8f : 1.0f, true);
		}
	}

	// =========================================================
	// Draws short illuminated brackets at the four corners of a rectangular area.
	private void DrawCornerBrackets(
		Rect2 bounds,
		Color colour,
		float length = 16.0f
	)
	{
		Vector2 topLeft = bounds.Position;
		Vector2 topRight = bounds.Position + new Vector2(bounds.Size.X, 0);
		Vector2 bottomLeft = bounds.Position + new Vector2(0, bounds.Size.Y);
		Vector2 bottomRight = bounds.End;

		DrawLine(topLeft, topLeft + Vector2.Right * length, colour, 2, true);
		DrawLine(topLeft, topLeft + Vector2.Down * length, colour, 2, true);

		DrawLine(topRight, topRight + Vector2.Left * length, colour, 2, true);
		DrawLine(topRight, topRight + Vector2.Down * length, colour, 2, true);

		DrawLine(bottomLeft, bottomLeft + Vector2.Right * length, colour, 2, true);
		DrawLine(bottomLeft, bottomLeft + Vector2.Up * length, colour, 2, true);

		DrawLine(bottomRight, bottomRight + Vector2.Left * length, colour, 2, true);
		DrawLine(bottomRight, bottomRight + Vector2.Up * length, colour, 2, true);
	}

	// =========================================================
	// Adds a faint technical grid within a preview rectangle.
	private void DrawTechnicalGrid(Rect2 bounds, float spacing = 28.0f)
	{
		Color colour = new(0.20f, 0.65f, 0.72f, 0.07f);

		for (float x = bounds.Position.X; x <= bounds.End.X; x += spacing)
		{
			DrawLine(
				new Vector2(x, bounds.Position.Y),
				new Vector2(x, bounds.End.Y),
				colour
			);
		}

		for (float y = bounds.Position.Y; y <= bounds.End.Y; y += spacing)
		{
			DrawLine(
				new Vector2(bounds.Position.X, y),
				new Vector2(bounds.End.X, y),
				colour
			);
		}
	}

	#endregion

	#endregion
}
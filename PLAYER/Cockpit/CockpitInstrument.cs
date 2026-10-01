using Godot;

// Draws cockpit flight or defence instruments using existing player stats.
public partial class CockpitInstrument : Control
{
	#region Settings

	public PlayerShip Ship;
	public bool ShowDefence;

	public Color AccentColor = new(0.20f, 0.90f, 1.0f);

	#endregion

	#region Drawing

	// =========================================================
	// Draws instruments within a fixed design area that scales with the panel.
	public override void _Draw()
	{
		if (!GodotObject.IsInstanceValid(Ship))
		{
			return;
		}

		DrawSetTransform(
			Vector2.Zero,
			0.0f,
			new Vector2(Size.X / 616.0f, Size.Y / 230.0f)
		);

		Color faint = new(
			AccentColor.R, AccentColor.G, AccentColor.B, 0.16f
		);

		// Fine background grid.
		for (int x = 0; x <= 616; x += 28)
		{
			DrawLine(
				new Vector2(x, 0),
				new Vector2(x, 230),
				faint, 1.0f
			);
		}

		for (int y = 0; y <= 230; y += 28)
		{
			DrawLine(
				new Vector2(0, y),
				new Vector2(616, y),
				faint, 1.0f
			);
		}

		DrawCorners();

		if (ShowDefence)
		{
			DrawDefence();
		}
		else
		{
			DrawFlight();
		}

		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
	}

	// =========================================================
	// Draws three segmented defence bars with current percentages.
	private void DrawDefence()
	{
		ShipDefence defence = Ship.Defence;

		if (defence == null)
		{
			DrawText("DEFENCE OFFLINE", new Vector2(20, 45), 26);
			return;
		}

		DrawDefenceRow(
			"SHIELD", defence.Shield, defence.MaxShield, 20.0f
		);

		DrawDefenceRow(
			"ARMOUR", defence.Armour, defence.MaxArmour, 67.0f
		);

		DrawDefenceRow(
			"HULL", defence.Hull, defence.MaxHull, 114.0f
		);

		DrawShip(new Vector2(308, 195), 0.34f);

		DrawText("INTEGRITY", new Vector2(20, 210), 17);
		DrawText(
			defence.Destroyed ? "OFFLINE" : "ONLINE",
			new Vector2(490, 210),
			17
		);
	}

	// =========================================================
	// Draws one defence layer and its proportion of maximum capacity.
	private void DrawDefenceRow(
		string title,
		float current,
		float maximum,
		float y
	)
	{
		float fraction = maximum > 0.0f
			? Mathf.Clamp(current / maximum, 0.0f, 1.0f)
			: 0.0f;

		DrawText(title, new Vector2(18, y + 20), 21);

		DrawSegments(
			new Rect2(155, y + 4, 340, 17),
			fraction,
			30
		);

		DrawText(
			maximum > 0.0f ? $"{fraction * 100.0f:0}%" : "—",
			new Vector2(515, y + 20),
			21
		);

		DrawText(
			$"{current:0} / {maximum:0}",
			new Vector2(155, y + 39),
			15,
			0.55f
		);
	}

	// =========================================================
	// Draws travel speed, current boost strength, and flight control mode.
	private void DrawFlight()
	{
		DrawText("VELOCITY", new Vector2(20, 33), 20);

		DrawText(
			$"{Ship.Velocity.Length():0.0}",
			new Vector2(16, 104),
			64
		);

		DrawText("BOOST THRUST", new Vector2(20, 143), 18);

		DrawSegments(
			new Rect2(20, 156, 290, 16),
			Ship.BoostAmount,
			24
		);

		DrawText(
			$"{Ship.BoostAmount * 100.0f:0}%",
			new Vector2(325, 172),
			23
		);

		DrawShip(new Vector2(485, 105), 0.85f);

		DrawText(
			Ship.CockpitInteractionActive
				? "COCKPIT CONTROL"
				: Ship.IsDashing
					? "DASH ACTIVE"
					: Ship.IsBoosting
						? "BOOST ACTIVE"
						: "FLIGHT CONTROL",
			new Vector2(20, 214),
			20
		);
	}

	// =========================================================
	// Draws capacity segments, including partially filled segments.
	private void DrawSegments(Rect2 area, float fraction, int count)
	{
		fraction = Mathf.Clamp(fraction, 0.0f, 1.0f);

		float step = area.Size.X / count;
		float width = step - 3.0f;

		Color dim = new(
			AccentColor.R, AccentColor.G, AccentColor.B, 0.15f
		);

		for (int index = 0; index < count; index++)
		{
			Vector2 position = area.Position
				+ new Vector2(index * step, 0.0f);

			DrawRect(
				new Rect2(position, new Vector2(width, area.Size.Y)),
				dim
			);

			float fill = Mathf.Clamp(
				fraction * count - index,
				0.0f,
				1.0f
			);

			if (fill > 0.0f)
			{
				DrawRect(
					new Rect2(
						position,
						new Vector2(width * fill, area.Size.Y)
					),
					AccentColor
				);
			}
		}
	}

	// =========================================================
	// Draws a decorative ship schematic without implying radar or targeting.
	private void DrawShip(Vector2 centre, float scale)
	{
		Vector2[] outline =
		{
			new(0, -68),
			new(18, -18),
			new(75, 28),
			new(68, 45),
			new(20, 24),
			new(12, 58),
			new(-12, 58),
			new(-20, 24),
			new(-68, 45),
			new(-75, 28),
			new(-18, -18),
			new(0, -68)
		};

		for (int index = 0; index < outline.Length; index++)
		{
			outline[index] = centre + outline[index] * scale;
		}

		DrawPolyline(outline, AccentColor, 1.5f, true);

		DrawLine(
			centre + new Vector2(0, -68) * scale,
			centre + new Vector2(0, 58) * scale,
			AccentColor, 1.0f, true
		);

		DrawLine(
			centre + new Vector2(-68, 45) * scale,
			centre + new Vector2(0, -18) * scale,
			AccentColor, 1.0f, true
		);

		DrawLine(
			centre + new Vector2(68, 45) * scale,
			centre + new Vector2(0, -18) * scale,
			AccentColor, 1.0f, true
		);
	}

	// =========================================================
	// Draws short corner brackets around the instrument area.
	private void DrawCorners()
	{
		for (int corner = 0; corner < 4; corner++)
		{
			float x = (corner & 1) == 0 ? 0.0f : 616.0f;
			float y = (corner & 2) == 0 ? 0.0f : 230.0f;

			float directionX = x == 0.0f ? 1.0f : -1.0f;
			float directionY = y == 0.0f ? 1.0f : -1.0f;

			DrawLine(
				new Vector2(x, y),
				new Vector2(x + directionX * 18.0f, y),
				AccentColor, 2.0f
			);

			DrawLine(
				new Vector2(x, y),
				new Vector2(x, y + directionY * 12.0f),
				AccentColor, 2.0f
			);
		}
	}

	// =========================================================
	// Draws consistent vector-style text with optional reduced opacity.
	private void DrawText(
		string text,
		Vector2 position,
		int fontSize,
		float opacity = 1.0f
	)
	{
		Color colour = AccentColor;
		colour.A = opacity;

		DrawString(
			ThemeDB.FallbackFont,
			position,
			text,
			HorizontalAlignment.Left,
			-1,
			fontSize,
			colour
		);
	}

	#endregion
}
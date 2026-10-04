using Godot;

// Draws a circular flight reticle whose outer brackets expand with weapon spread.
public partial class PlayerCrosshair : Control
{
	#region References

	[Export] public WeaponMount PrimaryWeapon;

	#endregion

	#region Appearance

	[Export] public Color CrosshairColor =
		new(0.65f, 0.95f, 1.0f, 0.9f);

	[Export] public float MinimumGap = 42.0f;
	[Export] public float MarkLength = 12.0f;
	[Export] public float LineWidth = 1.5f;

	[Export] public float CentreRingRadius = 12.0f;
	[Export] public float BracketHalfAngleDegrees = 30.0f;
	[Export] public float OuterArcSpacing = 4.0f;

	#endregion

	#region Runtime

	private float _gap;

	#endregion

	#region Setup

	// =========================================================
	// Covers the viewport without intercepting mouse input.
	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	#endregion

	#region Presentation

	// =========================================================
	// Projects actual weapon spread and adds it to the decorative bracket radius.
	public override void _Process(double delta)
	{
		Visible = Input.MouseMode == Input.MouseModeEnum.Captured
			&& GodotObject.IsInstanceValid(PrimaryWeapon);

		if (!Visible)
			return;

		_gap = Mathf.Max(
			MinimumGap, CentreRingRadius + MarkLength + 6.0f
		);

		Camera3D camera = GetViewport().GetCamera3D();

		if (GodotObject.IsInstanceValid(camera))
		{
			Vector2 center = GetViewport().GetVisibleRect().Size * 0.5f;

			Vector3 origin = camera.ProjectRayOrigin(center);
			Vector3 forward = camera.ProjectRayNormal(center);
			Vector3 right = camera.GlobalBasis.X.Normalized();

			float spread = Mathf.DegToRad(Mathf.Clamp(
				PrimaryWeapon.CurrentSpreadDegrees, 0.0f, 89.0f
			));

			Vector3 edgePoint = origin
				+ forward * 100.0f
				+ right * Mathf.Tan(spread) * 100.0f;

			float spreadPixels = camera.UnprojectPosition(
				edgePoint
			).DistanceTo(center);

			// Adding spread preserves visible expansion even with a larger reticle.
			_gap += spreadPixels;
		}

		QueueRedraw();
	}

	// =========================================================
	// Draws a fixed aiming ring surrounded by expanding curved brackets.
	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;
		float halfAngle = Mathf.DegToRad(Mathf.Clamp(
			BracketHalfAngleDegrees, 5.0f, 80.0f
		));

		DrawOutlinedArc(
			center, CentreRingRadius, 0.0f, Mathf.Tau,
			CrosshairColor, LineWidth, 48
		);

		DrawCircle(center, 1.0f, CrosshairColor);

		DrawBracket(center, 0.0f, halfAngle);
		DrawBracket(center, Mathf.Pi, halfAngle);

		DrawMark(center, Vector2.Left);
		DrawMark(center, Vector2.Right);
		DrawMark(center, Vector2.Up);
		DrawMark(center, Vector2.Down);
	}

	// =========================================================
	// Draws paired curved brackets on either side of the aiming point.
	private void DrawBracket(
		Vector2 center, float angle, float halfAngle
	)
	{
		DrawOutlinedArc(
			center, _gap, angle - halfAngle, angle + halfAngle,
			CrosshairColor, LineWidth, 24
		);

		Color faint = CrosshairColor;
		faint.A *= 0.4f;

		DrawOutlinedArc(
			center, _gap + Mathf.Max(0.0f, OuterArcSpacing),
			angle - halfAngle * 0.8f, angle + halfAngle * 0.8f,
			faint, 1.0f, 24
		);
	}

	// =========================================================
	// Draws short reference ticks between the centre ring and outer brackets.
	private void DrawMark(Vector2 center, Vector2 direction)
	{
		float startRadius = CentreRingRadius + 8.0f;
		float endRadius = Mathf.Min(
			startRadius + MarkLength, _gap - 6.0f
		);

		if (endRadius <= startRadius)
			return;

		Vector2 start = center + direction * startRadius;
		Vector2 end = center + direction * endRadius;

		DrawLine(start, end,
			new Color(0.0f, 0.0f, 0.0f, 0.5f),
			LineWidth + 1.5f, true);

		DrawLine(start, end, CrosshairColor, LineWidth, true);
	}

	// =========================================================
	// Adds a subtle dark outline for readability against bright scenery.
	private void DrawOutlinedArc(
		Vector2 center, float radius, float start, float end,
		Color color, float width, int points
	)
	{
		DrawArc(center, radius, start, end, points,
			new Color(0.0f, 0.0f, 0.0f, color.A * 0.55f),
			width + 1.5f, true);

		DrawArc(center, radius, start, end, points,
			color, width, true);
	}

	#endregion
}
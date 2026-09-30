using Godot;

// Draws a screen-centred crosshair using the primary weapon's actual spread.
public partial class PlayerCrosshair : Control
{
	#region References

	[Export] public WeaponMount PrimaryWeapon;

	#endregion

	#region Appearance

	[Export] public Color CrosshairColor =
		new Color(0.65f, 0.95f, 1.0f, 0.9f);

	[Export] public float MinimumGap = 5.0f;
	[Export] public float MarkLength = 7.0f;
	[Export] public float LineWidth = 1.5f;

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
	// Projects the spread cone into screen pixels and updates the crosshair.
	public override void _Process(double delta)
	{
		Visible =
			Input.MouseMode == Input.MouseModeEnum.Captured
			&& GodotObject.IsInstanceValid(PrimaryWeapon);

		if (!Visible)
		{
			return;
		}

		_gap = Mathf.Max(0.0f, MinimumGap);

		Camera3D camera = GetViewport().GetCamera3D();

		if (GodotObject.IsInstanceValid(camera))
		{
			Vector2 center =
				GetViewport().GetVisibleRect().Size * 0.5f;

			Vector3 origin = camera.ProjectRayOrigin(center);
			Vector3 forward = camera.ProjectRayNormal(center);
			Vector3 right = camera.GlobalBasis.X.Normalized();

			float spread = Mathf.DegToRad(
				Mathf.Clamp(
					PrimaryWeapon.CurrentSpreadDegrees,
					0.0f,
					89.0f
				)
			);

			Vector3 edgePoint =
				origin
				+ forward * 100.0f
				+ right * Mathf.Tan(spread) * 100.0f;

			float spreadPixels = camera.UnprojectPosition(
				edgePoint
			).DistanceTo(center);

			_gap = Mathf.Max(_gap, spreadPixels);
		}

		QueueRedraw();
	}

	// =========================================================
	// Draws four thin marks around the aiming point.
	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;

		DrawMark(center, Vector2.Left);
		DrawMark(center, Vector2.Right);
		DrawMark(center, Vector2.Up);
		DrawMark(center, Vector2.Down);

		DrawCircle(center, 1.0f, CrosshairColor);
	}

	// =========================================================
	// Draws one outlined mark so it remains visible against bright objects.
	private void DrawMark(Vector2 center, Vector2 direction)
	{
		Vector2 start = center + direction * _gap;
		Vector2 end = start + direction * MarkLength;

		DrawLine(
			start,
			end,
			new Color(0.0f, 0.0f, 0.0f, 0.65f),
			LineWidth + 2.0f,
			true
		);

		DrawLine(
			start,
			end,
			CrosshairColor,
			LineWidth,
			true
		);
	}

	#endregion
}
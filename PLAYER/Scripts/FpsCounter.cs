using Godot;

// Displays the current FPS in a HUD label.
public partial class FpsCounter : Label
{
	// =========================================================
	// Updates the text and warning colour every frame.
	public override void _Process(double delta)
	{
		int fps = Engine.GetFramesPerSecond();

		Text = $"FPS: {fps}";

		Modulate = fps < 60
			? Colors.Red
			: Colors.White;
	}
}

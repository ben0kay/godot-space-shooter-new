using Godot;

// Displays the current FPS in a HUD label.
public partial class FpsCounter : Label
{
	// =========================================================
	// Updates the FPS text and turns it red below 60 FPS.
	// =========================================================
	public override void _Process(double delta)
	{
		double fps = Engine.GetFramesPerSecond();

		Text = $"FPS: {fps:0}";

		Modulate = fps < 60.0
			? Colors.Red
			: Colors.White;
	}
}

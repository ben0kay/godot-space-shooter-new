using Godot;

// Stores shield fitting, subtle idle appearance, and temporary hit effects.
[GlobalClass]
public partial class ShieldVisualSettings : Resource
{
	#region Shape

	[Export] public bool Spherical = false;

	[Export] public float SizeMultiplier = 1.06f;
	[Export] public float Padding = 0.25f;

	#endregion

	#region Appearance

	[Export] public bool UseFactionPalette = true;
	[Export] public Color Color = new Color(0.15f, 0.85f, 1.0f);

	[Export] public float IdleOpacity = 0.001f;
	[Export] public float RimOpacity = 0.12f;
	[Export] public float Brightness = 1.1f;

	#endregion

	#region Hit And Collapse

	[Export] public float HitDuration = 0.5f;
	[Export] public float CollapseDuration = 0.3f;

	#endregion
}
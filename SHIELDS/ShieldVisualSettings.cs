using Godot;

// Stores the appearance and enclosing size of a ship's shield.
[GlobalClass]
public partial class ShieldVisualSettings : Resource
{
	#region Shape

	// Spherical shields accommodate cosmetic banking especially well.
	[Export] public bool Spherical = false;

	[Export] public float SizeMultiplier = 1.08f;
	[Export] public float Padding = 0.35f;

	#endregion

	#region Appearance

	[Export] public bool UseFactionPalette = true;
	[Export] public Color Color = new Color(0.15f, 0.85f, 1.0f);

	[Export] public float IdleOpacity = 0.035f;
	[Export] public float RimOpacity = 0.3f;
	[Export] public float Brightness = 1.4f;

	#endregion

	#region Hit And Collapse

	[Export] public float HitDuration = 0.45f;
	[Export] public float CollapseDuration = 0.3f;

	#endregion
}
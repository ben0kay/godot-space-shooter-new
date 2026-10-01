using Godot;

// Configures a shared explosion flash and shockwave independently of damage.
[GlobalClass]
public partial class ExplosionVisualSettings : Resource
{
	#region Colour

	[Export] public bool UseFactionPalette = false;
	[Export] public Color Color = new Color(1.0f, 0.55f, 0.15f);
	[Export] public float Brightness = 3.0f;

	#endregion

	#region Shockwave

	[Export] public float Duration = 0.4f;

	// Multiplies the explosion damage radius for presentation only.
	[Export] public float RadiusScale = 1.0f;

	[Export] public float StartRadiusFraction = 0.08f;
	[Export] public float Opacity = 0.45f;

	// Higher values produce a thinner visible rim.
	[Export] public float RimPower = 7.0f;

	[Export] public float Distortion = 0.025f;

	#endregion

	#region Flash

	[Export] public float FlashDuration = 0.09f;
	[Export] public float FlashRadiusFraction = 0.12f;
	[Export] public float FlashBrightness = 5.0f;

	#endregion
}

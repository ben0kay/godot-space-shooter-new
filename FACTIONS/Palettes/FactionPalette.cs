using Godot;

[Tool]
[GlobalClass]
public partial class FactionPalette : Resource
{
	#region Hull

	[Export] public Color HullDark = new("#202020");
	[Export] public Color HullMid = new("#404040");
	[Export] public Color HullLight = new("#707070");
	[Export] public Color Metal = new("#a0a0a0");

	#endregion

	#region Energy

	[Export] public Color Accent = new("#909090");
	[Export] public Color Energy = new("#cccccc");
	[Export] public Color Core = new("#ffffff");
	[Export] public Color Glow = new("#808080");

	#endregion

	#region Effects

	[Export] public Color Thruster = new("#cccccc");
	[Export] public Color Particle = new("#cccccc");

	#endregion
}

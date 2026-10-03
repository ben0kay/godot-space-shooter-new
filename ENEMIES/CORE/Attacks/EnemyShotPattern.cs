using Godot;

// Defines reusable projectile formations emitted by enemy attacks.
[GlobalClass]
public partial class EnemyShotPattern : Resource
{
	#region Pattern

	public enum PatternType { Single, ExpandingRing }

	[Export] public PatternType Type = PatternType.Single;
	[Export(PropertyHint.Range, "1,64,1")] public int Amount = 12;

	#endregion

	#region Ring Settings

	// Radius of the ring when launched, in world units.
	[Export] public float StartingRadius = 2.5f;

	// Outward angle added to each orb's forward direction.
	[Export(PropertyHint.Range, "0,45,0.1")]
	public float ExpansionDegrees = 4.0f;

	// Moves the launch centre forward from the muzzle to clear the hull.
	[Export] public float ForwardOffset = 12.0f;

	// Rotates the positions of the orbs around the ring.
	[Export] public float RotationDegrees = 0.0f;

	#endregion
}
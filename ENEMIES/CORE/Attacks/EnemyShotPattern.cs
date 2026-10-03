using Godot;

// Defines reusable projectile formations for enemy attacks.
// Sequential rings emit one position per volley round.
[GlobalClass]
public partial class EnemyShotPattern : Resource
{
	#region Pattern

	public enum PatternType { Single, ExpandingRing, SequentialRing }

	[Export] public PatternType Type = PatternType.Single;
	[Export(PropertyHint.Range, "1,64,1")] public int Amount = 12;

	#endregion

	#region Ring Settings

	// Initial ring radius, in world units.
	[Export] public float StartingRadius = 2.5f;

	// Outward angle that makes the formation expand during travel.
	[Export(PropertyHint.Range, "0,45,0.1")]
	public float ExpansionDegrees = 4.0f;

	// Distance ahead of the muzzle where projectiles begin.
	[Export] public float ForwardOffset = 4.0f;

	// Starting angle around the ring.
	[Export] public float RotationDegrees = 0.0f;

	#endregion
}
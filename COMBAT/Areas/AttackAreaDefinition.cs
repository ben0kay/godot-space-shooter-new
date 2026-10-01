using Godot;

// Defines one spherical damage burst, independently of its visual effects.
[GlobalClass]
public partial class AttackAreaDefinition : Resource
{
	#region Geometry

	[Export] public float Radius = 8.0f;

	#endregion

	#region Falloff

	// Damage multiplier at the outer edge. One disables distance falloff.
	[Export] public float FalloffMinimum = 0.25f;

	// One gives linear falloff; larger values preserve more damage near the centre.
	[Export] public float FalloffExponent = 1.0f;

	#endregion

	#region Targeting

	[Export] public bool DamageSource = false;
	[Export] public bool FriendlyFire = false;

	// Includes asteroids and other damageable objects without faction information.
	[Export] public bool DamageEnvironment = true;

	// Layer 1 contains the current world bodies; layer 5 contains ship shields.
	[Export(PropertyHint.Layers3DPhysics)]
	public uint TargetMask = 1u | ShipShield.ShieldLayer;

	#endregion

	#region Cover

	[Export] public bool CheckCover = true;

	// Zero fully blocks damage; a fraction allows reduced damage through cover.
	[Export] public float BlockedDamageMultiplier = 0.0f;

	// Current solid bodies are on layer 1. Shields do not block this cover ray.
	[Export(PropertyHint.Layers3DPhysics)]
	public uint CoverMask = 1u;

	#endregion
}
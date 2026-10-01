using Godot;

// Groups optional weapon overrides without modifying the shared projectile resource.
[GlobalClass]
public partial class ProjectileLaunchOverrides : Resource
{
	#region Damage

	[Export] public bool OverrideDamage = false;
	[Export] public float Damage = 3.0f;

	#endregion

	#region Speed

	[Export] public bool OverrideSpeed = false;
	[Export] public float Speed = 90.0f;

	#endregion

	#region Lifetime

	[Export] public bool OverrideLifetime = false;
	[Export] public float Lifetime = 3.0f;

	#endregion

	#region Size

	[Export] public float SizeScale = 1.0f;

	#endregion
	
		#region Guidance

	// Replaces the projectile's guidance settings for this weapon.
	[Export] public bool OverrideGuidance = false;

	// With OverrideGuidance enabled, null explicitly disables guidance.
	[Export] public ProjectileGuidanceSettings Guidance;

	#endregion

		#region Detonation

	// Enabling this with a null resource disables the projectile's explosion.
	[Export] public bool OverrideDetonation = false;
	[Export] public ProjectileDetonationSettings Detonation;

	[Export] public bool OverrideExplosionDamage = false;
	[Export] public float ExplosionDamage = 20.0f;

	// Multiplies the detonation's area scale independently of projectile size.
	[Export] public float ExplosionScale = 1.0f;

	#endregion
}

using Godot;

// Selects a projectile and stores weapon firing settings, launch overrides, and muzzle effects.
[GlobalClass]
public partial class WeaponDefinition : Resource
{
	#region Firing

	[Export] public float SecondsBetweenShots = 0.09f;

	#endregion

	#region Projectile Delivery

	[Export] public ProjectileDefinition Projectile;
	[Export] public ProjectileLaunchOverrides LaunchOverrides;

	// Exposes the selected projectile scene to the shared firing controllers.
	public PackedScene ProjectileScene => Projectile?.Scene;

	#endregion

	#region Muzzle Effects

	[Export] public WeaponEffectsProfile MuzzleEffects;

	#endregion
}

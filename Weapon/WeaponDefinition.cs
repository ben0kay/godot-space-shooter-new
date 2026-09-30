using Godot;

// Stores shared weapon settings for projectiles, damage, and optional effects.
[GlobalClass]
public partial class WeaponDefinition : Resource
{
	#region Firing

	[Export] public float SecondsBetweenShots = 0.09f;
	[Export] public PackedScene ProjectileScene;

	#endregion

	#region Projectile

	[Export] public float Damage = 3.0f;
	[Export] public float ProjectileSpeed = 90.0f;
	[Export] public float ProjectileLifetime = 3.0f;
	[Export] public float ProjectileRadius = 0.12f;
	[Export] public Color ProjectileColor = Colors.Orange;

	#endregion

	#region Effects

	[Export] public WeaponEffectsProfile Effects;

	#endregion
}

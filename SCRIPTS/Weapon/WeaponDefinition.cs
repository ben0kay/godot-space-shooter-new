using Godot;

[GlobalClass]
public partial class WeaponDefinition : Resource
{
	#region Firing

	[Export] public float SecondsBetweenShots = 0.09f;
	[Export] public PackedScene ProjectileScene;

	#endregion

	#region Projectile

	[Export] public float ProjectileSpeed = 90.0f;
	[Export] public float ProjectileLifetime = 3.0f;
	[Export] public float ProjectileRadius = 0.12f;
	[Export] public Color ProjectileColor = Colors.Orange;

	#endregion
}

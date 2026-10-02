using Godot;

// Selects a weapon's delivery, shared definitions, overrides, and optional mining.
[GlobalClass]
public partial class WeaponDefinition : Resource
{
	#region Delivery Types

	public enum DeliveryType
	{
		Projectile,
		Beam
	}

	#endregion

	#region Firing

	[Export] public DeliveryType Delivery = DeliveryType.Projectile;
	[Export] public float SecondsBetweenShots = 0.09f;

	#endregion

	#region Projectile Delivery

	[Export] public ProjectileDefinition Projectile;
	[Export] public ProjectileLaunchOverrides LaunchOverrides;

	public PackedScene ProjectileScene => Projectile?.Scene;

	#endregion

	#region Beam Delivery

	[Export] public BeamDefinition Beam;
	[Export] public BeamLaunchOverrides BeamOverrides;

	#endregion

	#region Mining

	// Null means this weapon uses normal combat behaviour.
	[Export] public MiningSettings Mining;

	#endregion

	#region Muzzle Effects

	[Export] public WeaponEffectsProfile MuzzleEffects;

	#endregion

	#region Accuracy

	// Null means no random projectile spread.
	[Export] public WeaponAccuracySettings Accuracy;

	#endregion
}
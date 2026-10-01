using Godot;

// Defines a reusable projectile's default launch values, appearance, and effects.
[GlobalClass]
public partial class ProjectileDefinition : Resource
{
	#region Runtime Scene

	[Export] public PackedScene Scene;

	#endregion

	#region Default Launch Stats

	[Export] public float Damage = 3.0f;
	[Export] public float Speed = 90.0f;
	[Export] public float Lifetime = 3.0f;

	#endregion

	#region Collision

	[Export] public float CollisionRadius = 0.12f;

	#endregion

	#region Appearance

	[Export] public Color Color = Colors.Orange;
	[Export] public float EmissionEnergy = 1.5f;

	// Zero creates a sphere; a positive length creates a narrow tracer.
	[Export] public float VisualLength = 0.0f;
	[Export] public float VisualWidth = 0.035f;

	#endregion

	#region Effects

	[Export] public WeaponEffectsProfile Effects;

	#endregion
	
		#region Guidance

	// Null means this projectile flies without guidance.
	[Export] public ProjectileGuidanceSettings Guidance;

	#endregion

		#region Detonation

	// Null means this projectile has no explosion.
	[Export] public ProjectileDetonationSettings Detonation;

	#endregion
}

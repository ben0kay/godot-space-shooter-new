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

		#region Model Visual

	// Optional imported model or visual scene; null uses the procedural visual.
	[Export] public PackedScene VisualScene;

	[Export] public float ModelScale = 1.0f;
	[Export] public Vector3 ModelOffset = Vector3.Zero;
	[Export] public Vector3 ModelRotationDegrees = Vector3.Zero;

	// Axis is relative to the projectile: negative Z is its forward direction.
	[Export] public Vector3 VisualSpinAxis = Vector3.Forward;
	[Export] public float VisualSpinDegreesPerSecond = 0.0f;

	#endregion

	#region Damage Classification

	[Export] public DamageType DamageType = global::DamageType.Neutral;

	#endregion
}

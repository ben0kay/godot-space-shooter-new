using Godot;

// Defines a projectile's optional explosion independently of direct-impact damage.
[GlobalClass]
public partial class ProjectileDetonationSettings : Resource
{
	#region Area And Damage

	[Export] public AttackAreaDefinition Area;
	[Export] public float Damage = 20.0f;
	[Export] public float AreaScale = 1.0f;

	#endregion

	#region Triggers

	[Export] public bool OnImpact = true;
	[Export] public bool OnLifetimeEnd = false;

	#endregion
}
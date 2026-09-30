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
}

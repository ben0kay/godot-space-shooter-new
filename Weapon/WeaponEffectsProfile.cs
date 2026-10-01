using Godot;

[GlobalClass]
public partial class WeaponEffectsProfile : Resource
{
	#region Appearance

	[ExportGroup("Appearance")]
	[Export] public bool UseFactionPalette = true;
	[Export] public Color Color = Colors.White;
	[Export] public float EmissionEnergy = 3.0f;
	[Export] public float ParticleRadius = 0.045f;

	#endregion

	#region Muzzle

	[ExportGroup("Muzzle")]
	[Export] public int MuzzleParticles = 8;
	[Export] public float MuzzleLifetime = 0.12f;
	[Export] public float MuzzleSpeed = 3.0f;

	#endregion

	#region Trail

	[ExportGroup("Trail")]
	[Export] public int TrailParticles = 12;
	[Export] public float TrailLifetime = 0.2f;

	#endregion

	#region Impact

	[ExportGroup("Impact")]
	[Export] public int ImpactParticles = 12;
	[Export] public float ImpactLifetime = 0.25f;
	[Export] public float ImpactSpeed = 5.0f;

	#endregion

		#region Glow Trail

	// Uses soft shader sprites instead of the normal sphere particles.
	[Export] public bool UseGlowTrail = false;

	[Export] public float GlowTrailWidth = 0.8f;
	[Export] public Vector3 TrailOffset = Vector3.Zero;

	// Bounds must encompass the distance travelled during the particle lifetime.
	[Export] public float TrailBoundsRadius = 32.0f;

	#endregion
}

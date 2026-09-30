using Godot;

// Carries damage ownership and optional world-space surface contact information.
public readonly struct DamageInfo
{
	public readonly float Amount;
	public readonly CollisionObject3D Source;
	public readonly Faction SourceFaction;

	public readonly bool HasImpact;
	public readonly Vector3 ImpactPosition;
	public readonly Vector3 ImpactNormal;

	// =========================================================
	// Records damage and optional contact information without requiring every caller to provide it.
	public DamageInfo(
		float amount,
		CollisionObject3D source,
		Faction sourceFaction,
		Vector3? impactPosition = null,
		Vector3? impactNormal = null
	)
	{
		Amount = amount;
		Source = source;
		SourceFaction = sourceFaction;

		HasImpact = impactPosition.HasValue;
		ImpactPosition = impactPosition ?? Vector3.Zero;
		ImpactNormal = impactNormal ?? Vector3.Up;
	}
}

// Implemented by ships, asteroids, and structures that can receive damage.
public interface IDamageable
{
	void ApplyDamage(DamageInfo damage);
}

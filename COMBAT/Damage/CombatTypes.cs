using Godot;

// Carries damage amount, type, ownership, and optional surface contact information.
public readonly struct DamageInfo
{
	public readonly float Amount;
	public readonly DamageType Type;

	public readonly CollisionObject3D Source;
	public readonly Faction SourceFaction;

	public readonly bool HasImpact;
	public readonly Vector3 ImpactPosition;
	public readonly Vector3 ImpactNormal;

	// =========================================================
	// Records damage while allowing older callers to default to neutral damage.
	public DamageInfo(
		float amount,
		CollisionObject3D source,
		Faction sourceFaction,
		Vector3? impactPosition = null,
		Vector3? impactNormal = null,
		DamageType type = DamageType.Neutral
	)
	{
		Amount = amount;
		Type = type;

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

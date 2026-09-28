using Godot;

public readonly struct DamageInfo
{
	public readonly float Amount;
	public readonly CollisionObject3D Source;
	public readonly Faction SourceFaction;

	// Records who caused a hit and how much damage it carries.
	public DamageInfo(float amount, CollisionObject3D source, Faction sourceFaction)
	{
		Amount = amount;
		Source = source;
		SourceFaction = sourceFaction;
	}
}

// Any ship, asteroid, or structure that receives damage implements this.
public interface IDamageable
{
	void ApplyDamage(DamageInfo damage);
}

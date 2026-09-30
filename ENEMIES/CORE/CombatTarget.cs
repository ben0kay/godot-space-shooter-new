public interface ICombatTarget
{
	Faction CombatFaction { get; }
	bool IsCombatTargetable { get; }
}

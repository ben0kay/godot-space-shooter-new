public static class FactionHostility
{
	public static float Get(Faction observer, Faction target)
	{
		if (observer == target)
		{
			return 0.0f;
		}

		return (observer, target) switch
		{
			(Faction.Simulant, Faction.Player) => 1.0f,
			(Faction.Simulant, Faction.Rebel) => 1.0f,
			(Faction.Simulant, Faction.Corporation) => 1.0f,

			(Faction.Rebel, Faction.Player) => 1.0f,
			(Faction.Rebel, Faction.Simulant) => 1.0f,
			(Faction.Rebel, Faction.Corporation) => 0.8f,

			(Faction.Corporation, Faction.Player) => 1.0f,
			(Faction.Corporation, Faction.Simulant) => 1.0f,
			(Faction.Corporation, Faction.Rebel) => 0.8f,

			_ => 0.0f
		};
	}
}

// Identifies how damage interacts with shield, armour, and hull.
// Explicit values keep saved resources stable if more types are added later.
public enum DamageType
{
	Neutral = 0,
	Kinetic = 1,
	Energy = 2,
	Explosive = 3,
	Electric = 4,
	Thermal = 5,
	Corrosive = 6
}
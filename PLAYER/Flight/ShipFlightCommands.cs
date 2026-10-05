using Godot;

// Carries flight requests without knowing which controller produced them.
public struct ShipFlightCommands
{
	public bool Active;
	public Vector2 MouseMotion;
	public float Thrust;
	public float Strafe;
	public float Rise;
	public float Roll;
	public bool Boost;
}
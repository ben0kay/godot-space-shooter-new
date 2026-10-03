// Identifies upgradeable player stats independently of Inspector grouping.
public enum PlayerStat
{
	MaxShield, MaxArmour, MaxHull,
	ForwardSpeed, ReverseSpeed, StrafeSpeed, VerticalSpeed,
	Acceleration, Deceleration, RollSpeed,
	MousePitchSensitivity, MouseYawSensitivity,
	MaxPitchSpeedDegrees, MaxYawSpeedDegrees, SteeringResponse,
	BoostSpeedMultiplier, BoostAccelerationMultiplier, BoostResponse,
	DashSpeed, DashDuration, DashExitMultiplier, DashDoubleTapWindow, DashCooldown,
	CargoMaximumMass,

	MaximumFuel, ThrustFuelPerSecond, BoostFuelPerSecond,
	DashFuelCost, JumpFuelCost,
	MaximumEnergy, EnergyRegenerationPerSecond, EnergyRechargeDelaySeconds,

	Count
}
using System;
using System.Collections.Generic;

// Calculates independent player stats from base values and removable modifier sources.
public sealed class PlayerRuntimeStats
{
	#region State

	public event Action Changed;

	private readonly float[] _base = new float[(int)PlayerStat.Count];
	private readonly float[] _final = new float[(int)PlayerStat.Count];

	private readonly SortedDictionary<string, StatModifier[]> _sources =
		new(StringComparer.Ordinal);

	#endregion

	#region Construction

	// =========================================================
// Copies authored values into an independent, upgradeable base snapshot.
public PlayerRuntimeStats(PlayerShipDefinition definition)
{
	if (definition == null
		|| definition.Defence == null
		|| definition.Handling == null
		|| definition.Boost == null
		|| definition.Dash == null
		|| definition.Cargo == null
		|| definition.Resources == null
		|| definition.Systems == null)
	{
		throw new ArgumentException(
			"Player definition requires Defence, Handling, Boost, Dash, "
			+ "Cargo, Resources and Systems."
		);
	}

	PlayerDefenceStats defence = definition.Defence;
	PlayerHandlingStats handling = definition.Handling;
	PlayerBoostStats boost = definition.Boost;
	PlayerDashStats dash = definition.Dash;
	PlayerResourceStats resources = definition.Resources;

	SetBase(PlayerStat.MaxShield, defence.MaxShield);
	SetBase(PlayerStat.MaxArmour, defence.MaxArmour);
	SetBase(PlayerStat.MaxHull, defence.MaxHull);

	SetBase(PlayerStat.ForwardSpeed, handling.ForwardSpeed);
	SetBase(PlayerStat.ReverseSpeed, handling.ReverseSpeed);
	SetBase(PlayerStat.StrafeSpeed, handling.StrafeSpeed);
	SetBase(PlayerStat.VerticalSpeed, handling.VerticalSpeed);
	SetBase(PlayerStat.Acceleration, handling.Acceleration);
	SetBase(PlayerStat.Deceleration, handling.Deceleration);
	SetBase(PlayerStat.RollSpeed, handling.RollSpeed);
	SetBase(PlayerStat.MousePitchSensitivity, handling.MousePitchSensitivity);
	SetBase(PlayerStat.MouseYawSensitivity, handling.MouseYawSensitivity);
	SetBase(PlayerStat.MaxPitchSpeedDegrees, handling.MaxPitchSpeedDegrees);
	SetBase(PlayerStat.MaxYawSpeedDegrees, handling.MaxYawSpeedDegrees);
	SetBase(PlayerStat.SteeringResponse, handling.SteeringResponse);

	SetBase(PlayerStat.BoostSpeedMultiplier, boost.SpeedMultiplier);
	SetBase(PlayerStat.BoostAccelerationMultiplier, boost.AccelerationMultiplier);
	SetBase(PlayerStat.BoostResponse, boost.Response);

	SetBase(PlayerStat.DashSpeed, dash.Speed);
	SetBase(PlayerStat.DashDuration, dash.DurationSeconds);
	SetBase(PlayerStat.DashExitMultiplier, dash.ExitSpeedMultiplier);
	SetBase(PlayerStat.DashDoubleTapWindow, dash.DoubleTapWindowSeconds);
	SetBase(PlayerStat.DashCooldown, dash.CooldownSeconds);

	SetBase(PlayerStat.CargoMaximumMass, definition.Cargo.MaximumMass);

	SetBase(PlayerStat.MaximumFuel, resources.MaximumFuel);
	SetBase(PlayerStat.ThrustFuelPerSecond, resources.ThrustFuelPerSecond);
	SetBase(PlayerStat.BoostFuelPerSecond, resources.BoostFuelPerSecond);
	SetBase(PlayerStat.DashFuelCost, resources.DashFuelCost);
	SetBase(PlayerStat.JumpFuelCost, resources.JumpFuelCost);
	SetBase(PlayerStat.MaximumEnergy, resources.MaximumEnergy);
	SetBase(
		PlayerStat.EnergyRegenerationPerSecond,
		resources.EnergyRegenerationPerSecond
	);
	SetBase(
		PlayerStat.EnergyRechargeDelaySeconds,
		resources.EnergyRechargeDelaySeconds
	);

	Recalculate();
}

	// =========================================================
	// Stores one finite authored value in the independent base snapshot.
	private void SetBase(PlayerStat stat, float value)
	{
		if (!float.IsFinite(value))
		{
			throw new ArgumentException($"Non-finite base stat: {stat}");
		}

		_base[(int)stat] = value;
	}

	#endregion

	#region Queries

	// =========================================================
	// Returns one cached final value without recalculating.
	public float Get(PlayerStat stat)
	{
		return _final[(int)stat];
	}

	// =========================================================
	// Returns an original base value for comparisons and statistics screens.
	public float GetBase(PlayerStat stat)
	{
		return _base[(int)stat];
	}

	#endregion

	#region Modifier Sources

	// =========================================================
	// Replaces one source's complete bonuses rather than adding duplicate upgrades.
	public void SetSource(string sourceKey, params StatModifier[] modifiers)
	{
		if (string.IsNullOrWhiteSpace(sourceKey))
		{
			throw new ArgumentException("Modifier source requires a key.");
		}

		if (modifiers == null || modifiers.Length == 0)
		{
			RemoveSource(sourceKey);
			return;
		}

		// Copy the array so external callers cannot silently change active bonuses.
		_sources[sourceKey] = (StatModifier[])modifiers.Clone();

		Recalculate();
		Changed?.Invoke();
	}

	// =========================================================
	// Removes all bonuses belonging to an upgrade, module, or temporary effect.
	public bool RemoveSource(string sourceKey)
	{
		if (string.IsNullOrWhiteSpace(sourceKey)
			|| !_sources.Remove(sourceKey))
		{
			return false;
		}

		Recalculate();
		Changed?.Invoke();

		return true;
	}

	#endregion

	#region Calculation

	// =========================================================
	// Rebuilds final values from base using a consistent stacking rule.
	private void Recalculate()
	{
		for (int index = 0; index < _base.Length; index++)
		{
			PlayerStat stat = (PlayerStat)index;

			double flat = 0.0;
			double percent = 0.0;
			double multiplier = 1.0;

			foreach (StatModifier[] source in _sources.Values)
			{
				foreach (StatModifier modifier in source)
				{
					if (modifier.Stat != stat)
					{
						continue;
					}

					flat += modifier.Flat;
					percent += modifier.Percent;
					multiplier *= modifier.Multiplier;
				}
			}

			double value = (_base[index] + flat)
				* Math.Max(0.0, 1.0 + percent)
				* multiplier;

			if (!double.IsFinite(value))
			{
				throw new InvalidOperationException(
					$"Stat calculation overflow: {stat}"
				);
			}

			double minimum = stat switch
			{
				PlayerStat.MaxHull => 1.0,
				PlayerStat.BoostSpeedMultiplier => 1.0,
				PlayerStat.BoostAccelerationMultiplier => 1.0,
				PlayerStat.DashDuration => 0.01,
				PlayerStat.DashDoubleTapWindow => 0.01,
				_ => 0.0
			};

			double maximum = stat == PlayerStat.DashExitMultiplier
				? 1.0
				: float.MaxValue;

			_final[index] = (float)Math.Clamp(value, minimum, maximum);
		}
	}

	#endregion
}
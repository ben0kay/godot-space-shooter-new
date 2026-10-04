using System;
using Godot;

// Owns independent subsystem condition, enabled state and temporary disruption.
// Shared definitions are never modified by damage or repairs.
public sealed class ShipSystems
{
	#region Runtime

	private sealed class SystemState
	{
		public float Maximum;
		public float Condition;
		public bool Enabled = true;
		public float DisruptionRemaining;
		public float DisruptionStrength;
	}

	private readonly SystemState[] _systems =
		new SystemState[(int)ShipSystem.Count];

	private int _disruptedCount;

	#endregion

	#region Setup

	// =========================================================
	// Creates a separate healthy runtime for each internal system.
	public ShipSystems(ShipSystemsDefinition definition)
	{
		if (definition == null)
			throw new ArgumentNullException(nameof(definition));

		float maximum = Mathf.Max(1.0f, definition.MaximumCondition);

		for (int index = 0; index < _systems.Length; index++)
		{
			_systems[index] = new SystemState
			{
				Maximum = maximum,
				Condition = maximum
			};
		}
	}

	// =========================================================
	// Validates a system identifier before accessing its runtime.
	private SystemState GetState(ShipSystem system)
	{
		int index = (int)system;

		if (index < 0 || index >= _systems.Length)
			throw new ArgumentOutOfRangeException(nameof(system));

		return _systems[index];
	}

	#endregion

	#region Queries

	// =========================================================
	// Returns remaining condition in condition units.
	public float GetCondition(ShipSystem system)
	{
		return GetState(system).Condition;
	}

	// =========================================================
	// Returns the system's maximum condition.
	public float GetMaximumCondition(ShipSystem system)
	{
		return GetState(system).Maximum;
	}

	// =========================================================
	// Combines condition and temporary disruption into usable output.
	public float GetEffectiveness(ShipSystem system)
	{
		SystemState state = GetState(system);
		if (!state.Enabled || state.Condition <= 0.0f) return 0.0f;

		float condition = state.Condition / state.Maximum;
		float disruption = state.DisruptionRemaining > 0.0f
			? state.DisruptionStrength : 0.0f;

		return Mathf.Clamp(condition * (1.0f - disruption), 0.0f, 1.0f);
	}

	// =========================================================
	// Returns whether the system can provide any usable output.
	public bool IsOperational(ShipSystem system)
	{
		return GetEffectiveness(system) > 0.0f;
	}

	#endregion

	#region Changes

	// =========================================================
	// Enables or disables a system without changing its condition.
	public void SetEnabled(ShipSystem system, bool enabled)
	{
		GetState(system).Enabled = enabled;
	}

	// =========================================================
	// Removes condition without damaging other systems.
	public void Damage(ShipSystem system, float amount)
	{
		SystemState state = GetState(system);
		state.Condition = Mathf.Max(
			0.0f, state.Condition - Mathf.Max(0.0f, amount)
		);
	}

	// =========================================================
	// Restores condition without clearing disruption or enabling the system.
	public void Repair(ShipSystem system, float amount)
	{
		SystemState state = GetState(system);
		state.Condition = Mathf.Min(
			state.Maximum, state.Condition + Mathf.Max(0.0f, amount)
		);
	}

	// =========================================================
	// Applies temporary disruption, retaining the strongest active values.
	public void ApplyDisruption(
		ShipSystem system, float durationSeconds, float strength = 1.0f
	)
	{
		if (durationSeconds <= 0.0f || strength <= 0.0f) return;

		SystemState state = GetState(system);
		if (state.DisruptionRemaining <= 0.0f) _disruptedCount++;

		state.DisruptionRemaining = Mathf.Max(
			state.DisruptionRemaining, durationSeconds
		);
		state.DisruptionStrength = Mathf.Max(
			state.DisruptionStrength, Mathf.Clamp(strength, 0.0f, 1.0f)
		);
	}

	// =========================================================
	// Updates timers only while at least one system is disrupted.
	public void Update(float seconds)
	{
		if (_disruptedCount == 0 || seconds <= 0.0f) return;

		foreach (SystemState state in _systems)
		{
			if (state.DisruptionRemaining <= 0.0f) continue;

			state.DisruptionRemaining = Mathf.Max(
				0.0f, state.DisruptionRemaining - seconds
			);

			if (state.DisruptionRemaining > 0.0f) continue;

			state.DisruptionStrength = 0.0f;
			_disruptedCount--;
		}
	}

	#endregion
}
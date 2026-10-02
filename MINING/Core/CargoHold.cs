using System.Collections.Generic;
using Godot;

// Stores harvested materials on its owning ship without requiring a cargo HUD.
public partial class CargoHold : Node
{
	#region Configuration

	[Export] public bool LogHarvest = true;

	#endregion

	#region Runtime

	private readonly Dictionary<MiningResourceType, float> _amounts = new();

	#endregion

	#region Setup

	// =========================================================
	// Keeps cargo event-driven rather than updating it every frame.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);
	}

	#endregion

	#region Cargo

	// =========================================================
	// Returns the stored amount of one material.
	public float GetAmount(MiningResourceType resource)
	{
		return _amounts.TryGetValue(resource, out float amount)
			? amount
			: 0.0f;
	}

	// =========================================================
	// Adds extracted material and optionally reports whole-unit progress.
	public void Add(MiningResourceType resource, float amount)
	{
		if (!float.IsFinite(amount) || amount <= 0.0f)
		{
			return;
		}

		float previous = GetAmount(resource);
		float current = previous + amount;

		_amounts[resource] = current;

		if (LogHarvest
			&& Mathf.Floor(current) > Mathf.Floor(previous))
		{
			GD.Print($"Cargo — {resource}: {current:0.0}");
		}
	}

	#endregion
}
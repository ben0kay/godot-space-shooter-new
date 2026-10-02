using Godot;

// Builds a mineable surface patch that combat attacks destroy without harvesting.
public partial class ResourceDeposit : StaticBody3D, IDamageable, IMineable
{
	#region Identity

	public string PersistentId { get; private set; } = "";

	public ResourceDepositDefinition Definition => _definition;

	public float Remaining => _remaining;
	public float MaximumReserve => _maximumReserve;

		public MiningResourceType ResourceType => _definition != null
		? _definition.ResourceType
		: MiningResourceType.Iron;

	#endregion

	#region Runtime

	private ResourceDepositDefinition _definition;
	private StandardMaterial3D _material;

	private float _radius;
	private float _remaining;
	private float _maximumReserve;

	private bool _removed;

	#endregion

	#region Setup

	// =========================================================
	// Receives generated deposit settings before entering the scene tree.
	public void Configure(
		ResourceDepositDefinition definition,
		float reserve,
		float radius,
		string persistentId
	)
	{
		_definition = definition;
		_radius = Mathf.Max(0.1f, radius);

		_maximumReserve = Mathf.Max(0.01f, reserve);
		_remaining = _maximumReserve;

		PersistentId = persistentId;
	}

	// =========================================================
	// Builds a flattened ore patch and its matching solid collision.
	public override void _Ready()
	{
		SetProcess(false);
		SetPhysicsProcess(false);

		if (_definition == null)
		{
			GD.PushError("ResourceDeposit requires a definition.");
			QueueFree();
			return;
		}

		// Existing weapon rays use layer 1 and collide with bodies.
		CollisionLayer = 1;
		CollisionMask = 0;

		_material = new StandardMaterial3D
		{
			AlbedoColor = _definition.SurfaceColor,
			Roughness = 0.85f,

			EmissionEnabled = true,
			Emission = _definition.GlowColor,

			EmissionEnergyMultiplier = Mathf.Max(
				0.0f,
				_definition.EmissionEnergy
			)
		};

		SphereMesh mesh = new()
		{
			Radius = _radius,
			Height = _radius * 0.45f,
			RadialSegments = 10,
			Rings = 4
		};

		AddChild(new MeshInstance3D
		{
			Name = "Visual",
			Mesh = mesh,
			MaterialOverride = _material
		});

		AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = mesh.CreateConvexShape()
		});
	}

	#endregion

	#region Mining

	// =========================================================
	// Extracts a bounded amount without damaging the host asteroid.
	public float Extract(
		float requestedAmount,
		float miningStrength,
		out MiningResourceType resource
	)
	{
		resource = _definition != null
			? _definition.ResourceType
			: MiningResourceType.Iron;

		if (_removed
			|| _definition == null
			|| IsQueuedForDeletion()
			|| GetParent() == null
			|| GetParent().IsQueuedForDeletion()
			|| !float.IsFinite(requestedAmount)
			|| requestedAmount <= 0.0f
			|| !float.IsFinite(miningStrength)
			|| miningStrength < _definition.RequiredStrength)
		{
			return 0.0f;
		}

		float extracted = Mathf.Min(
			requestedAmount,
			_remaining
		);

		_remaining = Mathf.Max(
			0.0f,
			_remaining - extracted
		);

		if (_remaining <= 0.0f)
		{
			RemoveDeposit();
		}
		else
		{
			UpdateAppearance();
		}

		return extracted;
	}

	#endregion

	#region Combat Damage

	// =========================================================
	// Destroys the deposit without converting its remaining reserve into cargo.
	public void ApplyDamage(DamageInfo damage)
	{
		if (!_removed && damage.Amount > 0.0f)
		{
			RemoveDeposit();
		}
	}

	#endregion

	#region Presentation And Removal

	// =========================================================
	// Darkens the patch and reduces its glow as material is extracted.
	private void UpdateAppearance()
	{
		if (_material == null)
		{
			return;
		}

		float fraction = Mathf.Clamp(
			_remaining / _maximumReserve,
			0.0f,
			1.0f
		);

		Color depletedColor =
			_definition.SurfaceColor.Darkened(0.65f);

		_material.AlbedoColor = depletedColor.Lerp(
			_definition.SurfaceColor,
			fraction
		);

		_material.EmissionEnergyMultiplier =
			Mathf.Max(0.0f, _definition.EmissionEnergy)
			* fraction;
	}

	// =========================================================
	// Immediately prevents further extraction and removes the patch safely.
	private void RemoveDeposit()
	{
		if (_removed)
		{
			return;
		}

		_removed = true;

		Hide();

		SetDeferred("collision_layer", 0);

		QueueFree();
	}

	#endregion
}
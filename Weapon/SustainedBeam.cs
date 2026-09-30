using Godot;

// Draws and simulates a sustained beam that extends from a moving muzzle.
public partial class SustainedBeam : Node3D
{
	#region Runtime

	private Node3D _emitter;
	private CollisionObject3D _source;
	private Faction _faction;

	private PhysicsRayQueryParameters3D _ray;

	private MeshInstance3D _body;
	private MeshInstance3D _core;
	private MeshInstance3D _glow;

	private float _range;
	private float _width;
	private float _extensionSpeed;
	private float _damagePerSecond;

	private float _extendedLength;
	private float _visibleLength;

	private bool _active;

	#endregion

	#region Setup

	// =========================================================
	// Resolves weapon settings and prepares one sustained beam.
	public void Configure(
		WeaponDefinition weapon,
		Node3D emitter,
		CollisionObject3D source,
		Faction faction
	)
	{
		BeamDefinition definition = weapon?.Beam;

		if (definition == null || emitter == null || source == null)
		{
			GD.PushError("SustainedBeam requires a beam, emitter, and source.");
			QueueFree();
			return;
		}

		_emitter = emitter;
		_source = source;
		_faction = faction;

		BeamLaunchOverrides overrides = weapon.BeamOverrides;

		_range = Mathf.Max(
			0.01f,
			overrides != null && overrides.OverrideRange
				? overrides.Range
				: definition.Range
		);

		_width = Mathf.Max(
			0.001f,
			overrides != null && overrides.OverrideWidth
				? overrides.Width
				: definition.Width
		);

		_extensionSpeed = Mathf.Max(
			0.01f,
			overrides != null && overrides.OverrideExtensionSpeed
				? overrides.ExtensionSpeed
				: definition.ExtensionSpeed
		);

		_damagePerSecond = Mathf.Max(
			0.0f,
			overrides != null && overrides.OverrideDamage
				? overrides.DamagePerSecond
				: definition.DamagePerSecond
		);

		_ray = new PhysicsRayQueryParameters3D
		{
			CollisionMask = definition.CollisionMask,
			CollideWithBodies = true,
			CollideWithAreas = false,
			HitFromInside = true,
			Exclude = new Godot.Collections.Array<Rid>
			{
				source.GetRid()
			}
		};

		TopLevel = true;

		BuildVisuals(definition);

		_active = true;
		FollowEmitter();
		UpdateVisuals();
	}

	// =========================================================
	// Creates the beam layers once when firing begins.
	private void BuildVisuals(BeamDefinition definition)
	{
		Color energy = definition.EnergyColor;
		Color core = definition.CoreColor;

		if (definition.UseFactionPalette)
		{
			var palette = FactionPalettes.Get(_faction);

			energy = palette.Energy;
			core = palette.Core;
		}

		CylinderMesh mesh = new CylinderMesh
		{
			Height = 1.0f,
			TopRadius = 0.5f,
			BottomRadius = 0.5f,
			RadialSegments = 8
		};

		_glow = CreateLayer(
			mesh,
			energy,
			definition.EmissionEnergy,
			Mathf.Clamp(definition.GlowOpacity, 0.0f, 1.0f)
		);

		_body = CreateLayer(
			mesh,
			energy,
			definition.EmissionEnergy,
			0.65f
		);

		_core = CreateLayer(
			mesh,
			core,
			definition.EmissionEnergy,
			1.0f
		);

		_glowWidth = _width * Mathf.Max(
			1.0f,
			definition.GlowWidthMultiplier
		);
	}

	private float _glowWidth;

	// =========================================================
	// Creates an emissive cylinder aligned with the muzzle's forward axis.
	private MeshInstance3D CreateLayer(
		Mesh mesh,
		Color color,
		float emissionEnergy,
		float opacity
	)
	{
		StandardMaterial3D material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Add,
			AlbedoColor = new Color(
				color.R, color.G, color.B, opacity
			),
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = Mathf.Max(0.0f, emissionEnergy),
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};

		MeshInstance3D layer = new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};

		AddChild(layer);

		return layer;
	}

	#endregion

	#region Simulation

	// =========================================================
	// Extends the beam, checks its current length, and applies sustained damage.
	public override void _PhysicsProcess(double delta)
	{
		if (!_active)
		{
			return;
		}

		if (!GodotObject.IsInstanceValid(_emitter)
			|| !GodotObject.IsInstanceValid(_source)
			|| _emitter.IsQueuedForDeletion()
			|| _source.IsQueuedForDeletion())
		{
			Stop();
			return;
		}

		float seconds = (float)delta;

		FollowEmitter();

		_extendedLength = Mathf.Min(
			_range,
			_extendedLength + _extensionSpeed * seconds
		);

		Vector3 origin = GlobalPosition;
		Vector3 direction = -GlobalBasis.Z.Normalized();

		_ray.From = origin;
		_ray.To = origin + direction * _extendedLength;

		var hit = GetWorld3D().DirectSpaceState.IntersectRay(_ray);

		_visibleLength = _extendedLength;

		if (hit.Count > 0)
		{
			Vector3 hitPosition = hit["position"].AsVector3();

			_visibleLength = origin.DistanceTo(hitPosition);

			GodotObject collider = hit["collider"].AsGodotObject();

			if (collider is IDamageable damageable)
			{
				damageable.ApplyDamage(new DamageInfo(
					_damagePerSecond * seconds,
					_source,
					_faction
				));
			}
		}

		UpdateVisuals();
	}

	// =========================================================
	// Keeps the beam attached to cosmetic ship motion between physics ticks.
	public override void _Process(double delta)
	{
		if (_active && GodotObject.IsInstanceValid(_emitter))
		{
			FollowEmitter();
		}
	}

	// =========================================================
	// Copies muzzle position and direction without inheriting its scale.
	private void FollowEmitter()
	{
		GlobalTransform = new Transform3D(
			_emitter.GlobalBasis.Orthonormalized(),
			_emitter.GlobalPosition
		);
	}

	// =========================================================
	// Stops damage immediately and removes the visible beam.
	public void Stop()
	{
		_active = false;

		Hide();
		SetPhysicsProcess(false);
		SetProcess(false);

		QueueFree();
	}

	#endregion

	#region Presentation

	// =========================================================
	// Sizes all beam layers to the unobstructed visible length.
	private void UpdateVisuals()
	{
		if (_body == null)
		{
			return;
		}

		Visible = _visibleLength > 0.001f;

		SetLayerTransform(_glow, _glowWidth);
		SetLayerTransform(_body, _width);
		SetLayerTransform(_core, _width * 0.3f);
	}

	// =========================================================
	// Places one cylinder from the muzzle to the beam endpoint.
	private void SetLayerTransform(
		MeshInstance3D layer,
		float diameter
	)
	{
		Basis alignment = new Basis(
			Vector3.Right,
			Mathf.Pi * 0.5f
		);

		Basis scale = Basis.Identity.Scaled(new Vector3(
			diameter,
			Mathf.Max(0.001f, _visibleLength),
			diameter
		));

		layer.Transform = new Transform3D(
			alignment * scale,
			new Vector3(0.0f, 0.0f, -_visibleLength * 0.5f)
		);
	}

	#endregion
}

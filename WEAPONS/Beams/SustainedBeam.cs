using Godot;

// Simulates an extending beam with flowing visuals and reusable GPU particle emitters.
public partial class SustainedBeam : Node3D
{
	#region Shared Assets

	private static Shader _flowShader;

	#endregion

	#region Runtime

	private Node3D _emitter;
	private CollisionObject3D _source;
	private Faction _faction;
	private BeamDefinition _definition;

	private PhysicsRayQueryParameters3D _ray;

	private readonly MeshInstance3D[] _layers = new MeshInstance3D[3];
	private readonly ShaderMaterial[] _materials = new ShaderMaterial[3];
	private readonly float[] _diameters = new float[3];

	private GpuParticles3D _embers;
	private GpuParticles3D _muzzleParticles;
	private GpuParticles3D _impactParticles;

	private ParticleProcessMaterial _emberProcess;
	private ParticleProcessMaterial _impactProcess;

	private float _range;
	private float _width;
	private float _extensionSpeed;
	private float _damagePerSecond;

	private float _extendedLength;
	private float _visibleLength;

	private float _releaseElapsed;
	private float _cleanupDuration;

	private bool _active;
	private bool _releasing;
		private WeaponMount _aimMount;
	private Vector3 _aimPoint;
	private bool _hasAimPoint;
	private DamageType _damageType;

		private MiningSettings _mining;
	private CargoHold _cargo;

	#endregion

	#region Setup

		// =========================================================
	// Connects optional camera aiming and schedules the beam after player movement.
	public void SetAimMount(WeaponMount mount)
	{
		_aimMount = mount;

		ProcessPhysicsPriority = 30;

		// Follow the emitter after the normal camera and ship presentation updates.
		ProcessPriority = 10;
	}

		// =========================================================
	// Resolves beam settings and selects combat damage or resource extraction.
	public void Configure(
		WeaponDefinition weapon,
		Node3D emitter,
		CollisionObject3D source,
		Faction faction
	)
	{
		_definition = weapon?.Beam;

		if (_definition == null || emitter == null || source == null)
		{
			GD.PushError(
				"SustainedBeam requires a beam, emitter, and source."
			);

			QueueFree();
			return;
		}

		_emitter = emitter;
		_source = source;
		_faction = faction;

		_mining = weapon.Mining;

		if (_mining != null)
		{
			_cargo = source.GetNodeOrNull<CargoHold>("Cargo");

			if (!GodotObject.IsInstanceValid(_cargo))
			{
				GD.PushError(
					"A mining beam requires a CargoHold child named Cargo "
					+ "on its source ship."
				);

				QueueFree();
				return;
			}
		}

		BeamLaunchOverrides overrides = weapon.BeamOverrides;

		_damageType = overrides != null && overrides.OverrideDamageType
			? overrides.DamageType
			: _definition.DamageType;

		_range = Mathf.Max(
			0.01f,
			overrides != null && overrides.OverrideRange
				? overrides.Range
				: _definition.Range
		);

		_width = Mathf.Max(
			0.001f,
			overrides != null && overrides.OverrideWidth
				? overrides.Width
				: _definition.Width
		);

		_extensionSpeed = Mathf.Max(
			0.01f,
			overrides != null && overrides.OverrideExtensionSpeed
				? overrides.ExtensionSpeed
				: _definition.ExtensionSpeed
		);

		_damagePerSecond = _mining != null
			? 0.0f
			: Mathf.Max(
				0.0f,
				overrides != null && overrides.OverrideDamage
					? overrides.DamagePerSecond
					: _definition.DamagePerSecond
			);

		_ray = new PhysicsRayQueryParameters3D
		{
			CollisionMask = _definition.CollisionMask,
			CollideWithBodies = true,
			CollideWithAreas = false,
			HitFromInside = true
		};

		ShipShield.ConfigureWeaponQuery(_ray, source);

		TopLevel = true;
		FollowEmitter();

		Color energy = _definition.EnergyColor;
		Color core = _definition.CoreColor;

		if (_definition.UseFactionPalette)
		{
			var palette = FactionPalettes.Get(_faction);

			energy = palette.Energy;
			core = palette.Core;
		}

		BuildVisuals(energy, core);

		if (_definition.ParticlesEnabled)
		{
			BuildParticles(energy);
		}

		_cleanupDuration = Mathf.Max(
			Mathf.Max(0.0f, _definition.ReleaseDuration),
			_definition.ParticlesEnabled
				? Mathf.Max(0.05f, _definition.EmberLifetime)
				: 0.0f
		);

		_active = true;

		UpdateVisuals();
	}

	// =========================================================
	// Builds segmented cylinders that can bend smoothly in the shader.
	private void BuildVisuals(Color energy, Color core)
	{
		if (_flowShader == null)
		{
			_flowShader = GD.Load<Shader>(
				"res://WEAPONS/Beams/FlowingBeam.gdshader"
			);
		}

		if (_flowShader == null)
		{
			GD.PushError("Could not load WEAPONS/Beams/FlowingBeam.gdshader.");
			return;
		}

		CylinderMesh mesh = new CylinderMesh
		{
			Height = 1.0f,
			TopRadius = 0.5f,
			BottomRadius = 0.5f,
			RadialSegments = 8,
			Rings = 64
		};

		_diameters[0] = _width * Mathf.Max(
			1.0f,
			_definition.GlowWidthMultiplier
		);

		_diameters[1] = _width;
		_diameters[2] = _width * 0.28f;

		CreateLayer(
			0,
			mesh,
			energy,
			Mathf.Clamp(_definition.GlowOpacity, 0.0f, 1.0f)
		);

		CreateLayer(1, mesh, energy, 0.55f);
		CreateLayer(2, mesh, core, 0.65f);
	}

	// =========================================================
	// Creates one beam layer and assigns its shared animation settings.
	private void CreateLayer(
		int index,
		Mesh mesh,
		Color color,
		float opacity
	)
	{
		ShaderMaterial material = new ShaderMaterial
		{
			Shader = _flowShader
		};

		material.SetShaderParameter("beam_color", color);
		material.SetShaderParameter("opacity", opacity);
		material.SetShaderParameter(
			"emission_energy",
			Mathf.Max(0.0f, _definition.EmissionEnergy)
		);

		material.SetShaderParameter(
			"beam_diameter",
			_diameters[index]
		);

		material.SetShaderParameter(
			"wobble_amount",
			_definition.WobbleAmount
		);

		material.SetShaderParameter(
			"wobble_wavelength",
			_definition.WobbleWavelength
		);

		material.SetShaderParameter(
			"wobble_speed",
			_definition.WobbleSpeed
		);

		material.SetShaderParameter(
			"pulse_amount",
			_definition.PulseAmount
		);

		material.SetShaderParameter(
			"pulse_speed",
			_definition.PulseSpeed
		);

		material.SetShaderParameter(
			"band_spacing",
			_definition.BandSpacing
		);

		material.SetShaderParameter(
			"band_speed",
			_definition.BandSpeed
		);

		material.SetShaderParameter(
			"band_strength",
			_definition.BandStrength
		);

		MeshInstance3D layer = new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,

			// Include shader displacement in the mesh's culling bounds.
			ExtraCullMargin = Mathf.Abs(_definition.WobbleAmount) * 3.0f
		};

		AddChild(layer);

		_layers[index] = layer;
		_materials[index] = material;
	}

	#endregion

	#region Particle Setup

	// =========================================================
	// Creates reusable emitters for the beam body, muzzle, and contact point.
	private void BuildParticles(Color energy)
	{
		float size = Mathf.Max(0.001f, _definition.EmberSize);
		float speed = Mathf.Max(0.0f, _definition.EmberSpeed);

		_embers = CreateParticles(
			_definition.EmberAmount,
			size,
			speed,
			energy
		);

		_emberProcess = (ParticleProcessMaterial)_embers.ProcessMaterial;
		_emberProcess.EmissionShape =
			ParticleProcessMaterial.EmissionShapeEnum.Box;

		_muzzleParticles = CreateParticles(
			_definition.MuzzleAmount,
			size * 1.25f,
			speed * 0.8f,
			energy
		);

		_impactParticles = CreateParticles(
			_definition.ImpactAmount,
			size * 0.8f,
			speed * 3.0f,
			energy
		);

		_impactProcess =
			(ParticleProcessMaterial)_impactParticles.ProcessMaterial;
	}

	// =========================================================
	// Builds a small world-space particle emitter with fading energy motes.
	private GpuParticles3D CreateParticles(
		int amount,
		float size,
		float speed,
		Color energy
	)
	{
		Gradient fade = new Gradient
		{
			Offsets = new float[] { 0.0f, 0.2f, 1.0f },
			Colors = new Color[]
			{
				new Color(energy.R, energy.G, energy.B, 0.0f),
				new Color(energy.R, energy.G, energy.B, 0.8f),
				new Color(energy.R, energy.G, energy.B, 0.0f)
			}
		};

		ParticleProcessMaterial process = new ParticleProcessMaterial
		{
			Direction = Vector3.Back,
			Spread = 70.0f,
			Gravity = Vector3.Zero,

			InitialVelocityMin = speed * 0.3f,
			InitialVelocityMax = speed,

			ScaleMin = 0.5f,
			ScaleMax = 1.3f,

			ColorRamp = new GradientTexture1D
			{
				Gradient = fade
			}
		};

		StandardMaterial3D material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Add,
			VertexColorUseAsAlbedo = true,

			AlbedoColor = Colors.White,

			// Particle colour and alpha come from the gradient.
			// Keeping emission disabled avoids washing the motes white.
			EmissionEnabled = false
		};

		SphereMesh mesh = new SphereMesh
		{
			Radius = size * 0.5f,
			Height = size,
			RadialSegments = 6,
			Rings = 3,
			Material = material
		};

		float padding = Mathf.Max(
			2.0f,
			speed * Mathf.Max(0.05f, _definition.EmberLifetime) + 1.0f
		);

		GpuParticles3D particles = new GpuParticles3D
		{
			Amount = Mathf.Max(1, amount),
			Lifetime = Mathf.Max(0.05f, _definition.EmberLifetime),
			LocalCoords = false,
			Emitting = false,
			ProcessMaterial = process,
			DrawPass1 = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,

			VisibilityAabb = new Aabb(
				new Vector3(-padding, -padding, -_range - padding),
				new Vector3(
					padding * 2.0f,
					padding * 2.0f,
					_range + padding * 2.0f
				)
			)
		};

		AddChild(particles);

		return particles;
	}

	#endregion

	#region Simulation

		// =========================================================
	// Extends the beam and applies its selected behaviour at the first contact.
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

		_hasAimPoint =
			GodotObject.IsInstanceValid(_aimMount)
			&& _aimMount.AimAtCameraCenter;

		if (_hasAimPoint)
		{
			_aimPoint = _aimMount.GetCameraAimPoint(
				_emitter.GlobalPosition,
				_range
			);
		}

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

		bool contact = hit.Count > 0;
		Vector3 normal = -direction;

		if (contact)
		{
			Vector3 position = hit["position"].AsVector3();

			_visibleLength = origin.DistanceTo(position);
			normal = hit["normal"].AsVector3();

			GodotObject collider = hit["collider"].AsGodotObject();

			ApplyContact(
				collider,
				position,
				normal,
				seconds
			);
		}

		UpdateVisuals();
		UpdateParticles(contact, normal);
	}

	// =========================================================
	// Follows cosmetic ship motion and fades released beam visuals.
	public override void _Process(double delta)
	{
		if (_active)
		{
			if (GodotObject.IsInstanceValid(_emitter))
			{
				FollowEmitter();
			}

			return;
		}

		if (!_releasing)
		{
			return;
		}

		_releaseElapsed += (float)delta;

		float duration = Mathf.Max(0.0f, _definition.ReleaseDuration);

		float alpha = duration > 0.0f
			? Mathf.Clamp(1.0f - _releaseElapsed / duration, 0.0f, 1.0f)
			: 0.0f;

		for (int i = 0; i < _materials.Length; i++)
		{
			if (_materials[i] != null)
			{
				_materials[i].SetShaderParameter(
					"release_alpha",
					alpha
				);
			}

			if (_layers[i] != null)
			{
				_layers[i].Visible = alpha > 0.0f;
			}
		}

		if (_releaseElapsed >= _cleanupDuration)
		{
			QueueFree();
		}
	}

	// =========================================================
	// Checks cargo space before extraction and keeps mining separate from damage.
	private void ApplyContact(
		GodotObject collider,
		Vector3 position,
		Vector3 normal,
		float seconds
	)
	{
		if (_mining != null)
		{
			if (collider is IMineable mineable
				&& GodotObject.IsInstanceValid(_cargo)
				&& !_cargo.IsQueuedForDeletion())
			{
				float requested = Mathf.Min(
					Mathf.Max(0.0f, _mining.UnitsPerSecond) * seconds,
					_cargo.GetFreeSpace(mineable.ResourceType)
				);

				if (requested > 0.0f)
				{
					float extracted = mineable.Extract(
						requested,
						Mathf.Max(0.0f, _mining.Strength),
						out MiningResourceType resource
					);

					_cargo.Add(resource, extracted);
				}
			}

			return;
		}

		if (_damagePerSecond > 0.0f
			&& collider is IDamageable damageable)
		{
			damageable.ApplyDamage(new DamageInfo(
				_damagePerSecond * seconds,
				_source,
				_faction,
				position,
				normal,
				_damageType
			));
		}
	}

		// =========================================================
	// Anchors the beam to its muzzle and optionally points toward the crosshair.
	private void FollowEmitter()
	{
		Vector3 origin = _emitter.GlobalPosition;

		if (_hasAimPoint)
		{
			Vector3 offset = _aimPoint - origin;

			if (offset.LengthSquared() > 0.0001f)
			{
				Vector3 direction = offset.Normalized();

				Vector3 up =
					Mathf.Abs(direction.Dot(Vector3.Up)) > 0.99f
						? Vector3.Right
						: Vector3.Up;

				LookAtFromPosition(
					origin,
					origin + direction,
					up
				);

				return;
			}
		}

		GlobalTransform = new Transform3D(
			_emitter.GlobalBasis.Orthonormalized(),
			origin
		);
	}

		// =========================================================
	// Stops damage immediately while allowing beam visuals and particles to fade.
	public void Stop()
	{
		if (_releasing)
		{
			return;
		}

		_active = false;
		_releasing = true;
		_releaseElapsed = 0.0f;

		SetPhysicsProcess(false);

		if (_embers != null)
		{
			_embers.Emitting = false;
		}

		if (_muzzleParticles != null)
		{
			_muzzleParticles.Emitting = false;
		}

		if (_impactParticles != null)
		{
			_impactParticles.Emitting = false;
		}
	}

	#endregion

	#region Presentation

	// =========================================================
	// Updates beam length and places each cylinder between its endpoints.
	private void UpdateVisuals()
	{
		Basis alignment = new Basis(
			Vector3.Right,
			Mathf.Pi * 0.5f
		);

		for (int i = 0; i < _layers.Length; i++)
		{
			if (_layers[i] == null)
			{
				continue;
			}

			_layers[i].Visible = _visibleLength > 0.001f;

			Basis scale = Basis.Identity.Scaled(new Vector3(
				_diameters[i],
				Mathf.Max(0.001f, _visibleLength),
				_diameters[i]
			));

			_layers[i].Transform = new Transform3D(
				alignment * scale,
				new Vector3(0.0f, 0.0f, -_visibleLength * 0.5f)
			);

			_materials[i].SetShaderParameter(
				"beam_length",
				_visibleLength
			);
		}
	}

	// =========================================================
	// Updates the reusable emission regions and contact spark direction.
	private void UpdateParticles(bool contact, Vector3 normal)
	{
		if (_embers == null)
		{
			return;
		}

		bool visibleBeam = _visibleLength > 0.01f;

		_embers.Emitting = visibleBeam;
		_muzzleParticles.Emitting = visibleBeam;

		// Spread embers along the current beam, stopping before its endpoint.
		_embers.Position = new Vector3(
			0.0f,
			0.0f,
			-_visibleLength * 0.5f
		);

		_emberProcess.EmissionBoxExtents = new Vector3(
			_width * 0.5f,
			_width * 0.5f,
			_visibleLength * 0.48f
		);

		_impactParticles.Position = new Vector3(
			0.0f,
			0.0f,
			-_visibleLength
		);

		_impactParticles.Emitting = contact && visibleBeam;

		if (normal.LengthSquared() > 0.001f)
		{
			_impactProcess.Direction =
				GlobalBasis.Inverse() * normal.Normalized();
		}
	}

	#endregion
}

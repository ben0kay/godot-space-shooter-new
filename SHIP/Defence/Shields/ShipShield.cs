using System.Collections.Generic;
using Godot;

// Generates an enclosing shield, intercepts weapon hits, and forwards damage to its ship.
public partial class ShipShield : StaticBody3D, IDamageable
{
	#region Physics

	public const uint ShieldLayer = 1u << 4;

	#endregion

	#region Runtime

	private static Shader _shader;

	private CollisionObject3D _ship;
	private IDamageable _damageReceiver;
	private ShipDefence _defence;
	private ShieldVisualSettings _settings;

	private MeshInstance3D _visual;
	private ShaderMaterial _material;

	private Vector3 _radii;

	private float _hitElapsed;
	private float _collapseElapsed;

	private bool _hitActive;
	private bool _collapsing;

	public CollisionObject3D Ship => _ship;

	#endregion

	#region Setup

// =========================================================
// Adds a shared enclosing shield using explicit settings or the default ellipsoid.
public static ShipShield Attach(
	CollisionObject3D ship,
	ShipDefence defence,
	Faction faction,
	ShieldVisualSettings settings
)
{
	if (defence == null || defence.MaxShield <= 0.0f)
	{
		return null;
	}

	ShipShield shield = new ShipShield
	{
		Name = "Shield",
		CollisionLayer = ShieldLayer,
		CollisionMask = 0,

		_ship = ship,
		_damageReceiver = ship as IDamageable,
		_defence = defence,

		_settings = settings ?? new ShieldVisualSettings()
	};

	ship.AddChild(shield);

	Color color = shield._settings.UseFactionPalette
		? FactionPalettes.Get(faction).Energy
		: shield._settings.Color;

	shield.Build(color);

	return shield;
}

// =========================================================
// Fits the shield to ship bounds and sampled player banking, then builds matching geometry.
private void Build(Color color)
{
	SetPhysicsProcess(false);
	SetProcess(false);

	List<Vector3> points = new();
	CollectBounds(_ship, points);

	if (points.Count == 0)
	{
		points.Add(new Vector3(-2.0f, -1.0f, -2.0f));
		points.Add(new Vector3(2.0f, 1.0f, 2.0f));
	}

	if (!_settings.Spherical)
	{
		AddPlayerMotionBounds(points);
	}

	Vector3 minimum = points[0];
	Vector3 maximum = points[0];

	float sphereRadius = 0.0f;

	foreach (Vector3 point in points)
	{
		minimum = new Vector3(
			Mathf.Min(minimum.X, point.X),
			Mathf.Min(minimum.Y, point.Y),
			Mathf.Min(minimum.Z, point.Z)
		);

		maximum = new Vector3(
			Mathf.Max(maximum.X, point.X),
			Mathf.Max(maximum.Y, point.Y),
			Mathf.Max(maximum.Z, point.Z)
		);

		sphereRadius = Mathf.Max(sphereRadius, point.Length());
	}

	float multiplier = Mathf.Max(1.0f, _settings.SizeMultiplier);
	float padding = Mathf.Max(0.0f, _settings.Padding);

	if (_settings.Spherical)
	{
		Position = Vector3.Zero;

		_radii = Vector3.One * (
			sphereRadius * multiplier + padding
		);
	}
	else
	{
		Position = (minimum + maximum) * 0.5f;

		Vector3 halfSize = (maximum - minimum) * 0.5f;

		halfSize = new Vector3(
			Mathf.Max(0.1f, halfSize.X),
			Mathf.Max(0.1f, halfSize.Y),
			Mathf.Max(0.1f, halfSize.Z)
		);

		// Find the expansion needed to enclose the collected points.
		// This avoids automatically using sqrt(3) for every ship.
		float enclosingScale = 1.0f;

		foreach (Vector3 point in points)
		{
			Vector3 relative = point - Position;

			Vector3 normalized = new Vector3(
				relative.X / halfSize.X,
				relative.Y / halfSize.Y,
				relative.Z / halfSize.Z
			);

			enclosingScale = Mathf.Max(
				enclosingScale,
				normalized.Length()
			);
		}

		_radii = halfSize * enclosingScale * multiplier
			+ Vector3.One * padding;
	}

	SphereMesh mesh = new SphereMesh
	{
		Radius = 1.0f,
		Height = 2.0f,
		RadialSegments = 48,
		Rings = 24
	};

	Vector3[] vertices = mesh.SurfaceGetArrays(0)[
		(int)Mesh.ArrayType.Vertex
	].AsVector3Array();

	for (int i = 0; i < vertices.Length; i++)
	{
		vertices[i] *= _radii;
	}

	AddChild(new CollisionShape3D
	{
		Name = "ShieldCollision",
		Shape = new ConvexPolygonShape3D
		{
			Points = vertices
		}
	});

	if (_shader == null)
	{
		_shader = GD.Load<Shader>(
			"res://SHIP/Defence/Shields/ShipShield.gdshader"
		);
	}

	if (_shader == null)
	{
		GD.PushError("Could not load SHIELDS/ShipShield.gdshader.");
		return;
	}

	_material = new ShaderMaterial
	{
		Shader = _shader
	};

	_material.SetShaderParameter("shield_color", color);
	_material.SetShaderParameter("idle_opacity", _settings.IdleOpacity);
	_material.SetShaderParameter("rim_opacity", _settings.RimOpacity);
	_material.SetShaderParameter("brightness", _settings.Brightness);

	_visual = new MeshInstance3D
	{
		Name = "ShieldVisual",
		Mesh = mesh,
		Scale = _radii,
		MaterialOverride = _material,
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
	};

	AddChild(_visual);

	Refresh();
}

// =========================================================
// Adds sampled cosmetic pitch and bank poses so the player's wings stay enclosed.
private void AddPlayerMotionBounds(List<Vector3> points)
{
	if (_ship is not PlayerShip)
	{
		return;
	}

	PlayerFlightVisuals visuals =
		_ship.GetNodeOrNull<PlayerFlightVisuals>("FlightVisuals");

	if (visuals == null)
	{
		return;
	}

	float maximumPitch = Mathf.Abs(visuals.PitchTiltDegrees);

	float maximumBank =
		Mathf.Abs(visuals.TurnBankDegrees)
		+ Mathf.Abs(visuals.StrafeBankDegrees)
		+ Mathf.Abs(visuals.RollTiltDegrees);

	Vector3[] restPoints = points.ToArray();

	// Sample once at spawn; no bounds calculations run during normal flight.
	for (int pitchStep = 0; pitchStep <= 4; pitchStep++)
	{
		float pitch = Mathf.Lerp(
			-maximumPitch,
			maximumPitch,
			pitchStep / 4.0f
		);

		for (int bankStep = 0; bankStep <= 16; bankStep++)
		{
			float bank = Mathf.Lerp(
				-maximumBank,
				maximumBank,
				bankStep / 16.0f
			);

			Basis rotation = Basis.FromEuler(new Vector3(
				Mathf.DegToRad(pitch),
				0.0f,
				Mathf.DegToRad(bank)
			));

			foreach (Vector3 point in restPoints)
			{
				points.Add(rotation * point);
			}
		}
	}
}

	// =========================================================
	// Collects mesh and collision bounds in the owning ship's coordinates.
	private void CollectBounds(Node parent, List<Vector3> points)
	{
		foreach (Node child in parent.GetChildren())
		{
			// Exclude the shield and effect emitters from ship geometry.
			if (child is ShipShield || child is GpuParticles3D)
			{
				continue;
			}

			if (child is MeshInstance3D mesh
				&& mesh.Mesh != null
				&& mesh.Visible)
			{
				AddBounds(mesh, mesh.GetAabb(), points);
			}
			else if (child is CollisionShape3D collision
				&& collision.Shape != null
				&& !collision.Disabled)
			{
				AddBounds(
					collision,
					collision.Shape.GetDebugMesh().GetAabb(),
					points
				);
			}

			CollectBounds(child, points);
		}
	}

	// =========================================================
	// Transforms all eight corners of one local bound into ship space.
	private void AddBounds(
		Node3D node,
		Aabb bounds,
		List<Vector3> points
	)
	{
		Transform3D transform =
			_ship.GlobalTransform.AffineInverse() * node.GlobalTransform;

		for (int i = 0; i < 8; i++)
		{
			points.Add(transform * bounds.GetEndpoint(i));
		}
	}

	#endregion

	#region Damage

	// =========================================================
	// Forwards a shield-surface hit to the owning ship's existing defence resolver.
	public void ApplyDamage(DamageInfo damage)
	{
		if (!GodotObject.IsInstanceValid(_ship)
			|| _ship.IsQueuedForDeletion()
			|| damage.Source == _ship
			|| _defence.Destroyed)
		{
			return;
		}

		_damageReceiver?.ApplyDamage(damage);
	}

	// =========================================================
// Shows actual shield damage without restarting an expanding ripple every beam tick.
public void NotifyDamage(float shieldBefore, DamageInfo damage)
{
	if (shieldBefore <= _defence.Shield)
	{
		return;
	}

	if (_material != null
		&& damage.HasImpact
		&& !_hitActive)
	{
		Vector3 local = ToLocal(damage.ImpactPosition);

		Vector3 direction = new Vector3(
			local.X / _radii.X,
			local.Y / _radii.Y,
			local.Z / _radii.Z
		);

		direction = direction.LengthSquared() > 0.001f
			? direction.Normalized()
			: Vector3.Forward;

		_material.SetShaderParameter("hit_direction", direction);
		_material.SetShaderParameter("hit_progress", 0.0f);
		_material.SetShaderParameter("hit_strength", 1.0f);

		_hitElapsed = 0.0f;
		_hitActive = true;

		SetProcess(true);
	}

	if (_defence.Shield <= 0.0f)
	{
		CollisionLayer = 0;

		_collapsing = true;
		_collapseElapsed = 0.0f;

		SetProcess(true);
	}

	Refresh();
}

	// =========================================================
	// Synchronizes visibility and interception with the authoritative defence values.
	public void Refresh()
	{
		bool active = _defence.Shield > 0.0f && !_defence.Destroyed;

		CollisionLayer = active ? ShieldLayer : 0u;

		if (_material != null)
		{
			_material.SetShaderParameter(
				"shield_ratio",
				_defence.MaxShield > 0.0f
					? _defence.Shield / _defence.MaxShield
					: 0.0f
			);
		}

		if (_visual != null)
		{
			_visual.Visible = active || _collapsing;
		}
	}

	#endregion

	#region Animation

	// =========================================================
	// Updates temporary ripples and collapse, then disables frame processing.
	public override void _Process(double delta)
	{
		float seconds = (float)delta;

		if (_hitActive)
		{
			_hitElapsed += seconds;

			float progress = Mathf.Clamp(
				_hitElapsed / Mathf.Max(0.01f, _settings.HitDuration),
				0.0f,
				1.0f
			);

			_material?.SetShaderParameter("hit_progress", progress);
			_material?.SetShaderParameter("hit_strength", 1.0f - progress);

			_hitActive = progress < 1.0f;
		}

		if (_collapsing)
		{
			_collapseElapsed += seconds;

			float progress = Mathf.Clamp(
				_collapseElapsed
					/ Mathf.Max(0.01f, _settings.CollapseDuration),
				0.0f,
				1.0f
			);

			_material?.SetShaderParameter("collapse_progress", progress);

			if (_visual != null)
			{
				_visual.Scale = _radii * (1.0f + progress * 0.12f);
			}

			if (progress >= 1.0f)
			{
				_collapsing = false;
				_hitActive = false;

				_visual?.Hide();
			}
		}

		if (!_hitActive && !_collapsing)
		{
			SetProcess(false);
		}
	}

	#endregion

	#region Weapon Queries

	// =========================================================
	// Includes shield surfaces and excludes the firing ship's own shield.
	public static void ConfigureWeaponQuery(
		PhysicsRayQueryParameters3D query,
		CollisionObject3D source
	)
	{
		query.CollisionMask |= ShieldLayer;
		query.CollideWithBodies = true;

		if (!GodotObject.IsInstanceValid(source))
		{
			return;
		}

		var exclusions = query.Exclude;

		Rid sourceRid = source.GetRid();

		if (!exclusions.Contains(sourceRid))
		{
			exclusions.Add(sourceRid);
		}

		ShipShield shield = source.GetNodeOrNull<ShipShield>("Shield");

		if (shield != null && !exclusions.Contains(shield.GetRid()))
		{
			exclusions.Add(shield.GetRid());
		}

		query.Exclude = exclusions;
	}

	#endregion
}

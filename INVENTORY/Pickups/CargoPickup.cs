using Godot;

// Represents a shared world item stack with automatic collection and placeholder visuals.
public partial class CargoPickup : Area3D
{
	#region Configuration

	[Export] public ItemDefinition Item;
	[Export] public float Amount = 1.0f;

	[Export] public float CollectionRadius = 4.0f;
	[Export] public float RetryInterval = 0.2f;
	[Export] public float SpinDegreesPerSecond = 18.0f;

	#endregion

	#region Runtime

	private Node3D _visual;
	private Label3D _label;

	private PlayerShip _dropOwner;
	private bool _ownerCleared;

	private float _armingRemaining;
	private float _retryRemaining;
	private bool _collecting;
	private bool _consumed;

	#endregion

	#region Setup

	// =========================================================
	// Builds a collection volume and a small visible floating cargo container.
	public override void _Ready()
	{
		if (Item == null || !float.IsFinite(Amount) || Amount <= 0.0f)
		{
			GD.PushError("CargoPickup requires an item and a positive amount.");
			QueueFree();
			return;
		}

		CollectionRadius = Mathf.Max(0.5f, CollectionRadius);

		// Pickups detect bodies without blocking ships or projectile body rays.
		CollisionLayer = 0;
		CollisionMask = 1;
		Monitoring = true;
		Monitorable = false;

		AddChild(new CollisionShape3D
		{
			Shape = new SphereShape3D
			{
				Radius = CollectionRadius
			}
		});

		BuildVisual();

		BodyEntered += OnBodyEntered;

		_retryRemaining = UpdateStagger.Offset(
			this,
			Mathf.Max(0.05f, RetryInterval)
		);
	}

	// =========================================================
	// Creates a replaceable container visual with item-coloured illuminated bands.
	private void BuildVisual()
	{
		_visual = new Node3D
		{
			Name = "Visual"
		};

		AddChild(_visual);

		StandardMaterial3D metal = new()
		{
			AlbedoColor = new Color(0.09f, 0.12f, 0.15f),
			Metallic = 0.65f,
			Roughness = 0.45f
		};

		StandardMaterial3D light = new()
		{
			AlbedoColor = Item.IconColor,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			EmissionEnabled = true,
			Emission = Item.IconColor,
			EmissionEnergyMultiplier = 1.8f
		};

		AddVisualBox(
			new Vector3(1.1f, 0.7f, 0.8f),
			Vector3.Zero,
			metal
		);

		for (int side = -1; side <= 1; side += 2)
		{
			AddVisualBox(
				new Vector3(0.06f, 0.76f, 0.86f),
				new Vector3(side * 0.38f, 0, 0),
				light
			);
		}

		_label = new Label3D
		{
			Name = "Readout",
			Position = new Vector3(0, 1.1f, 0),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 32,
			PixelSize = 0.006f,
			Modulate = Item.IconColor
		};

		AddChild(_label);
		UpdateReadout();
	}

	// =========================================================
	// Adds one generated container part without physical collision.
	private void AddVisualBox(Vector3 size, Vector3 position, Material material)
	{
		_visual.AddChild(new MeshInstance3D
		{
			Position = position,
			Mesh = new BoxMesh
			{
				Size = size
			},
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		});
	}

	// =========================================================
	// Updates the label only when the stored quantity changes.
	private void UpdateReadout()
	{
		if (_label != null)
		{
			_label.Text = $"{Item.DisplayName}\n×{Amount:0.##}";
		}
	}

	#endregion

	#region Updates

	// =========================================================
	// Slowly rotates the visual while leaving the collection volume stationary.
	public override void _Process(double delta)
	{
		if (_visual != null)
		{
			_visual.RotateY(
				Mathf.DegToRad(SpinDegreesPerSecond) * (float)delta
			);
		}
	}

	// =========================================================
	// Retries collection at staggered intervals and arms dropped cargo safely.
	public override void _PhysicsProcess(double delta)
	{
		if (_consumed)
		{
			return;
		}

		float seconds = (float)delta;

		_armingRemaining = Mathf.Max(0.0f, _armingRemaining - seconds);
		_retryRemaining -= seconds;

		if (_retryRemaining > 0.0f)
		{
			return;
		}

		_retryRemaining = Mathf.Max(0.05f, RetryInterval);

		if (!_ownerCleared)
		{
			float clearance = CollectionRadius + 6.0f;

			if (!GodotObject.IsInstanceValid(_dropOwner)
				|| _dropOwner.GlobalPosition.DistanceSquaredTo(GlobalPosition)
					> clearance * clearance)
			{
				_ownerCleared = true;
			}
		}

		foreach (Node3D body in GetOverlappingBodies())
		{
			if (body is PlayerShip ship)
			{
				TryCollect(ship);
			}

			if (_consumed)
			{
				break;
			}
		}
	}

	#endregion

	#region Collection

	// =========================================================
	// Attempts immediate collection when a player enters the collection volume.
	private void OnBodyEntered(Node3D body)
	{
		if (body is PlayerShip ship)
		{
			TryCollect(ship);
		}
	}

	// =========================================================
	// Transfers only the amount accepted by cargo and preserves any remainder.
	private void TryCollect(PlayerShip ship)
	{
		if (_consumed
			|| _collecting
			|| _armingRemaining > 0.0f
			|| !ship.IsCombatTargetable
			|| ship.CockpitInteractionActive
			|| Input.MouseMode != Input.MouseModeEnum.Captured
			|| (ship == _dropOwner && !_ownerCleared))
		{
			return;
		}

		CargoHold cargo = ship.GetNodeOrNull<CargoHold>("Cargo");

		if (cargo == null)
		{
			return;
		}

		_collecting = true;

		try
		{
			float accepted = cargo.Add(Item.Key, Amount);

			if (accepted <= 0.0f)
			{
				return;
			}

			Amount = Mathf.Max(0.0f, Amount - accepted);

			GD.Print($"Collected {Item.DisplayName}: {accepted:0.##}");

			if (Amount <= 0.0f)
			{
				_consumed = true;
				Hide();
				SetProcess(false);
				SetPhysicsProcess(false);
				QueueFree();
			}
			else
			{
				UpdateReadout();
			}
		}
		finally
		{
			_collecting = false;
		}
	}

	#endregion

	#region Dropping

	// =========================================================
	// Releases one selected stack into the current sector without deleting its items.
	public static bool DropFromCargo(
		PlayerShip ship,
		CargoHold cargo,
		int slotIndex
	)
	{
		if (!GodotObject.IsInstanceValid(ship)
			|| !ship.IsCombatTargetable
			|| !GodotObject.IsInstanceValid(cargo))
		{
			return false;
		}

		CargoSlot slot = cargo.GetSlot(slotIndex);

		if (slot.IsEmpty)
		{
			return false;
		}

		Node parent = WorldSector.GetContentParent(ship);

		if (!GodotObject.IsInstanceValid(parent)
			|| parent.IsQueuedForDeletion())
		{
			return false;
		}

		// Place the container behind the hull, with a little vertical clearance.
		Vector3 position = ship.GlobalPosition
			+ ship.GlobalBasis.Z.Normalized() * 8.0f
			+ ship.GlobalBasis.Y.Normalized() * 1.5f;

		if (!IsDropPositionClear(ship, position))
		{
			GD.Print("Cargo drop blocked. Move away from nearby structures.");
			return false;
		}

		CargoPickup pickup = new()
		{
			Name = "CargoPickup",
			Item = slot.Item,
			Amount = slot.Amount,
			_dropOwner = ship,
			_ownerCleared = false,
			_armingRemaining = 2.0f
		};

		// Convert world placement into the owning sector's local space before adding.
		pickup.Position = parent is Node3D spatial
			? spatial.ToLocal(position)
			: position;

		parent.AddChild(pickup);

		float removed = cargo.RemoveFromSlot(slotIndex, slot.Amount);

		if (removed <= 0.0f)
		{
			pickup.QueueFree();
			return false;
		}

		pickup.Amount = removed;
		pickup.UpdateReadout();

		return true;
	}

	// =========================================================
	// Rejects blocked release paths and positions inside solid world geometry.
	private static bool IsDropPositionClear(PlayerShip ship, Vector3 position)
	{
		Godot.Collections.Array<Rid> excluded = new()
		{
			ship.GetRid()
		};

		PhysicsDirectSpaceState3D space =
			ship.GetWorld3D().DirectSpaceState;

		PhysicsRayQueryParameters3D ray = new()
		{
			From = ship.GlobalPosition,
			To = position,
			CollisionMask = 1,
			CollideWithBodies = true,
			CollideWithAreas = false,
			Exclude = excluded
		};

		if (space.IntersectRay(ray).Count > 0)
		{
			return false;
		}

		PhysicsShapeQueryParameters3D shape = new()
		{
			Shape = new SphereShape3D
			{
				Radius = 0.8f
			},
			Transform = new Transform3D(Basis.Identity, position),
			CollisionMask = 1,
			CollideWithBodies = true,
			CollideWithAreas = false,
			Exclude = excluded
		};

		return space.IntersectShape(shape, 1).Count == 0;
	}

	#endregion
}
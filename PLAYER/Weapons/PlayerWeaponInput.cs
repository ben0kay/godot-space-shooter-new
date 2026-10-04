using Godot;

// Routes primary/secondary firing and cycles the secondary slot through assigned weapons.
public partial class PlayerWeaponInput : Node
{
	#region Slots

	[Export] public WeaponMount PrimaryWeapon;
	[Export] public WeaponMount SecondaryWeapon;

	#endregion

	#region Secondary Equipment

	[Export] public Godot.Collections.Array<WeaponDefinition>
		SecondaryOptions = new();

	[Export] public int StartingSecondaryIndex = 0;

	public int SecondaryIndex { get; private set; } = -1;

	#endregion

	#region Controls

	[Export] public MouseButton PrimaryFireButton = MouseButton.Left;
	[Export] public MouseButton SecondaryFireButton = MouseButton.Middle;
	[Export] public Key CycleSecondaryKey = Key.B;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private bool _hasFocus = true;

	#endregion

	#region Setup

// =========================================================
// Finds the owning player through organisational containers
// and equips the configured starting secondary weapon.
// =========================================================
public override void _Ready()
{
	ProcessPhysicsPriority = 20;

	_ship = NodeHelpers.FindAncestor<PlayerShip>(this);

	if (_ship == null)
	{
		GD.PushError("PlayerWeaponInput needs a PlayerShip ancestor.");
		SetPhysicsProcess(false);
		SetProcessInput(false);
		return;
	}

	if (SecondaryOptions.Count > 0)
	{
		int start = Mathf.Clamp(
			StartingSecondaryIndex,
			0,
			SecondaryOptions.Count - 1
		);

		SelectSecondary(start);
	}
}

	#endregion

	#region Input

	// =========================================================
	// Cycles the secondary weapon once per key press during active gameplay.
	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey key
			|| !key.Pressed
			|| key.Echo
			|| key.PhysicalKeycode != CycleSecondaryKey
			|| !CanUseWeapons())
		{
			return;
		}

		CycleSecondary();
	}

	// =========================================================
	// Updates both firing slots independently.
	public override void _PhysicsProcess(double delta)
	{
		bool canFire = CanUseWeapons();

		UpdateSlot(
			PrimaryWeapon,
			canFire && Input.IsMouseButtonPressed(PrimaryFireButton)
		);

		UpdateSlot(
			SecondaryWeapon,
			canFire && Input.IsMouseButtonPressed(SecondaryFireButton)
		);
	}

	// =========================================================
	// Checks focus, mouse capture, and the player's combat state.
	private bool CanUseWeapons()
	{
		return _hasFocus
			&& Input.MouseMode == Input.MouseModeEnum.Captured
			&& GodotObject.IsInstanceValid(_ship)
			&& _ship.IsCombatTargetable;
	}

	// =========================================================
	// Updates a weapon mount that may currently be unassigned.
	private void UpdateSlot(WeaponMount mount, bool held)
	{
		if (GodotObject.IsInstanceValid(mount))
		{
			mount.SetTriggerHeld(held);
		}
	}

	// =========================================================
	// Stops firing immediately when the application loses focus.
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut)
		{
			_hasFocus = false;
			StopWeapons();
		}
		else if (what == NotificationApplicationFocusIn)
		{
			_hasFocus = true;
		}
	}

	// =========================================================
	// Releases weapons when this controller leaves the scene.
	public override void _ExitTree()
	{
		StopWeapons();
	}

	// =========================================================
	// Releases both weapon slots.
	private void StopWeapons()
	{
		UpdateSlot(PrimaryWeapon, false);
		UpdateSlot(SecondaryWeapon, false);
	}

	#endregion

	#region Equipment

	// =========================================================
	// Equips a new definition in the primary slot.
	public void EquipPrimary(WeaponDefinition weapon)
	{
		if (GodotObject.IsInstanceValid(PrimaryWeapon))
		{
			PrimaryWeapon.EquipWeapon(weapon);
		}
	}

	// =========================================================
	// Equips a secondary definition and records its index when present in the list.
	public void EquipSecondary(WeaponDefinition weapon)
	{
		if (!GodotObject.IsInstanceValid(SecondaryWeapon))
		{
			return;
		}

		SecondaryWeapon.EquipWeapon(weapon);
		SecondaryIndex = SecondaryOptions.IndexOf(weapon);
	}

	// =========================================================
	// Selects one valid weapon from the configured secondary list.
	public void SelectSecondary(int index)
	{
		if (index < 0
			|| index >= SecondaryOptions.Count
			|| SecondaryOptions[index] == null)
		{
			return;
		}

		EquipSecondary(SecondaryOptions[index]);
	}

	// =========================================================
	// Advances through available secondary weapons while skipping empty entries.
	public void CycleSecondary()
	{
		int count = SecondaryOptions.Count;

		if (count == 0)
		{
			return;
		}

		for (int step = 1; step <= count; step++)
		{
			int index = (SecondaryIndex + step) % count;

			if (SecondaryOptions[index] != null)
			{
				SelectSecondary(index);
				return;
			}
		}
	}

	#endregion
}

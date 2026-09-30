using Godot;

// Routes independent primary and secondary fire inputs to swappable weapon mounts.
public partial class PlayerWeaponInput : Node
{
	#region Slots

	[Export] public WeaponMount PrimaryWeapon;
	[Export] public WeaponMount SecondaryWeapon;

	#endregion

	#region Controls

	[Export] public MouseButton PrimaryFireButton = MouseButton.Left;
	[Export] public MouseButton SecondaryFireButton = MouseButton.Middle;

	#endregion

	#region Runtime

	private PlayerShip _ship;
	private bool _hasFocus = true;

	#endregion

	#region Setup

	// =========================================================
	// Caches the owning player ship.
	public override void _Ready()
	{
		_ship = GetParent() as PlayerShip;

		if (_ship == null)
		{
			GD.PushError("PlayerWeaponInput must be a child of PlayerShip.");
			SetPhysicsProcess(false);
		}
	}

	#endregion

	#region Input

	// =========================================================
	// Updates each weapon slot independently while gameplay input is active.
	public override void _PhysicsProcess(double delta)
	{
		bool canFire =
			_hasFocus
			&& Input.MouseMode == Input.MouseModeEnum.Captured
			&& GodotObject.IsInstanceValid(_ship)
			&& _ship.IsCombatTargetable;

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
	// Safely updates a slot that may currently be empty.
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
	// Releases held weapons when this input controller leaves the scene.
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
	// Equips a new definition in the primary weapon slot.
	public void EquipPrimary(WeaponDefinition weapon)
	{
		if (GodotObject.IsInstanceValid(PrimaryWeapon))
		{
			PrimaryWeapon.EquipWeapon(weapon);
		}
	}

	// =========================================================
	// Equips a new definition in the secondary weapon slot.
	public void EquipSecondary(WeaponDefinition weapon)
	{
		if (GodotObject.IsInstanceValid(SecondaryWeapon))
		{
			SecondaryWeapon.EquipWeapon(weapon);
		}
	}

	#endregion
}

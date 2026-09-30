using Godot;

public partial class PlayerWeaponInput : Node
{
	#region Settings

	[Export] public WeaponMount PrimaryWeapon;
	[Export] public MouseButton PrimaryFireButton = MouseButton.Left;

	#endregion

	#region Godot Events

	// Checks that the player's primary weapon has been assigned.
	public override void _Ready()
	{
		if (PrimaryWeapon == null)
		{
			GD.PushError("Assign PrimaryWeapon to PlayerWeaponInput.");
			SetPhysicsProcess(false);
		}
	}

	// Requests shots while the player holds the primary fire button.
	public override void _PhysicsProcess(double delta)
	{
		if (Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			return;
		}

		if (Input.IsMouseButtonPressed(PrimaryFireButton))
		{
			PrimaryWeapon.TryFire();
		}
	}

	#endregion
}

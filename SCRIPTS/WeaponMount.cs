using System.Collections.Generic;
using Godot;

public partial class WeaponMount : Node3D
{
	#region Settings

	[Export] public WeaponDefinition Weapon;
	[Export] public MouseButton FireButton = MouseButton.Left;

	#endregion

	#region Runtime

	private readonly List<Marker3D> _muzzles = new();
	private CharacterBody3D _ship;
	private int _muzzleIndex;
	private float _cooldown;

	#endregion

	#region Godot Events

	// Caches the ship and this mount's muzzle markers once.
	public override void _Ready()
	{
		_ship = GetParent<CharacterBody3D>();

		foreach (Node child in GetChildren())
		{
			if (child is Marker3D muzzle)
			{
				_muzzles.Add(muzzle);
			}
		}

		if (Weapon == null || Weapon.ProjectileScene == null || _muzzles.Count == 0)
		{
			GD.PushError("WeaponMount needs a weapon definition, projectile scene, and muzzle markers.");
			SetPhysicsProcess(false);
		}
	}

	// Fires at the weapon's configured rate while its mouse button is held.
	public override void _PhysicsProcess(double delta)
	{
		_cooldown = Mathf.Max(0.0f, _cooldown - (float)delta);

		if (Input.MouseMode != Input.MouseModeEnum.Captured
			|| !Input.IsMouseButtonPressed(FireButton)
			|| _cooldown > 0.0f)
		{
			return;
		}

		FireShot();

		_cooldown = Mathf.Max(0.01f, Weapon.SecondsBetweenShots);
	}

	#endregion

	#region Firing

	// Spawns one bullet at the current muzzle, then selects the next muzzle.
	private void FireShot()
	{
		Marker3D muzzle = _muzzles[_muzzleIndex];
		_muzzleIndex = (_muzzleIndex + 1) % _muzzles.Count;

		Projectile projectile = Weapon.ProjectileScene.Instantiate<Projectile>();

		GetTree().CurrentScene.AddChild(projectile);

		projectile.GlobalTransform = muzzle.GlobalTransform;
		projectile.Configure(Weapon, _ship, _ship.Velocity);
	}

	#endregion
}

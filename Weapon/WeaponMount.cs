using System.Collections.Generic;
using Godot;

public partial class WeaponMount : Node3D
{
	#region Settings

	[Export] public WeaponDefinition Weapon;
	[Export] public Faction ShooterFaction = Faction.Player;

	#endregion

	#region Runtime

	private readonly List<Marker3D> _muzzles = new();
	private CharacterBody3D _ship;
	private int _muzzleIndex;
	private float _cooldown;
	private bool _configured;

	#endregion

	#region Godot Events

	// Caches the firing ship and its muzzle markers in scene-tree order.
	public override void _Ready()
	{
		_ship = GetParent() as CharacterBody3D;

		foreach (Node child in GetChildren())
		{
			if (child is Marker3D muzzle)
			{
				_muzzles.Add(muzzle);
			}
		}

		_configured =
			_ship != null
			&& Weapon != null
			&& Weapon.ProjectileScene != null
			&& _muzzles.Count > 0;

		if (!_configured)
		{
			GD.PushError(
				"WeaponMount needs a CharacterBody3D parent, weapon definition, " +
                "projectile scene, and at least one muzzle marker."
			);

			SetPhysicsProcess(false);
		}
	}

	// Counts down the time until another shot is allowed.
	public override void _PhysicsProcess(double delta)
	{
		_cooldown = Mathf.Max(0.0f, _cooldown - (float)delta);
	}

	#endregion

	#region Firing

	// Attempts one shot and returns whether it fired.
	public bool TryFire()
	{
		if (!_configured || _cooldown > 0.0f)
		{
			return false;
		}

		FireShot();
		_cooldown = Mathf.Max(0.01f, Weapon.SecondsBetweenShots);

		return true;
	}

	// Spawns one projectile and advances to the next muzzle.
	private void FireShot()
	{
		Marker3D muzzle = _muzzles[_muzzleIndex];
		_muzzleIndex = (_muzzleIndex + 1) % _muzzles.Count;

		Projectile projectile = Weapon.ProjectileScene.Instantiate<Projectile>();

		GetTree().CurrentScene.AddChild(projectile);

		projectile.GlobalTransform = muzzle.GlobalTransform;
		projectile.Configure(Weapon, _ship, ShooterFaction, _ship.Velocity);
	}

	#endregion
}

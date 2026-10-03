using Godot;

// Connects an enemy's unique key, definition, and spawn scene.
[GlobalClass]
public partial class EnemyRegistration : Resource
{
	#region Registration

	[Export] public string Key = "";
	[Export] public EnemyDefinition Definition;
	[Export] public PackedScene SpawnScene;

	public string DisplayName => Definition?.DisplayName ?? Key;

	public bool IsValid =>
		!string.IsNullOrWhiteSpace(Key)
		&& Definition != null
		&& Definition.Defence != null
		&& SpawnScene != null;

	#endregion

	#region Creation

	// =========================================================
	// Creates an unparented enemy and applies the registered definition
	// before its Ready method builds the ship.
	// =========================================================
	public EnemyShip CreateInstance()
	{
		if (!IsValid)
		{
			GD.PushError($"Invalid enemy registration: '{Key}'.");
			return null;
		}

		Node instance = SpawnScene.Instantiate();

		if (instance is not EnemyShip enemy)
		{
			GD.PushError($"Enemy '{Key}' needs an EnemyShip scene root.");
			instance.Free();
			return null;
		}

		enemy.Definition = Definition;
		return enemy;
	}

	#endregion
}

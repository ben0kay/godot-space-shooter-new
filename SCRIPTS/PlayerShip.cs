using Godot;

public partial class PlayerShip : CharacterBody3D
{
	public override void _Ready()
	{
		CreateBox(
			"Hull",
			new Vector3(0, 0, 0),
			new Vector3(2, 0.5f, 3),
			Colors.SteelBlue
		);

		CreateBox(
			"Nose",
			new Vector3(0, 0, -1.7f),
			new Vector3(0.8f, 0.35f, 1),
			Colors.OrangeRed
		);

		BoxShape3D shape = new BoxShape3D();
		shape.Size = new Vector3(2, 0.5f, 3);

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = shape;
		AddChild(collision);
	}

	private void CreateBox(string name, Vector3 position, Vector3 size, Color color)
	{
		BoxMesh mesh = new BoxMesh();
		mesh.Size = size;

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Name = name;
		visual.Position = position;
		visual.Mesh = mesh;
		AddChild(visual);
	}
}

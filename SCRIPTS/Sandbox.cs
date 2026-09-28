using Godot;

public partial class Sandbox : Node3D
{
	public override void _Ready()
{
	CreateBox("Floor", new Vector3(0, -0.5f, 0), new Vector3(80, 1, 80), Colors.DarkSlateGray);
	CreateBox("NorthWall", new Vector3(0, 4, -40), new Vector3(80, 8, 1), Colors.DimGray);
	CreateBox("SouthWall", new Vector3(0, 4, 40), new Vector3(80, 8, 1), Colors.DimGray);
	CreateBox("WestWall", new Vector3(-40, 4, 0), new Vector3(1, 8, 80), Colors.DimGray);
	CreateBox("EastWall", new Vector3(40, 4, 0), new Vector3(1, 8, 80), Colors.DimGray);

	CreateBox("ReferenceBlock", new Vector3(0, 1, -12), new Vector3(2, 2, 2), Colors.OrangeRed);

	DirectionalLight3D light = new DirectionalLight3D();
	light.RotationDegrees = new Vector3(-45, -30, 0);
	AddChild(light);
}

	private void CreateBox(string name, Vector3 position, Vector3 size, Color color)
	{
		StaticBody3D body = new StaticBody3D();
		body.Name = name;
		body.Position = position;
		AddChild(body);

		BoxMesh mesh = new BoxMesh();
		mesh.Size = size;

		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoColor = color;
		mesh.Material = material;

		MeshInstance3D visual = new MeshInstance3D();
		visual.Mesh = mesh;
		body.AddChild(visual);

		BoxShape3D shape = new BoxShape3D();
		shape.Size = size;

		CollisionShape3D collision = new CollisionShape3D();
		collision.Shape = shape;
		body.AddChild(collision);
	}
}

using Godot;

public partial class PlayerShip : CharacterBody3D
{
	[Export] public float ForwardSpeed = 18.0f;
	[Export] public float ReverseSpeed = 8.0f;
	[Export] public float Acceleration = 24.0f;
	[Export] public float Deceleration = 18.0f;
	[Export] public float TurnSpeed = 120.0f;

	public override void _Ready()
	{
		CreateBox("Hull", Vector3.Zero, new Vector3(2, 0.5f, 3), Colors.SteelBlue);
		CreateBox("Nose", new Vector3(0, 0, -1.7f), new Vector3(0.8f, 0.35f, 1), Colors.OrangeRed);

		BoxShape3D shape = new BoxShape3D();
		shape.Size = new Vector3(2, 0.5f, 3);

		CollisionShape3D collision = new CollisionShape3D();
		collision.Name = "Collision";
		collision.Shape = shape;
		AddChild(collision);
	}

	public override void _PhysicsProcess(double delta)
	{
		float seconds = (float)delta;

		UpdateTurning(seconds);
		UpdateMovement(seconds);
	}

	private void UpdateTurning(float seconds)
	{
		float turn = 0.0f;

		if (Input.IsPhysicalKeyPressed(Key.Left)) turn -= 1.0f;
		if (Input.IsPhysicalKeyPressed(Key.Right)) turn += 1.0f;

		RotateY(Mathf.DegToRad(-turn * TurnSpeed * seconds));
	}

	private void UpdateMovement(float seconds)
	{
		float thrust = 0.0f;

		if (Input.IsPhysicalKeyPressed(Key.Up)) thrust += 1.0f;
		if (Input.IsPhysicalKeyPressed(Key.Down)) thrust -= 1.0f;

		float speed = thrust >= 0.0f ? ForwardSpeed : ReverseSpeed;
		Vector3 targetVelocity = -GlobalBasis.Z * thrust * speed;

		float response = thrust == 0.0f ? Deceleration : Acceleration;
		Velocity = Velocity.MoveToward(targetVelocity, response * seconds);

		MoveAndSlide();
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

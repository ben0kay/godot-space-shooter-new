using Godot;

public partial class PlayerThrusters : Node3D
{
	#region Settings

	[Export] public int ParticlesPerEngine = 35;
	[Export] public float ParticleLifetime = 0.3f;
	[Export] public float ExhaustSpeed = 6.0f;
	[Export] public Color ExhaustColor = new Color(1.0f, 0.45f, 0.08f);

	#endregion

	#region Setup

	// Finds the visually positioned engine markers and adds an emitter to each.
	public override void _Ready()
	{
		Marker3D leftEngine = GetNode<Marker3D>("LeftEngine");
		Marker3D rightEngine = GetNode<Marker3D>("RightEngine");

		CreateEmitter(leftEngine);
		CreateEmitter(rightEngine);
	}

	#endregion

	#region Particles

	// Attaches a glowing exhaust stream directly to one engine marker.
	private void CreateEmitter(Marker3D marker)
	{
		ParticleProcessMaterial process = new ParticleProcessMaterial();
		process.Direction = Vector3.Back;
		process.Spread = 10.0f;
		process.InitialVelocityMin = ExhaustSpeed * 0.75f;
		process.InitialVelocityMax = ExhaustSpeed;
		process.Gravity = Vector3.Zero;

		StandardMaterial3D glow = new StandardMaterial3D();
		glow.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		glow.AlbedoColor = ExhaustColor;
		glow.EmissionEnabled = true;
		glow.Emission = ExhaustColor;

		SphereMesh particleMesh = new SphereMesh();
		particleMesh.Radius = 0.07f;
		particleMesh.Height = 0.14f;
		particleMesh.RadialSegments = 6;
		particleMesh.Rings = 4;
		particleMesh.Material = glow;

		GPUParticles3D emitter = new GPUParticles3D();
		emitter.Name = "ExhaustParticles";
		emitter.Amount = ParticlesPerEngine;
		emitter.Lifetime = ParticleLifetime;
		emitter.ProcessMaterial = process;
		emitter.DrawPass1 = particleMesh;
		emitter.LocalCoords = false;

		marker.AddChild(emitter);
		emitter.Emitting = true;
	}

	#endregion
}

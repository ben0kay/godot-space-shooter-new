using Godot;

[GlobalClass]
public partial class EnemyRangeStats : Resource
{
    [Export] public float Detection = 100.0f;
    [Export] public float Combat = 70.0f;
    [Export] public float BackAway = 25.0f;
    [Export] public float Forget = 130.0f;
}
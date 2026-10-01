using Godot;

// Stores shared faction behaviour settings.
// Runtime timers, decisions, and attempt counts belong to each ship.
[GlobalClass]
public partial class FactionDoctrine : Resource
{
    #region Alert

    [ExportGroup("Alert")]

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AlertChance = 0.0f;

    [Export] public bool AlertOnDetection = false;
    [Export] public bool AlertOnDamage = false;

    [Export(PropertyHint.Range, "0,10,1")]
    public int AlertMaxAttempts = 1;

    [Export] public float AlertCooldownSeconds = 5.0f;
    [Export] public float AlertMemorySeconds = 5.0f;

    #endregion

    #region Engagement

    [ExportGroup("Engagement")]

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float EngagementChance = 1.0f;

    [Export] public float RejectCooldownSeconds = 5.0f;

    [ExportGroup("Retargeting")]

    [Export] public float RetargetIntervalSeconds = 0.65f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float RetargetDistanceRatio = 0.65f;

    #endregion

    #region Critical Response

    [ExportGroup("Critical Response")]

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float CriticalResponseChance = 0.0f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float CriticalHullRatio = 0.1f;

    [Export(PropertyHint.Range, "0,10,1")]
    public int CriticalMaxAttempts = 1;

    [Export] public float CriticalCooldownSeconds = 5.0f;
    [Export] public float CriticalSpeedScale = 1.0f;

    [ExportGroup("Critical Response Weights")]

    [Export(PropertyHint.Range, "0,100,1")]
    public float FleeWeight = 100.0f;

    [Export(PropertyHint.Range, "0,100,1")]
    public float RetreatWeight = 0.0f;

    #endregion
}
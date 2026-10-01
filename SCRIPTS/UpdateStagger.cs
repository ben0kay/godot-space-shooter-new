using Godot;

// Shared timing helpers for staggering periodic instance updates.
public static class UpdateStagger
{
    #region Timing Helpers

    // =========================================================
    // Calculates an initial timer offset using the instance ID.
    // Slots controls how many timing groups share the interval.
    // =========================================================
    public static float Offset(
        GodotObject instance,
        float interval,
        int slots = 10
    )
    {
        if (!GodotObject.IsInstanceValid(instance) || interval <= 0.0f)
        {
            return 0.0f;
        }

        slots = Mathf.Max(1, slots);

        ulong slot = instance.GetInstanceId() % (ulong)slots;

        return (float)slot / slots * interval;
    }

    #endregion
}
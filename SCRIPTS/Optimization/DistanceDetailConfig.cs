// Shared distances and timing defaults for world-detail optimisation.
public static class DistanceDetailConfig
{
    public const float ManagerInterval = 0.05f;

    public const float NearCheckInterval = 0.25f;
    public const float ReducedCheckInterval = 0.5f;
    public const float DistantCheckInterval = 1.0f;

    public const float NearDistance = 180.0f;
    public const float DistantDistance = 450.0f;

    // Prevents repeated detail switching near distance boundaries.
    public const float DistanceHysteresis = 20.0f;

    // Keeps cosmetic updates active slightly beyond screen edges.
    public const float VisibilityPadding = 12.0f;
}
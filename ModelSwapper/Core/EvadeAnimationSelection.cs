namespace LastEpoch.ModelSwapper;

public static class EvadeAnimationSelection
{
    public static byte Resolve(bool local, int? pinnedBody, bool sentinelEvade, byte original, byte? target)
        => local && sentinelEvade && pinnedBody is 3 or 4 && target.HasValue
            ? target.Value : original;
}

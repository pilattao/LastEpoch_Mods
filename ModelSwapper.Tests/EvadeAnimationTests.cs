using LastEpoch.ModelSwapper;
using Xunit;

public sealed class EvadeAnimationTests
{
    // Catch applying the swap to another actor or to a different skill sharing its animation.
    [Theory]
    [InlineData(true, 3, true, (byte)28)]
    [InlineData(true, 4, true, (byte)28)]
    [InlineData(false, 3, true, (byte)30)]
    [InlineData(true, 3, false, (byte)30)]
    [InlineData(true, 2, true, (byte)30)]
    [InlineData(true, 0, true, (byte)30)]
    public void Only_local_swapped_sentinel_evade_uses_target_animation(
        bool local, int body, bool sentinelEvade, byte expected)
    {
        // Steam build 25711439 native assets: Sentinel=30, Acolyte/Rogue=28.
        Assert.Equal(expected, EvadeAnimationSelection.Resolve(local, body, sentinelEvade, 30, 28));
    }

    [Fact]
    public void Missing_target_asset_keeps_original_animation()
    {
        Assert.Equal((byte)30, EvadeAnimationSelection.Resolve(true, 3, true, 30, null));
    }

    [Fact]
    public void Body_preference_change_does_not_mix_animation_with_existing_rig()
    {
        var sessions = new VisualSessions();
        var actor = new IntPtr(10);
        sessions.Begin(actor, true, 2, VisualModel.Acolyte);
        // Another actor can load the newly selected preference without changing this rig.
        sessions.Begin(new IntPtr(20), true, 2, VisualModel.Rogue);
        Assert.Equal((byte)28, EvadeAnimationSelection.Resolve(true, sessions.Get(actor), true, 30, 28));
        sessions.Remove(actor);
        Assert.Equal((byte)30, EvadeAnimationSelection.Resolve(true, sessions.Get(actor), true, 30, 28));
    }
}

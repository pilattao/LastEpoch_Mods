using Xunit;

namespace LastEpoch.ModelSwapper.Tests;

public class HelmetVisibilityTests
{
    // Unity's GameObject boundary is the only fake: these tests exercise the real
    // visibility controller's changes to active flags, including asynchronous replacement.
    private sealed class Part(long id, bool active) : IVisualPart
    {
        public long Id => id;
        public bool Exists { get; set; } = true;
        public bool Active { get; set; } = active;
    }

    [Fact]
    public void HidingHelmetRestoresItsOriginalActiveFlag()
    {
        var controller = new HelmetVisibility();
        var helmet = new Part(1, true);
        controller.Apply(true, new[] { helmet });
        Assert.False(helmet.Active);
        controller.Apply(false, new[] { helmet });
        Assert.True(helmet.Active);
    }

    [Fact]
    public void ReapplyingDoesNotForgetWhichObjectsWereInitiallyInactive()
    {
        var controller = new HelmetVisibility();
        var helmet = new Part(1, false);
        controller.Apply(true, new[] { helmet });
        controller.Apply(true, new[] { helmet });
        controller.Restore();
        Assert.False(helmet.Active);
    }

    [Fact]
    public void NewlyLoadedHelmetIsHiddenWithoutRestoringDestroyedObjects()
    {
        var controller = new HelmetVisibility();
        var oldHelmet = new Part(1, true);
        controller.Apply(true, new[] { oldHelmet });
        oldHelmet.Exists = false;
        var replacement = new Part(3, true);
        controller.Apply(true, new[] { replacement });
        Assert.False(replacement.Active);
        controller.Restore();
        Assert.True(replacement.Active);
        Assert.False(oldHelmet.Active);
    }

    [Fact]
    public void TurningOffClearsSnapshotsBeforeTheNextHelmet()
    {
        var controller = new HelmetVisibility();
        var helmet = new Part(1, true);
        controller.Apply(true, new[] { helmet });
        controller.Restore();
        helmet.Active = false;
        controller.Apply(true, new[] { helmet });
        controller.Restore();
        Assert.False(helmet.Active);
    }
}

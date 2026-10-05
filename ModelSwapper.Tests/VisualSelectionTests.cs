using Xunit;

namespace LastEpoch.ModelSwapper.Tests;

public class VisualSelectionTests
{
    [Theory]
    [InlineData(true, 2, VisualModel.Rogue, 4)]
    [InlineData(true, 2, VisualModel.Acolyte, 3)]
    [InlineData(true, 2, VisualModel.Original, 2)]
    [InlineData(false, 2, VisualModel.Rogue, 2)]
    [InlineData(true, 0, VisualModel.Rogue, 0)]
    [InlineData(true, 1, VisualModel.Acolyte, 1)]
    [InlineData(true, 3, VisualModel.Rogue, 3)]
    [InlineData(true, 4, VisualModel.Acolyte, 4)]
    [InlineData(true, -1, VisualModel.Rogue, -1)]
    [InlineData(true, 2, (VisualModel)99, 2)]
    public void SubstitutesOnlyTheLocalSentinel(bool local, int realClass, VisualModel model, int expected)
        => Assert.Equal(expected, VisualSelection.Resolve(local, realClass, model));

    [Theory]
    [InlineData("rogue", VisualModel.Rogue)]
    [InlineData(" Acolyte ", VisualModel.Acolyte)]
    [InlineData("Original", VisualModel.Original)]
    [InlineData(null, VisualModel.Original)]
    [InlineData("typo", VisualModel.Original)]
    [InlineData("99", VisualModel.Original)]
    public void InvalidSettingsKeepTheOriginalBody(string? value, VisualModel expected)
        => Assert.Equal(expected, VisualSelection.Parse(value));

    [Fact]
    public void ModelChoiceStaysFixedUntilTheNextVisualCreation()
    {
        var sessions = new VisualSessions();
        var actor = new IntPtr(101);
        Assert.Equal(4, sessions.Begin(actor, true, 2, VisualModel.Rogue));
        Assert.Equal(4, sessions.Get(actor));
        Assert.Null(sessions.Get(new IntPtr(102)));
        Assert.Equal(3, sessions.Begin(actor, true, 2, VisualModel.Acolyte));
        Assert.Equal(3, sessions.Get(actor));
    }

    [Fact]
    public void DestroyedOrUnswappedActorsHaveNoEquipmentOverride()
    {
        var sessions = new VisualSessions();
        var actor = new IntPtr(101);
        sessions.Begin(actor, true, 2, VisualModel.Rogue);
        sessions.Remove(actor);
        Assert.Null(sessions.Get(actor));
        sessions.Begin(actor, true, 2, VisualModel.Rogue);
        sessions.Begin(actor, false, 2, VisualModel.Rogue);
        Assert.Null(sessions.Get(actor));
        sessions.Begin(actor, true, 2, VisualModel.Rogue);
        sessions.Begin(actor, true, 2, VisualModel.Original);
        Assert.Null(sessions.Get(actor));
    }

    [Fact]
    public void SceneChangesDropOldActorIdentities()
    {
        var sessions = new VisualSessions();
        sessions.Begin(new IntPtr(101), true, 2, VisualModel.Rogue);
        sessions.Clear();
        Assert.Null(sessions.Get(new IntPtr(101)));
    }

    [Theory]
    [InlineData(0, true, 123, 4, new[] { 0, 4, 16 }, 0, true, 123)]
    [InlineData(1, true, 123, 4, new[] { 0, 4, 16 }, 2, false, 0)]
    [InlineData(1, false, 0, 3, new[] { 0, 4, 16 }, 0, false, 0)]
    [InlineData(0, true, 123, 4, new[] { 20 }, 0, true, 123)]
    [InlineData(0, true, 123, 4, new[] { 4 }, 0, true, 123)]
    [InlineData(-1, true, 123, 4, new[] { 0 }, -1, true, 123)]
    [InlineData(5, true, 123, 4, new[] { 0 }, 5, true, 123)]
    public void UsesCompatibleArmorWithoutInventingAnotherUnique(int subtype, bool unique, int id,
        int model, int[] requirements, int expectedSubtype, bool expectedUnique, int expectedId)
    {
        var choice = GearSelection.Resolve(subtype, unique, (ushort)id, model, requirements);
        Assert.Equal(new GearChoice(expectedSubtype, expectedUnique, (ushort)expectedId), choice);
    }
}

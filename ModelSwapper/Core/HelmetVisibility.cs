using System.Collections.Generic;

namespace LastEpoch.ModelSwapper;

public interface IVisualPart
{
    long Id { get; }
    bool Exists { get; }
    bool Active { get; set; }
}

public sealed class HelmetVisibility
{
    private readonly Dictionary<long, (IVisualPart Part, bool Active)> snapshots = new();

    public void Apply(bool hide, IEnumerable<IVisualPart> helmet)
    {
        if (!hide) { Restore(); return; }
        foreach (var part in helmet)
        {
            if (!part.Exists) continue;
            if (!snapshots.ContainsKey(part.Id)) snapshots[part.Id] = (part, part.Active);
            part.Active = false;
        }
        var dead = new List<long>();
        foreach (var pair in snapshots)
            if (!pair.Value.Part.Exists) dead.Add(pair.Key);
        foreach (var id in dead) snapshots.Remove(id);
    }

    public void Restore()
    {
        foreach (var state in snapshots.Values)
            if (state.Part.Exists) state.Part.Active = state.Active;
        snapshots.Clear();
    }
}

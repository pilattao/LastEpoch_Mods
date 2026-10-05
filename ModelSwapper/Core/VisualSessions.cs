using System;
using System.Collections.Generic;

namespace LastEpoch.ModelSwapper;

public sealed class VisualSessions
{
    private readonly Dictionary<IntPtr, int> models = new();

    public int Begin(IntPtr actor, bool localPlayer, int realClass, VisualModel model)
    {
        var selected = VisualSelection.Resolve(localPlayer, realClass, model);
        if (selected != realClass && actor != IntPtr.Zero) models[actor] = selected;
        else models.Remove(actor);
        return selected;
    }

    public int? Get(IntPtr actor) => models.TryGetValue(actor, out var model) ? model : null;
    public void Remove(IntPtr actor) => models.Remove(actor);
    public void Clear() => models.Clear();
}

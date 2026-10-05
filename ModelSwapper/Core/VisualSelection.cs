using System;

namespace LastEpoch.ModelSwapper;

public enum VisualModel { Original, Rogue, Acolyte }

public static class VisualSelection
{
    public static int Resolve(bool localPlayer, int realClass, VisualModel model)
        => localPlayer && realClass == 2 ? model switch
        {
            VisualModel.Rogue => 4,
            VisualModel.Acolyte => 3,
            _ => realClass
        } : realClass;

    public static VisualModel Parse(string? value)
        => Enum.TryParse<VisualModel>(value?.Trim(), true, out var model)
            && Enum.IsDefined(typeof(VisualModel), model) ? model : VisualModel.Original;
}

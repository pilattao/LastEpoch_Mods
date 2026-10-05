using System.Collections.Generic;

namespace LastEpoch.ModelSwapper;

public readonly record struct GearChoice(int Subtype, bool IsUnique, ushort UniqueId);
public static class GearSelection
{
    // Requirements are class bitmasks; zero means usable by every class.
    public static GearChoice Resolve(int subtype, bool unique, ushort uniqueId,
        int visualClass, IReadOnlyList<int> requirements)
    {
        var original = new GearChoice(subtype, unique, uniqueId);
        if (subtype < 0 || subtype >= requirements.Count || visualClass < 0 || visualClass > 4)
            return original;
        var mask = 1 << visualClass;
        if (requirements[subtype] == 0 || (requirements[subtype] & mask) != 0) return original;
        // Prefer the selected class's armor, then a shared base. Only rendering arguments change.
        for (var i = 0; i < requirements.Count; i++)
            if ((requirements[i] & mask) != 0) return new(i, false, 0);
        for (var i = 0; i < requirements.Count; i++)
            if (requirements[i] == 0) return new(i, false, 0);
        return original;
    }
}

using Il2Cpp;
using UnityEngine;

namespace LastEpoch.ModelSwapper;

internal sealed class HelmetController
{
    private readonly HelmetVisibility visibility = new();
    private IMSlotManager? manager;
    private bool overriding;
    private bool failed;
    private string? lastReport;

    private sealed class Part(GameObject item) : IVisualPart
    {
        public long Id => item.Pointer.ToInt64();
        public bool Exists => item != null;
        public bool Active { get => item.activeSelf; set => item.SetActive(value); }
    }

    private static IEnumerable<IVisualPart> Parts(IMSlot slot)
    {
        var current = slot.currentItems;
        if (current == null) yield break;
        for (var i = 0; i < current.Count; i++)
            if (current[i] != null) yield return new Part(current[i]);
    }

    internal void Update(ActorVisuals? visuals, bool hide)
    {
        if (failed) return;
        try
        {
            var equipment = visuals?.equipmentVisualsManager;
            var eligible = visuals != null && ModelSwapperMod.IsLocal(visuals)
                && (ModelSwapperMod.Sessions.Get(visuals.Pointer).HasValue
                    || visuals.clientCharacterInfoProvider?.classID == 2
                    || PlayerFinder.getPlayerData()?.CharacterClass == 2);
            var next = eligible ? equipment?.SlotManager : null;
            if (next == null || manager == null || next.Pointer != manager.Pointer)
            {
                Restore();
                manager = next;
            }
            if (manager == null || equipment == null || !equipment.Initialized || equipment.IsLoadingAnything) return;
            var slot = manager.getSlot(IMSlotType.Helmet, false);
            if (slot == null) return;
            visibility.Apply(hide, Parts(slot));
            // These native routines reveal default hair/head and undo the helmet's skin mask.
            // Keep headConfig and equippedGearData intact so turning the toggle off restores
            // the CURRENT equipped appearance, even after changing helmets while hidden.
            if (hide)
            {
                slot.ShowDefaultGear(true);
                manager.bodyHiding?.ApplyDefaultConfigHead();
                overriding = true;
            }
            else if (overriding)
            {
                slot.ShowDefaultGear(slot.currentItems == null || slot.currentItems.Count == 0);
                manager.bodyHiding?.ApplyConfigHead();
                overriding = false;
            }
            var report = $"{manager.Pointer}:{hide}:{slot.currentItems?.Count}";
            if (report != lastReport)
            {
                lastReport = report;
                ModelSwapperMod.Instance.LoggerInstance.Msg($"Helmet visibility: hidden={hide}; visualObjects={slot.currentItems?.Count ?? 0}; inventory untouched.");
            }
        }
        catch (Exception error)
        {
            failed = true;
            Restore();
            ModelSwapperMod.Instance.LoggerInstance.Warning($"Helmet visibility disabled: {error.Message}");
        }
    }

    internal void Restore()
    {
        visibility.Restore();
        if (overriding && manager != null)
        {
            var slot = manager.getSlot(IMSlotType.Helmet, false);
            if (slot != null) slot.ShowDefaultGear(slot.currentItems == null || slot.currentItems.Count == 0);
            manager.bodyHiding?.ApplyConfigHead();
        }
        overriding = false;
        manager = null;
    }
}

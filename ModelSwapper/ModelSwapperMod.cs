using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(LastEpoch.ModelSwapper.ModelSwapperMod), "Sentinel Model Swapper", "0.1.0", "pilattao (based on RCInet)")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]
[assembly: HarmonyDontPatchAll]

namespace LastEpoch.ModelSwapper;

public sealed class ModelSwapperMod : MelonMod
{
    internal static ModelSwapperMod Instance = null!;
    internal static readonly VisualSessions Sessions = new();
    internal static readonly Dictionary<IntPtr, ClientCharacterInfoProvider> Providers = new();
    private MelonPreferences_Category preferences = null!;
    private MelonPreferences_Entry<string> model = null!;
    private bool showMenu;
    private bool ready;
    private IntPtr reportedActor;

    internal VisualModel Selected => VisualSelection.Parse(model?.Value);

    public override void OnInitializeMelon()
    {
        Instance = this;
        preferences = MelonPreferences.CreateCategory("SentinelModelSwapper");
        model = preferences.CreateEntry("Model", "Rogue", "Sentinel body", "Original, Rogue, or Acolyte. Applies on next area/character load.");
        try
        {
            HarmonyInstance.PatchAll(typeof(ModelSwapperMod).Assembly);
            var count = HarmonyInstance.GetPatchedMethods().Count();
            if (count != 5) throw new InvalidOperationException($"Expected 5 patched methods, found {count}.");
            ready = true;
            LoggerInstance.Msg($"Ready; 5/5 patches; body={Selected}. F8 opens the menu. Changes apply on the next area/character load.");
        }
        catch (Exception error)
        {
            HarmonyInstance.UnpatchSelf();
            Sessions.Clear();
            LoggerInstance.Error($"Model swapper disabled: {error}");
        }
    }

    public override void OnUpdate()
    {
        if (ready && Input.GetKeyDown(KeyCode.F8)) showMenu = !showMenu;
    }

    public override void OnGUI()
    {
        if (!ready || !showMenu) return;
        GUI.Box(new Rect(24, 100, 340, 205), "Sentinel Model Swapper / F8");
        GUI.Label(new Rect(40, 130, 310, 25), $"Next load: {Selected}");
        GUI.Label(new Rect(40, 155, 310, 25), "Choose a body, then enter another area.");
        DrawChoice(VisualModel.Rogue, 190);
        DrawChoice(VisualModel.Acolyte, 225);
        DrawChoice(VisualModel.Original, 260);
    }

    private void DrawChoice(VisualModel choice, float y)
    {
        if (!GUI.Button(new Rect(40, y, 308, 28), choice.ToString())) return;
        model.Value = choice.ToString();
        preferences.SaveToFile();
        LoggerInstance.Msg($"Body={choice}; applies on next area/character load.");
    }

    public override void OnLateUpdate()
    {
        // One readiness record per local actor; no character names, account IDs, or saves are logged.
        if (!ready) return;
        var visuals = PlayerFinder.playerActorVisuals;
        if (visuals == null || visuals.Pointer == reportedActor || !visuals.ClassVisualsCreated) return;
        reportedActor = visuals.Pointer;
        var target = Sessions.Get(visuals.Pointer);
        var data = PlayerFinder.getPlayerData();
        LoggerInstance.Msg($"Visuals created; requestedBody={target?.ToString() ?? "original"}; realClass={data?.CharacterClass.ToString() ?? "unavailable"}; equipmentReady={visuals.equipmentVisualsManager?.Initialized}; modelObject={visuals._classVisualsGo?.name ?? visuals._classVisualsGO?.name ?? "unavailable"}.");
    }

    internal static bool IsLocal(ActorVisuals visuals)
    {
        if (!visuals.isPlayer) return false;
        if (visuals.actorSync != null && visuals.actorSync.IsLocalPlayer) return true;
        var local = PlayerFinder.playerActorVisuals;
        return local != null && local.Pointer == visuals.Pointer;
    }

    internal static CharacterInfoProvider VisualProvider(ActorVisuals visuals, CharacterInfoProvider original, int target)
    {
        if (!Providers.TryGetValue(visuals.Pointer, out var copy) || copy == null)
        {
            // A separate native component preserves faction/level information without mutating
            // the shared gameplay provider. Unity destroys it with the owning visual actor.
            var holder = new GameObject("ModelSwapper.VisualInfo");
            holder.transform.SetParent(visuals.transform, false);
            copy = holder.AddComponent<ClientCharacterInfoProvider>();
            Providers[visuals.Pointer] = copy;
        }
        copy.classID = target;
        copy.level = original.getLevel();
        copy.subClassID = original.getSubClassID();
        copy.factionInfoProvider = original.getFactionInfoProvider();
        return copy.Cast<CharacterInfoProvider>();
    }

    internal static int? PrepareEquipment(EquipmentVisualsManager manager)
    {
        var visuals = manager.actorVisuals;
        if (visuals == null || !IsLocal(visuals)) return null;
        var target = Sessions.Get(visuals.Pointer);
        if (target.HasValue) manager.characterInfoProvider = visuals.GetCharacterInfoProvider();
        return target;
    }

    internal static GearChoice CompatibleGear(ItemList.BaseEquipmentItem? itemType, int subtype, bool unique, ushort id, int target)
    {
        if (itemType == null || itemType.subItems == null) return new(subtype, unique, id);
        var bases = itemType.subItems;
        var currentIndex = -1;
        var requirements = new int[bases.Count];
        for (var i = 0; i < bases.Count; i++)
        {
            requirements[i] = (int)bases[i].classRequirement;
            if (bases[i].subTypeID == subtype) currentIndex = i;
        }
        if (currentIndex < 0) return new(subtype, unique, id);
        var choice = GearSelection.Resolve(currentIndex, unique, id, target, requirements);
        return choice with { Subtype = bases[choice.Subtype].subTypeID };
    }
}

[HarmonyPatch(typeof(ActorVisuals), nameof(ActorVisuals.CreateClassVisuals))]
internal static class CreateClassVisualsPatch
{
    private static void Prefix(ActorVisuals __instance, ref int __0)
    {
        var local = ModelSwapperMod.IsLocal(__instance);
        var original = __0;
        __0 = ModelSwapperMod.Sessions.Begin(__instance.Pointer, local, original, ModelSwapperMod.Instance.Selected);
        if (__instance.isPlayer)
            ModelSwapperMod.Instance.LoggerInstance.Msg($"CreateClassVisuals: local={local}, realClass={original}, body={__0}.");
    }

    private static Exception? Finalizer(ActorVisuals __instance, Exception? __exception)
    {
        if (__exception != null) ModelSwapperMod.Sessions.Remove(__instance.Pointer);
        return __exception;
    }
}

[HarmonyPatch(typeof(ActorVisuals), nameof(ActorVisuals.GetCharacterInfoProvider))]
internal static class VisualProviderPatch
{
    private static void Postfix(ActorVisuals __instance, ref CharacterInfoProvider __result)
    {
        if (__result == null || !ModelSwapperMod.IsLocal(__instance)) return;
        var target = ModelSwapperMod.Sessions.Get(__instance.Pointer);
        if (target.HasValue) __result = ModelSwapperMod.VisualProvider(__instance, __result, target.Value);
    }
}

[HarmonyPatch(typeof(EquipmentVisualsManager), nameof(EquipmentVisualsManager.EquipGearAsync))]
internal static class GearPatch
{
    private static void Prefix(EquipmentVisualsManager __instance, EquipmentType __0, ref int __1, ref bool __2, ref ushort __3)
    {
        var target = ModelSwapperMod.PrepareEquipment(__instance);
        if (!target.HasValue) return;
        var items = ItemList.get();
        if (items == null) return;
        var choice = ModelSwapperMod.CompatibleGear(items.GetEquipmentBaseItem(__0), __1, __2, __3, target.Value);
        __1 = choice.Subtype;
        __2 = choice.IsUnique;
        __3 = choice.UniqueId;
    }
}

[HarmonyPatch(typeof(EquipmentVisualsManager), nameof(EquipmentVisualsManager.EquipWeaponAsync))]
internal static class WeaponPatch
{
    private static void Prefix(EquipmentVisualsManager __instance, int __0, ref int __1, ref int __2, ref ushort __3)
    {
        var target = ModelSwapperMod.PrepareEquipment(__instance);
        if (!target.HasValue || __0 < 0) return;
        var items = ItemList.get();
        if (items == null) return;
        var choice = ModelSwapperMod.CompatibleGear(items.GetEquipmentBaseItem(__0), __1, __2 > 6, __3, target.Value);
        if (choice.Subtype == __1) return;
        __1 = choice.Subtype;
        __3 = choice.UniqueId;
        if (!choice.IsUnique && __2 > 6) __2 = 2;
    }
}

[HarmonyPatch(typeof(ActorVisuals), nameof(ActorVisuals.OnDestroy))]
internal static class DestroyVisualsPatch
{
    private static void Prefix(ActorVisuals __instance)
    {
        ModelSwapperMod.Sessions.Remove(__instance.Pointer);
        ModelSwapperMod.Providers.Remove(__instance.Pointer);
    }
}

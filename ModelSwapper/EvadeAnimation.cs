using HarmonyLib;
using Il2Cpp;

namespace LastEpoch.ModelSwapper;

[HarmonyPatch(typeof(ClientUsingAbilityState), nameof(ClientUsingAbilityState.startAbilityAnimationAndCastingVFX))]
internal static class EvadeAnimationPatch
{
    private static readonly HashSet<IntPtr> reported = new();
    private static bool warned;

    private static void Prefix(ClientUsingAbilityState __instance, Ability __0, ref AbilityAnimation __1)
    {
        try
        {
            var visuals = __instance.actorVisuals;
            if (visuals == null || __0 == null || !ModelSwapperMod.IsLocal(visuals)) return;
            var body = ModelSwapperMod.Sessions.Get(visuals.Pointer);
            if (body is not (3 or 4) || !__0.TryGetAbilityID(out var id) || id != AbilityID.sentinelEvade) return;

            // Read the native target ability without changing the equipped ability, its
            // serialized asset, CastingData, movement, cooldown, or gameplay class.
            var targetId = body == 3 ? AbilityID.acolyteEvade : AbilityID.rogueEvade;
            var target = Ability.getAbility(targetId);
            var original = __1;
            byte? targetAnimation = target != null ? (byte)target.getAbilityAnimation() : null;
            __1 = (AbilityAnimation)EvadeAnimationSelection.Resolve(true, body, true, (byte)__1, targetAnimation);
            if (reported.Add(visuals.Pointer))
                ModelSwapperMod.Instance.LoggerInstance.Msg($"Evade animation: ability={id}; body={body}; animation={original}->{__1}; targetAsset={targetId}; available={target != null}. Movement and cooldown untouched.");
        }
        catch (Exception error)
        {
            // Keep the original animation if target assets cannot be resolved.
            if (warned) return;
            warned = true;
            ModelSwapperMod.Instance.LoggerInstance.Warning($"Evade animation adaptation unavailable: {error.Message}");
        }
    }

    internal static void Remove(IntPtr actor) => reported.Remove(actor);
    internal static void Clear() => reported.Clear();
}

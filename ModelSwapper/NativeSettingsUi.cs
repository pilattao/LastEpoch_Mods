using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastEpoch.ModelSwapper;

internal static class NativeSettingsUi
{
    private const string Category = "ModsCategory - Sentinel Appearance";

    internal static void Inject(SettingsPanelTabNavigable panel)
    {
        Transform? category = null;
        var newRows = new List<GameObject>();
        try
        {
            // Same native Gameplay-settings templates used by Terrible fog_OF_war.
            var root = panel.transform.GetChild(0).GetChild(0);
            var template = root.Find("Toogle - Minion Health Bars");
            var anchor = root.Find("Header - Interface");
            if (template == null || anchor == null || root.Find(Category) != null) return;

            category = Object.Instantiate(anchor, root);
            category.name = Category;
            category.SetSiblingIndex(anchor.GetSiblingIndex());
            RemoveLocalization(category);
            category.GetComponentInChildren<TMP_Text>().text = "Sentinel Appearance";
            var choices = new List<(VisualModel Model, Toggle Toggle)>();
            var index = category.GetSiblingIndex() + 1;

            foreach (var model in new[] { VisualModel.Rogue, VisualModel.Acolyte, VisualModel.Original })
            {
                var value = model;
                var row = MakeRow(template, root, index++, "ModelSwapper - " + model,
                    "Body: " + model, "Applies on the next area or character load.");
                newRows.Add(row);
                var toggle = PrepareToggle(row);
                choices.Add((model, toggle));
                toggle.SetIsOnWithoutNotify(ModelSwapperMod.Instance.Selected == model);
                toggle.onValueChanged.AddListener((UnityAction<bool>)(Action<bool>)(_ =>
                {
                    ModelSwapperMod.Instance.SelectModel(value);
                    foreach (var choice in choices)
                        if (choice.Toggle != null) choice.Toggle.SetIsOnWithoutNotify(choice.Model == value);
                }));
            }

            var helmetRow = MakeRow(template, root, index, "ModelSwapper - Hide helmet",
                "Hide helmet", "Visual only. Your equipped helmet keeps its stats. Applies immediately.");
            newRows.Add(helmetRow);
            var helmetToggle = PrepareToggle(helmetRow);
            helmetToggle.SetIsOnWithoutNotify(ModelSwapperMod.Instance.HideHelmet);
            helmetToggle.onValueChanged.AddListener((UnityAction<bool>)(Action<bool>)(hide =>
                ModelSwapperMod.Instance.SelectHelmetVisibility(hide)));
            ModelSwapperMod.Instance.LoggerInstance.Msg("Settings inserted: Gameplay > Sentinel Appearance (4 controls).");
        }
        catch (Exception error)
        {
            foreach (var row in newRows) if (row != null) Object.Destroy(row);
            if (category != null) Object.Destroy(category.gameObject);
            ModelSwapperMod.Instance.LoggerInstance.Warning($"Native settings unavailable: {error.Message}. Preferences remain in UserData/MelonPreferences.cfg.");
        }
    }

    private static GameObject MakeRow(Transform template, Transform root, int index,
        string name, string title, string description)
    {
        var row = Object.Instantiate(template, root);
        row.name = name;
        row.SetSiblingIndex(index);
        RemoveLocalization(row);
        row.GetComponentInChildren<TMP_Text>().text = title + "\n<size=62%><color=#AAAAAA>" + description + "</color></size>";
        return row.gameObject;
    }

    private static Toggle PrepareToggle(GameObject row)
    {
        var toggle = row.GetComponentInChildren<Toggle>();
        if (toggle == null) throw new InvalidOperationException("Native checkbox template has no Toggle");
        // RemoveAllListeners leaves Inspector-persistent callbacks attached. Replace
        // the event so this cloned checkbox cannot change the source game's setting.
        toggle.onValueChanged = new Toggle.ToggleEvent();
        toggle.group = null;
        var button = row.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener((UnityAction)(Action)(() => toggle.isOn = !toggle.isOn));
        }
        return toggle;
    }

    private static void RemoveLocalization(Transform row)
    {
        foreach (var localizer in row.GetComponentsInChildren<LocalizeStringEvent>(true))
            Object.DestroyImmediate(localizer);
    }
}

[HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
internal static class SettingsPatch
{
    private static void Postfix(SettingsPanelTabNavigable __instance) => NativeSettingsUi.Inject(__instance);
}

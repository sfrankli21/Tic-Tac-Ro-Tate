using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MFDButtonOutcome
{
    [Header("Menu Logic")]
    public string presetName;                   // Only triggers this outcome if current menu matches
    public bool changeMenuOnPress = false;
    public string menuNameToSelect = "";        // Instead of index, we use a name

    [Header("Event Outputs")]
    public UnityEvent onButtonPress;
}

public class MFDButton : MonoBehaviour
{
    public MFDManager manager;
    public List<MFDButtonOutcome> outcomes = new();

    public void Output()
    {
        if (manager == null) return;

        string currentPreset = manager.GetCurrentPresetName();

        foreach (var outcome in outcomes)
        {
            if (outcome.presetName != currentPreset) continue;

            outcome.onButtonPress?.Invoke();

            if (outcome.changeMenuOnPress && !string.IsNullOrEmpty(outcome.menuNameToSelect))
            {
                manager.SelectMenuByName(outcome.menuNameToSelect);
            }

            break;
        }
    }
}

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(UIButton), true)]
[CanEditMultipleObjects]
public class UIButtonEditor : SelectableEditor
{
    private SerializedProperty m_OnClickProperty;
    private SerializedProperty outputTypeProperty;
    private SerializedProperty audioBackendProperty;
    private SerializedProperty unityAudioSourceProperty;

    private SerializedProperty clickAudioProperty;

    private SerializedProperty hoverButtonEnabledProperty;
    private SerializedProperty onHoverButtonEnterProperty;
    private SerializedProperty onHoverButtonExitProperty;
    private SerializedProperty hoverEnterAudioProperty;
    private SerializedProperty hoverExitAudioProperty;
    private SerializedProperty hoverScaleEnabledProperty;
    private SerializedProperty hoverScaleTargetProperty;
    private SerializedProperty hoverTargetScaleProperty;
    private SerializedProperty hoverScaleInDurationProperty;
    private SerializedProperty hoverScaleOutDurationProperty;
    private SerializedProperty useUnscaledTimeForHoverScaleProperty;

    private SerializedProperty holdButtonEnabledProperty;
    private SerializedProperty holdEnterDelayProperty;
    private SerializedProperty holdLogicFrequencyProperty;
    private SerializedProperty onHoldButtonEnterProperty;
    private SerializedProperty onHoldButtonLogicProperty;
    private SerializedProperty onHoldButtonExitProperty;
    private SerializedProperty holdEnterAudioProperty;
    private SerializedProperty holdLogicAudioProperty;
    private SerializedProperty holdExitAudioProperty;

    private SerializedProperty startingToggleStateProperty;
    private SerializedProperty toggleStateOnProperty;
    private SerializedProperty toggleStateOffProperty;

    private static bool showClickSettings = true;
    private static bool showHoverSettings = true;
    private static bool showHoldSettings = true;
    private static bool showToggleOutputs = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
        outputTypeProperty = serializedObject.FindProperty("outputType");
        audioBackendProperty = serializedObject.FindProperty("audioBackend");
        unityAudioSourceProperty = serializedObject.FindProperty("unityAudioSource");

        clickAudioProperty = serializedObject.FindProperty("clickAudio");

        hoverButtonEnabledProperty = serializedObject.FindProperty("hoverButtonEnabled");
        onHoverButtonEnterProperty = serializedObject.FindProperty("onHoverButtonEnter");
        onHoverButtonExitProperty = serializedObject.FindProperty("onHoverButtonExit");
        hoverEnterAudioProperty = serializedObject.FindProperty("hoverEnterAudio");
        hoverExitAudioProperty = serializedObject.FindProperty("hoverExitAudio");
        hoverScaleEnabledProperty = serializedObject.FindProperty("hoverScaleEnabled");
        hoverScaleTargetProperty = serializedObject.FindProperty("hoverScaleTarget");
        hoverTargetScaleProperty = serializedObject.FindProperty("hoverTargetScale");
        hoverScaleInDurationProperty = serializedObject.FindProperty("hoverScaleInDuration");
        hoverScaleOutDurationProperty = serializedObject.FindProperty("hoverScaleOutDuration");
        useUnscaledTimeForHoverScaleProperty = serializedObject.FindProperty("useUnscaledTimeForHoverScale");

        holdButtonEnabledProperty = serializedObject.FindProperty("holdButtonEnabled");
        holdEnterDelayProperty = serializedObject.FindProperty("holdEnterDelay");
        holdLogicFrequencyProperty = serializedObject.FindProperty("holdLogicFrequency");
        onHoldButtonEnterProperty = serializedObject.FindProperty("onHoldButtonEnter");
        onHoldButtonLogicProperty = serializedObject.FindProperty("onHoldButtonLogic");
        onHoldButtonExitProperty = serializedObject.FindProperty("onHoldButtonExit");
        holdEnterAudioProperty = serializedObject.FindProperty("holdEnterAudio");
        holdLogicAudioProperty = serializedObject.FindProperty("holdLogicAudio");
        holdExitAudioProperty = serializedObject.FindProperty("holdExitAudio");

        startingToggleStateProperty = serializedObject.FindProperty("startingToggleState");
        toggleStateOnProperty = serializedObject.FindProperty("toggleStateOn");
        toggleStateOffProperty = serializedObject.FindProperty("toggleStateOff");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        DrawClickSettings();
        DrawOutputTypeSettings();
        DrawAudioBackendSettings();
        DrawHoverSettings();
        DrawHoldSettings();
        DrawToggleOutputs();

        serializedObject.ApplyModifiedProperties();
    }


    private void DrawOutputTypeSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output Type", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        bool useButton = !outputTypeProperty.hasMultipleDifferentValues && outputTypeProperty.enumValueIndex == (int)UIButton.OutputType.Button;
        bool useToggle = !outputTypeProperty.hasMultipleDifferentValues && outputTypeProperty.enumValueIndex == (int)UIButton.OutputType.Toggle;

        if (GUILayout.Button(useButton ? "✓ Button" : "Button", GUILayout.Height(24)))
        {
            outputTypeProperty.enumValueIndex = (int)UIButton.OutputType.Button;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            Repaint();
        }

        if (GUILayout.Button(useToggle ? "✓ Toggle" : "Toggle", GUILayout.Height(24)))
        {
            outputTypeProperty.enumValueIndex = (int)UIButton.OutputType.Toggle;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAudioBackendSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio Backend", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        bool useUnityAudio = !audioBackendProperty.hasMultipleDifferentValues && audioBackendProperty.enumValueIndex == (int)UIButton.AudioBackend.UnityAudio;
        bool useFMODAudio = !audioBackendProperty.hasMultipleDifferentValues && audioBackendProperty.enumValueIndex == (int)UIButton.AudioBackend.FMODAudio;

        if (GUILayout.Button(useUnityAudio ? "✓ Unity Audio" : "Unity Audio", GUILayout.Height(24)))
            audioBackendProperty.enumValueIndex = (int)UIButton.AudioBackend.UnityAudio;

        if (GUILayout.Button(useFMODAudio ? "✓ FMOD Audio" : "FMOD Audio", GUILayout.Height(24)))
            audioBackendProperty.enumValueIndex = (int)UIButton.AudioBackend.FMODAudio;

        EditorGUILayout.EndHorizontal();

        if (audioBackendProperty.enumValueIndex == (int)UIButton.AudioBackend.UnityAudio)
            EditorGUILayout.PropertyField(unityAudioSourceProperty);
    }

    private void DrawClickSettings()
    {
        EditorGUILayout.Space();
        showClickSettings = EditorGUILayout.Foldout(showClickSettings, "Click Button", true);

        if (!showClickSettings)
            return;

        EditorGUI.indentLevel++;
        DrawAudioCue(clickAudioProperty, "Click Audio");
        EditorGUILayout.PropertyField(m_OnClickProperty);
        EditorGUI.indentLevel--;
    }

    private void DrawHoverSettings()
    {
        EditorGUILayout.Space();
        showHoverSettings = EditorGUILayout.Foldout(showHoverSettings, "Hover Button", true);

        if (!showHoverSettings)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(hoverButtonEnabledProperty);

        if (hoverButtonEnabledProperty.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hover Scale", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hoverScaleEnabledProperty);

            if (hoverScaleEnabledProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hoverScaleTargetProperty, new GUIContent("Hover Scale Target"));
                EditorGUILayout.HelpBox("Assign a child visual object here. Do not scale the button root/Image that receives raycasts, or hover can flicker.", MessageType.Info);
                EditorGUILayout.PropertyField(hoverTargetScaleProperty, new GUIContent("Hover Target Scale"));
                EditorGUILayout.PropertyField(hoverScaleInDurationProperty, new GUIContent("Scale In Duration"));
                EditorGUILayout.PropertyField(hoverScaleOutDurationProperty, new GUIContent("Scale Out Duration"));
                EditorGUILayout.PropertyField(useUnscaledTimeForHoverScaleProperty, new GUIContent("Use Unscaled Time"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            DrawAudioCue(hoverEnterAudioProperty, "Hover Enter Audio");
            EditorGUILayout.PropertyField(onHoverButtonEnterProperty);

            EditorGUILayout.Space();

            DrawAudioCue(hoverExitAudioProperty, "Hover Exit Audio");
            EditorGUILayout.PropertyField(onHoverButtonExitProperty);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawHoldSettings()
    {
        EditorGUILayout.Space();
        showHoldSettings = EditorGUILayout.Foldout(showHoldSettings, "Hold Button", true);

        if (!showHoldSettings)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(holdButtonEnabledProperty);

        if (holdButtonEnabledProperty.boolValue)
        {
            EditorGUILayout.PropertyField(holdEnterDelayProperty, new GUIContent("Hold Enter Delay"));
            EditorGUILayout.PropertyField(holdLogicFrequencyProperty, new GUIContent("Hold Logic Frequency"));

            EditorGUILayout.Space();

            DrawAudioCue(holdEnterAudioProperty, "Hold Enter Audio");
            EditorGUILayout.PropertyField(onHoldButtonEnterProperty);

            EditorGUILayout.Space();

            DrawAudioCue(holdLogicAudioProperty, "Hold Logic Audio");
            EditorGUILayout.PropertyField(onHoldButtonLogicProperty);

            EditorGUILayout.Space();

            DrawAudioCue(holdExitAudioProperty, "Hold Exit Audio");
            EditorGUILayout.PropertyField(onHoldButtonExitProperty);
        }

        EditorGUI.indentLevel--;
    }


    private void DrawToggleOutputs()
    {
        if (outputTypeProperty.hasMultipleDifferentValues || outputTypeProperty.enumValueIndex != (int)UIButton.OutputType.Toggle)
            return;

        EditorGUILayout.Space();
        showToggleOutputs = EditorGUILayout.Foldout(showToggleOutputs, "Toggle Outputs", true);

        if (!showToggleOutputs)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(startingToggleStateProperty, new GUIContent("Starting Toggle State"));
        EditorGUILayout.PropertyField(toggleStateOnProperty, new GUIContent("Toggle State On"));
        EditorGUILayout.PropertyField(toggleStateOffProperty, new GUIContent("Toggle State Off"));
        EditorGUI.indentLevel--;
    }

    private void DrawAudioCue(SerializedProperty audioCueProperty, string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        SerializedProperty playAudioProperty = audioCueProperty.FindPropertyRelative("playAudio");
        SerializedProperty unityClipProperty = audioCueProperty.FindPropertyRelative("unityClip");
        SerializedProperty unityVolumeProperty = audioCueProperty.FindPropertyRelative("unityVolume");
        SerializedProperty fmodEventProperty = audioCueProperty.FindPropertyRelative("fmodEvent");
        SerializedProperty fmodPlayAtButtonPositionProperty = audioCueProperty.FindPropertyRelative("fmodPlayAtButtonPosition");

        EditorGUILayout.PropertyField(playAudioProperty);

        if (!playAudioProperty.boolValue)
            return;

        EditorGUI.indentLevel++;

        if (audioBackendProperty.enumValueIndex == (int)UIButton.AudioBackend.UnityAudio)
        {
            EditorGUILayout.PropertyField(unityClipProperty);
            EditorGUILayout.Slider(unityVolumeProperty, 0f, 1f);
        }

        if (audioBackendProperty.enumValueIndex == (int)UIButton.AudioBackend.FMODAudio)
        {
            EditorGUILayout.PropertyField(fmodEventProperty, new GUIContent("FMOD Event"), true);
            EditorGUILayout.PropertyField(fmodPlayAtButtonPositionProperty);
        }

        EditorGUI.indentLevel--;
    }
}
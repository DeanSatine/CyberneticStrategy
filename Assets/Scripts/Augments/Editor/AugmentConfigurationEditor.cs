// Updated /Assets/Scripts/Augments/Editor/AugmentConfigurationEditor.cs
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AugmentConfiguration))]
public class AugmentConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AugmentConfiguration config = (AugmentConfiguration)target;

        serializedObject.Update();

        // Always show the augment selection
        EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedAugment"), new GUIContent("Selected Augment"));

        EditorGUILayout.Space();

        // Get the current selection
        var selectedAugmentProp = serializedObject.FindProperty("selectedAugment");
        ConfigurableAugmentType selectedAugment = (ConfigurableAugmentType)selectedAugmentProp.enumValueIndex;

        // Show only relevant settings based on selection
        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak:
                EditorGUILayout.LabelField("Eradicate the Weak Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("manaDrivePrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusAttackDamage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusAttackSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("healPercentage"));
                break;

            case ConfigurableAugmentType.ClobberingTime:
                EditorGUILayout.LabelField("Clobbering Time Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bopPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("attackDamageBonus"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lowHealthThreshold"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpDamage"));
                break;

            case ConfigurableAugmentType.SupportRevolution:
                EditorGUILayout.LabelField("Support Revolution Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearsPerUnit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearOrbitRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearOrbitSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseHealAmount"));
                break;
        }

        EditorGUILayout.Space();

        // Show debug info
        EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugAugmentId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugAugmentName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugAugmentType"));
        GUI.enabled = true;

        serializedObject.ApplyModifiedProperties();
    }
}

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

        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak:
                EditorGUILayout.LabelField("Eradicate the Weak Settings", EditorStyles.boldLabel);

                // Prefabs
                EditorGUILayout.LabelField("Prefabs", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("manaDrivePrefab"));

                // Stats
                EditorGUILayout.LabelField("Stats", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusAttackDamage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusAttackSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("healPercentage"));

                // VFX
                EditorGUILayout.LabelField("VFX Prefabs", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("linkVFXPrefab"), new GUIContent("Link VFX", "Visual effect for the link between unit and press"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("healVFXPrefab"), new GUIContent("Heal VFX", "Effect when unit heals from execution"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("linkedUnitVFXPrefab"), new GUIContent("Linked Unit VFX", "Aura around the linked unit"));
                break;

            case ConfigurableAugmentType.ClobberingTime:
                EditorGUILayout.LabelField("Clobbering Time Settings", EditorStyles.boldLabel);

                // Prefabs
                EditorGUILayout.LabelField("Prefabs", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bopPrefab"));

                // Stats
                EditorGUILayout.LabelField("Stats", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("attackDamageBonus"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lowHealthThreshold"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpDamage"));

                // VFX
                EditorGUILayout.LabelField("VFX Prefabs", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpVFXPrefab"), new GUIContent("Jump VFX", "Effect when Clobbertron jumps"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("landingVFXPrefab"), new GUIContent("Landing VFX", "Effect when Clobbertron lands"));
                break;

            case ConfigurableAugmentType.SupportRevolution:
                EditorGUILayout.LabelField("Support Revolution Settings", EditorStyles.boldLabel);

                // Prefabs
                EditorGUILayout.LabelField("Prefabs", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearPrefab"));

                // Stats
                EditorGUILayout.LabelField("Stats", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearsPerUnit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearOrbitRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearOrbitSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseHealAmount"));

                // VFX
                EditorGUILayout.LabelField("VFX Prefabs", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gearHealVFXPrefab"), new GUIContent("Gear Heal VFX", "Effect when gear heals a unit"));
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

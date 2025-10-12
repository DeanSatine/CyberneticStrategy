using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public static class SceneAugmentMapping
{
    // Define mapping between augment names and their scene containers
    private static Dictionary<string, string> augmentToSceneMap = new Dictionary<string, string>()
    {
        // Map augment names to scene container names
        { "Its Clobbering Time!", "ClobberingTimeAugment" },
        { "Eradicate the Weak", "EradicateTheWeakAugment" },
        { "Support Revolution", "SupportRevolutionAugment" },
        
        // Alternative names in case augments use different strings
        { "ItsClobberingTimeAugment", "ClobberingTimeAugment" },
        { "ClobberingTime", "ClobberingTimeAugment" },
        { "Clobbering", "ClobberingTimeAugment" },
        { "EradicateWeak", "EradicateTheWeakAugment" },
        { "Eradicate", "EradicateTheWeakAugment" },
        { "SupportRevolution", "SupportRevolutionAugment" },
        { "Support", "SupportRevolutionAugment" },
        { "Revolution", "SupportRevolutionAugment" }
    };

    /// <summary>
    /// Gets the description text from the scene Desc object
    /// </summary>
    public static string GetDescriptionFromScene(string augmentName)
    {
        string sceneContainerName = GetSceneContainerName(augmentName);
        if (string.IsNullOrEmpty(sceneContainerName))
        {
            Debug.LogWarning($"⚠️ No scene mapping found for augment: {augmentName}");
            return null;
        }

        // Find the specific scene path
        string scenePath = $"/AugmentCanvas/AugmentSelectionPanel";

        // Search through all augment containers
        for (int i = 1; i <= 3; i++)
        {
            string containerPath = $"{scenePath}/Augment{i}/{sceneContainerName}";
            GameObject container = GameObject.Find($"Augment{i}");

            if (container != null)
            {
                Transform augmentContainer = container.transform.Find(sceneContainerName);
                if (augmentContainer != null)
                {
                    Transform descTransform = augmentContainer.Find("Desc");
                    if (descTransform != null)
                    {
                        TextMeshProUGUI descComponent = descTransform.GetComponent<TextMeshProUGUI>();
                        if (descComponent != null && !string.IsNullOrEmpty(descComponent.text))
                        {
                            Debug.Log($"✅ Found scene description for '{augmentName}' in {containerPath}/Desc: {descComponent.text.Substring(0, Mathf.Min(50, descComponent.text.Length))}...");
                            return descComponent.text;
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"⚠️ Could not find scene description for augment '{augmentName}' in container '{sceneContainerName}'");
        return null;
    }

    /// <summary>
    /// Gets the icon sprite from the scene AugmentIcon object
    /// </summary>
    public static Sprite GetIconFromScene(string augmentName)
    {
        string sceneContainerName = GetSceneContainerName(augmentName);
        if (string.IsNullOrEmpty(sceneContainerName))
        {
            Debug.LogWarning($"⚠️ No scene mapping found for augment: {augmentName}");
            return null;
        }

        // Search through all augment containers
        for (int i = 1; i <= 3; i++)
        {
            GameObject container = GameObject.Find($"Augment{i}");

            if (container != null)
            {
                Transform augmentContainer = container.transform.Find(sceneContainerName);
                if (augmentContainer != null)
                {
                    Transform iconTransform = augmentContainer.Find("AugmentIcon");
                    if (iconTransform != null)
                    {
                        Image iconComponent = iconTransform.GetComponent<Image>();
                        if (iconComponent != null && iconComponent.sprite != null)
                        {
                            Debug.Log($"✅ Found scene icon for '{augmentName}' in Augment{i}/{sceneContainerName}/AugmentIcon: {iconComponent.sprite.name}");
                            return iconComponent.sprite;
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"⚠️ Could not find scene icon for augment '{augmentName}' in container '{sceneContainerName}'");
        return null;
    }

    /// <summary>
    /// Gets the title text from the scene Title object (useful for verification)
    /// </summary>
    public static string GetTitleFromScene(string augmentName)
    {
        string sceneContainerName = GetSceneContainerName(augmentName);
        if (string.IsNullOrEmpty(sceneContainerName)) return null;

        // Search through all augment containers
        for (int i = 1; i <= 3; i++)
        {
            GameObject container = GameObject.Find($"Augment{i}");

            if (container != null)
            {
                Transform augmentContainer = container.transform.Find(sceneContainerName);
                if (augmentContainer != null)
                {
                    Transform titleTransform = augmentContainer.Find("Title");
                    if (titleTransform != null)
                    {
                        TextMeshProUGUI titleComponent = titleTransform.GetComponent<TextMeshProUGUI>();
                        if (titleComponent != null && !string.IsNullOrEmpty(titleComponent.text))
                        {
                            return titleComponent.text;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Maps augment name to scene container name
    /// </summary>
    private static string GetSceneContainerName(string augmentName)
    {
        if (string.IsNullOrEmpty(augmentName)) return null;

        // Direct lookup
        if (augmentToSceneMap.ContainsKey(augmentName))
        {
            return augmentToSceneMap[augmentName];
        }

        // Try partial matches
        string cleanName = augmentName.Replace(" ", "").Replace("!", "").Replace("'", "");
        foreach (var kvp in augmentToSceneMap)
        {
            string cleanKey = kvp.Key.Replace(" ", "").Replace("!", "").Replace("'", "");
            if (cleanName.Contains(cleanKey) || cleanKey.Contains(cleanName))
            {
                Debug.Log($"🔍 Found partial match: '{augmentName}' → '{kvp.Value}' via '{kvp.Key}'");
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Debug method to list all scene augment data
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    public static void DebugListAllSceneAugments()
    {
        Debug.Log("=== SCENE AUGMENT DATA DEBUG ===");

        for (int i = 1; i <= 3; i++)
        {
            GameObject container = GameObject.Find($"Augment{i}");
            if (container != null)
            {
                Debug.Log($"Found Augment{i} container");

                foreach (Transform child in container.transform)
                {
                    string containerName = child.name;
                    Debug.Log($"  - Container: {containerName}");

                    // Get Title
                    Transform titleT = child.Find("Title");
                    string titleText = titleT?.GetComponent<TextMeshProUGUI>()?.text ?? "No title";

                    // Get Description
                    Transform descT = child.Find("Desc");
                    string descText = descT?.GetComponent<TextMeshProUGUI>()?.text ?? "No description";

                    // Get Icon
                    Transform iconT = child.Find("AugmentIcon");
                    string iconName = iconT?.GetComponent<Image>()?.sprite?.name ?? "No icon";

                    Debug.Log($"    Title: {titleText}");
                    Debug.Log($"    Desc: {descText.Substring(0, Mathf.Min(50, descText.Length))}...");
                    Debug.Log($"    Icon: {iconName}");
                }
            }
        }
    }

    /// <summary>
    /// Manually test mapping for a specific augment name
    /// </summary>
    public static void TestAugmentMapping(string augmentName)
    {
        Debug.Log($"=== TESTING AUGMENT MAPPING FOR: {augmentName} ===");

        string sceneContainer = GetSceneContainerName(augmentName);
        Debug.Log($"Scene Container: {sceneContainer ?? "NOT FOUND"}");

        string description = GetDescriptionFromScene(augmentName);
        Debug.Log($"Description: {description ?? "NOT FOUND"}");

        Sprite icon = GetIconFromScene(augmentName);
        Debug.Log($"Icon: {(icon != null ? icon.name : "NOT FOUND")}");

        string title = GetTitleFromScene(augmentName);
        Debug.Log($"Title: {title ?? "NOT FOUND"}");
    }
}

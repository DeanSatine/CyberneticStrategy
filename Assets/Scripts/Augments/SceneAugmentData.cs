using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SceneAugmentData
{
    /// <summary>
    /// Gets augment description from scene Desc object based on augment name
    /// </summary>
    public static string GetAugmentDescriptionFromScene(string augmentName)
    {
        // Try different naming patterns to find the right augment container
        string[] possibleContainerNames = {
            $"{augmentName.Replace(" ", "").Replace("!", "")}Augment", // "ItsClobberingTimeAugment"
            $"{augmentName}Augment",
            "EradicateTheWeakAugment", // Fallback specific names
            "ClobberingTimeAugment",
            "SupportRevolutionAugment"
        };

        foreach (string containerName in possibleContainerNames)
        {
            // Look for the augment container in the scene
            GameObject augmentContainer = GameObject.Find(containerName);
            if (augmentContainer != null)
            {
                // Look for Desc child object
                Transform descTransform = augmentContainer.transform.Find("Desc");
                if (descTransform != null)
                {
                    TextMeshProUGUI descText = descTransform.GetComponent<TextMeshProUGUI>();
                    if (descText != null && !string.IsNullOrEmpty(descText.text))
                    {
                        Debug.Log($"📝 Found description for {augmentName} in scene object: {containerName}/Desc");
                        return descText.text;
                    }
                }
            }
        }

        // If not found in scene, return a fallback message
        Debug.LogWarning($"⚠️ Could not find scene description for augment: {augmentName}");
        return "Description not found in scene.";
    }

    /// <summary>
    /// Gets augment icon from scene AugmentIcon object based on augment name
    /// </summary>
    public static Sprite GetAugmentIconFromScene(string augmentName)
    {
        // Try different naming patterns to find the right augment container
        string[] possibleContainerNames = {
            $"{augmentName.Replace(" ", "").Replace("!", "")}Augment", // "ItsClobberingTimeAugment"
            $"{augmentName}Augment",
            "EradicateTheWeakAugment", // Fallback specific names
            "ClobberingTimeAugment",
            "SupportRevolutionAugment"
        };

        foreach (string containerName in possibleContainerNames)
        {
            // Look for the augment container in the scene
            GameObject augmentContainer = GameObject.Find(containerName);
            if (augmentContainer != null)
            {
                // Look for AugmentIcon child object
                Transform iconTransform = augmentContainer.transform.Find("AugmentIcon");
                if (iconTransform != null)
                {
                    Image iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null && iconImage.sprite != null)
                    {
                        Debug.Log($"🎨 Found icon for {augmentName} in scene object: {containerName}/AugmentIcon");
                        return iconImage.sprite;
                    }
                }
            }
        }

        // If not found in scene, return null (will use fallback coloring)
        Debug.LogWarning($"⚠️ Could not find scene icon for augment: {augmentName}");
        return null;
    }

    /// <summary>
    /// Alternative method: Find by searching all AugmentCanvas descendants
    /// </summary>
    public static string GetDescriptionFromAugmentCanvas(string augmentName)
    {
        GameObject augmentCanvas = GameObject.Find("AugmentCanvas");
        if (augmentCanvas == null) return null;

        // Search through all Desc objects in AugmentCanvas
        TextMeshProUGUI[] allDescTexts = augmentCanvas.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (TextMeshProUGUI descText in allDescTexts)
        {
            if (descText.gameObject.name == "Desc" && !string.IsNullOrEmpty(descText.text))
            {
                // Check if this Desc belongs to our augment by checking parent names
                Transform parent = descText.transform.parent;
                if (parent != null && (parent.name.Contains(augmentName.Replace(" ", "")) ||
                                     parent.name.Contains("ClobberingTime") ||
                                     parent.name.Contains("EradicateTheWeak") ||
                                     parent.name.Contains("SupportRevolution")))
                {
                    Debug.Log($"📝 Found description via canvas search: {descText.text.Substring(0, Mathf.Min(30, descText.text.Length))}...");
                    return descText.text;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Alternative method: Find icon by searching all AugmentCanvas descendants  
    /// </summary>
    public static Sprite GetIconFromAugmentCanvas(string augmentName)
    {
        GameObject augmentCanvas = GameObject.Find("AugmentCanvas");
        if (augmentCanvas == null) return null;

        // Search through all Image components in AugmentCanvas
        Image[] allImages = augmentCanvas.GetComponentsInChildren<Image>();

        foreach (Image image in allImages)
        {
            if (image.gameObject.name == "AugmentIcon" && image.sprite != null)
            {
                // Check if this AugmentIcon belongs to our augment by checking parent names
                Transform parent = image.transform.parent;
                if (parent != null && (parent.name.Contains(augmentName.Replace(" ", "")) ||
                                     parent.name.Contains("ClobberingTime") ||
                                     parent.name.Contains("EradicateTheWeak") ||
                                     parent.name.Contains("SupportRevolution")))
                {
                    Debug.Log($"🎨 Found icon via canvas search: {image.sprite.name}");
                    return image.sprite;
                }
            }
        }

        return null;
    }
}

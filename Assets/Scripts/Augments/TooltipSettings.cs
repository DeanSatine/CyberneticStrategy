using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "TooltipSettings", menuName = "UI/Tooltip Settings")]
public class TooltipSettings : ScriptableObject
{
    [Header("Text Settings")]
    public float fontSize = 14f;
    public FontStyles fontStyle = FontStyles.Normal;
    public Color textColor = Color.white;
    public TextAlignmentOptions textAlignment = TextAlignmentOptions.TopLeft;

    [Header("Dimensions")]
    public float maxWidth = 300f;
    public float minHeight = 50f;
    public int maxCharactersPerLine = 45;

    [Header("Padding & Spacing")]
    public float leftPadding = 10f;
    public float rightPadding = 10f;
    public float topPadding = 8f;
    public float bottomPadding = 8f;

    [Header("Background")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
    public bool useOutline = true;
    public Color outlineColor = new Color(1f, 1f, 1f, 0.3f);
    public Vector2 outlineDistance = new Vector2(1, -1);

    [Header("Positioning")]
    public bool useFixedPosition = true;  // Toggle between fixed and mouse following
    [Space]
    [Header("Fixed Position Mode")]
    public bool usePercentagePositioning = true; // Use screen percentage instead
    [Range(0f, 1f)] public float screenPercentageX = 0.75f; // 75% from left
    [Range(0f, 1f)] public float screenPercentageY = 0.8f;  // 80% from bottom
    [Space]
    public Vector2 fixedPosition = new Vector2(400f, -200f);  // Canvas coordinates (if not using percentage)
    public Vector2 offsetFromCursor = new Vector2(15, 15);  // For mouse following mode
    public float fadeSpeed = 8f;

    [Header("Font Asset (Optional)")]
    public TMP_FontAsset customFont;
}

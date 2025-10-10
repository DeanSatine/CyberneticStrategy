// /Assets/Scripts/System/BaseAugment.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public abstract class BaseAugment
{
    [Header("Augment Info")]
    public string augmentName;
    public string description;
    public Sprite icon;
    public AugmentType type;

    [Header("Visual Effects")]
    public GameObject augmentVFXPrefab;
    public Color augmentColor = Color.white;

    public abstract void ApplyAugment();
    public abstract void OnCombatStart();
    public abstract void OnCombatEnd();
    public abstract void OnUnitSpawned(UnitAI unit);
    public virtual void RemoveAugment() { } // For cleanup if needed
}

public enum AugmentType
{
    Origin,
    Class,
    Generic
}

[System.Serializable]
public class AugmentData
{
    public string augmentName;
    public AugmentType type;
    public BaseAugment augmentInstance;
    public bool isActive;
}

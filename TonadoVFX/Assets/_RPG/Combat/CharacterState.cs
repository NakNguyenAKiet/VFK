using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [Header("Core Stats")]
    public float maxHealth = 100f;
    public float maxMana = 100f;
    public float healthRegen = 1f;
    public float manaRegen = 2f;
    
    [Header("Offensive Stats")]
    public float attackDamage = 10f;
    public float attackSpeed = 1f;
    public float criticalChance = 5f;
    public float criticalDamage = 150f;
    public float magicPower = 10f;
    
    [Header("Defensive Stats")]
    public float armor = 5f;
    public float magicResist = 5f;
    public float dodgeChance = 5f;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    // Stat modifiers từ items, buffs, debuffs
    private Dictionary<string, StatModifier> modifiers = new Dictionary<string, StatModifier>();
    
    public float GetFinalStat(StatType statType)
    {
        float baseStat = GetBaseStat(statType);
        float finalValue = baseStat;
        
        foreach (var modifier in modifiers.Values)
        {
            if (modifier.statType == statType)
            {
                finalValue += baseStat * (modifier.percentageBonus / 100f);
                finalValue += modifier.flatBonus;
            }
        }
        
        return finalValue;
    }
    
    private float GetBaseStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.MaxHealth: return maxHealth;
            case StatType.AttackDamage: return attackDamage;
            case StatType.Armor: return armor;
            // ... other stats
            default: return 0f;
        }
    }
}

public enum StatType
{
    MaxHealth, MaxMana, AttackDamage, AttackSpeed,
    CriticalChance, CriticalDamage, Armor, MagicResist,
    MoveSpeed, HealthRegen, ManaRegen
}

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public float flatBonus;
    public float percentageBonus;
    public float duration;
}
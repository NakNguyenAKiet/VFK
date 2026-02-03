using System.Collections.Generic;
using UnityEngine;

// Base Stats System
public interface IDamageable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }  // Thêm property này
    Transform transform { get; }  // Cần để truy cập transform
    
    void TakeDamage(DamageInfo damageInfo);
    void Die();
}
public interface IAttacker
{
    float AttackDamage { get; }
    float AttackSpeed { get; }
    float AttackRange { get; }
    void PerformAttack(IDamageable target);
}

[System.Serializable]
public class DamageInfo
{
    public float physicalDamage;
    public float magicalDamage;
    public DamageType damageType;
    public GameObject attacker;
    public Vector3 hitPoint;
    public Vector3 hitDirection;
    public bool isCritical;
    public float criticalMultiplier;
    
    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Poison,
        Fire,
        Ice
    }
}
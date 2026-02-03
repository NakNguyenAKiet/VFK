
using UnityEngine;

public abstract class CombatEntity : MonoBehaviour, IDamageable, IAttacker
{
    [SerializeField] protected CharacterStats baseStats;
    protected CharacterStats currentStats;
    protected float currentHealth;
    protected float currentMana;
    protected bool isDead = false;
    
    // Combat state
    protected bool isAttacking = false;
    protected bool isInvulnerable = false;
    protected float lastAttackTime;
    
    // Components
    protected Animator animator;
    protected Rigidbody rb;
    protected CapsuleCollider capsuleCollider;
    
    // Events
    public event System.Action<DamageInfo> OnDamageReceived;
    public event System.Action OnDeath;
    public event System.Action<IDamageable> OnAttackPerformed;
    
    // Properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => currentStats.GetFinalStat(StatType.MaxHealth);
    public float AttackDamage => currentStats.GetFinalStat(StatType.AttackDamage);
    public float AttackSpeed => currentStats.GetFinalStat(StatType.AttackSpeed);
    public abstract float AttackRange { get; }
    public bool IsDead => isDead;
    
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        // Clone stats để tránh modify ScriptableObject gốc
        currentStats = CloneStats(baseStats);
        currentHealth = MaxHealth;
        currentMana = currentStats.maxMana;
    }
    
    protected virtual void Update()
    {
        if (!isDead)
        {
            RegenerateResources();
        }
    }
    
    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || isInvulnerable) return;
        
        float finalDamage = CalculateDamage(damageInfo);
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnDamageReceived?.Invoke(damageInfo);
        
        // Visual feedback
        ShowDamageNumber(finalDamage, damageInfo.isCritical);
        PlayHitEffect(damageInfo.hitPoint);
        
        // Animation
        if (!isAttacking)
        {
            animator.SetTrigger("Hit");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    protected virtual float CalculateDamage(DamageInfo damageInfo)
    {
        float damage = 0f;
        
        switch (damageInfo.damageType)
        {
            case DamageInfo.DamageType.Physical:
                float armor = currentStats.GetFinalStat(StatType.Armor);
                float damageReduction = armor / (armor + 100f);
                damage = damageInfo.physicalDamage * (1f - damageReduction);
                break;
                
            case DamageInfo.DamageType.Magical:
                float magicResist = currentStats.GetFinalStat(StatType.MagicResist);
                float magicReduction = magicResist / (magicResist + 100f);
                damage = damageInfo.magicalDamage * (1f - magicReduction);
                break;
                
            case DamageInfo.DamageType.True:
                damage = damageInfo.physicalDamage + damageInfo.magicalDamage;
                break;
        }
        
        if (damageInfo.isCritical)
        {
            damage *= damageInfo.criticalMultiplier / 100f;
        }
        
        return damage;
    }
    
    public abstract void PerformAttack(IDamageable target);
    
    public virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        animator.SetTrigger("Death");
        
        OnDeath?.Invoke();
        
        // Disable combat
        enabled = false;
        capsuleCollider.enabled = false;
    }
    
    protected virtual void RegenerateResources()
    {
        if (currentHealth < MaxHealth)
        {
            currentHealth += currentStats.healthRegen * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, MaxHealth);
        }
        
        if (currentMana < currentStats.maxMana)
        {
            currentMana += currentStats.manaRegen * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, currentStats.maxMana);
        }
    }
    
    protected bool CanAttack()
    {
        return !isDead && 
               !isAttacking && 
               Time.time >= lastAttackTime + (1f / AttackSpeed);
    }
    
    protected virtual void ShowDamageNumber(float damage, bool isCritical)
    {
        // Implement damage number popup
        DamageNumberManager.Instance?.ShowDamage(
            transform.position + Vector3.up * 2f,
            damage,
            isCritical
        );
    }
    
    protected virtual void PlayHitEffect(Vector3 hitPoint)
    {
        // Spawn hit VFX
        VFXManager.Instance?.PlayHitEffect(hitPoint);
    }
    
    private CharacterStats CloneStats(CharacterStats original)
    {
        // Deep clone implementation
        CharacterStats clone = new CharacterStats();
        // Copy all fields...
        return clone;
    }
}
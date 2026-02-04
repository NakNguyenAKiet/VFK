using UnityEngine;

public class EnemyCombat : CombatEntity
{
    #region Settings
    [Header("Enemy Specific")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private EnemyType enemyType;
    #endregion

    #region Components
    private EnemyAI enemyAI;
    #endregion

    #region Combat State
    private Transform target;
    private float nextAttackTime;
    #endregion

    #region Enemy Type Enum
    public enum EnemyType
    {
        Melee,
        Ranged,
        Tank,
        Elite,
        Boss
    }
    #endregion

    #region Properties
    public override float AttackRange => enemyType == EnemyType.Melee ? 2f : 8f;
    #endregion

    #region Lifecycle
    protected override void Awake()
    {
        base.Awake();
        enemyAI = GetComponent<EnemyAI>();
        
        // Scale stats theo enemy type
        ApplyEnemyTypeModifiers();
    }
    
    #endregion

    #region Attack Handling
    public void TryAttackTarget()
    {
        if (Time.time < nextAttackTime || !enemyAI.HasTarget()) return;
        if (!CanAttack()) return;
        if (enemyAI == null || !enemyAI.HasTarget()) return;
        
        Transform target = enemyAI.Target;
        if (target == null) return;
        
        // Check if target is in range
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > AttackRange) return;
        
        // Check if target is damageable and alive
        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable == null || targetDamageable.IsDead) return;
        
        // Perform attack
        PerformAttack(targetDamageable);
    }
    
    public override void PerformAttack(IDamageable target)
    {
        if (target == null || target.IsDead) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;
        
        // Face target
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackTimer());

        OnMeleeHit();
    }
    
    // Animation Event cho melee attack
    public void OnMeleeHit()
    {
        if (target == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= AttackRange)
        {
            IDamageable targetDamageable = target.GetComponent<IDamageable>();
            if (targetDamageable != null)
            {
                DamageInfo damageInfo = new DamageInfo
                {
                    physicalDamage = AttackDamage,
                    damageType = DamageInfo.DamageType.Physical,
                    attacker = gameObject,
                    hitPoint = target.position,
                    hitDirection = (target.position - transform.position).normalized
                };
                
                targetDamageable.TakeDamage(damageInfo);
            }
        }
        
        isAttacking = false;
    }
    #endregion

    #region Death
    public override void Die()
    {
        base.Die();
        
        // Destroy sau animation
        Destroy(gameObject, 3f);
    }
    #endregion

    #region Stat Modifiers
    private void ApplyEnemyTypeModifiers()
    {
        switch (enemyType)
        {
            case EnemyType.Tank:
                currentStats.maxHealth *= 2f;
                currentStats.armor *= 1.5f;
                currentStats.moveSpeed *= 0.7f;
                break;
                
            case EnemyType.Elite:
                currentStats.maxHealth *= 3f;
                currentStats.attackDamage *= 1.5f;
                break;
                
            case EnemyType.Boss:
                currentStats.maxHealth *= 10f;
                currentStats.attackDamage *= 2f;
                currentStats.armor *= 2f;
                break;
        }
        
        currentHealth = MaxHealth;
    }

    private int GetExpReward()
    {
        return enemyType switch
        {
            EnemyType.Melee => 50,
            EnemyType.Ranged => 60,
            EnemyType.Tank => 75,
            EnemyType.Elite => 150,
            EnemyType.Boss => 500,
            _ => 50
        };
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Visualize ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
    #endregion
}

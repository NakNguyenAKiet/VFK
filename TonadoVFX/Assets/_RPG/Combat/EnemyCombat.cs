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
    private EnemyAI aiController;
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
        aiController = GetComponent<EnemyAI>();
        
        // Scale stats theo enemy type
        ApplyEnemyTypeModifiers();
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (isDead) return;
        
        if (target == null)
        {
            FindTarget();
        }
        else
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (distanceToTarget <= AttackRange && Time.time >= nextAttackTime)
            {
                TryAttackTarget();
            }
        }
    }
    #endregion

    #region Target Detection
    private void FindTarget()
    {
        // Tìm player trong range
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                target = hit.transform;
                aiController.SetTarget(target);
                break;
            }
        }
    }
    #endregion

    #region Attack Handling
    private void TryAttackTarget()
    {
        if (!CanAttack()) return;
        
        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable != null && !targetDamageable.IsDead)
        {
            PerformAttack(targetDamageable);
        }
    }
    
    public override void PerformAttack(IDamageable target)
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;
        
        // Face target
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        
        // Animation
        animator.SetTrigger("Attack");
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

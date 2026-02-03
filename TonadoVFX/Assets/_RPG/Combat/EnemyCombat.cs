using UnityEngine;

public class EnemyCombat : CombatEntity
{
    [Header("Enemy Specific")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private EnemyType enemyType;
    
    private Transform target;
    private EnemyAI aiController;
    private float nextAttackTime;
    
    public override float AttackRange => enemyType == EnemyType.Melee ? 2f : 8f;
    
    public enum EnemyType
    {
        Melee,
        Ranged,
        Tank,
        Elite,
        Boss
    }
    
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
        
        if (enemyType == EnemyType.Ranged)
        {
            // Shoot projectile
            ShootProjectile(target);
        }
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
    
    private void ShootProjectile(IDamageable target)
    {
        // Spawn projectile
        GameObject projectile = ProjectilePool.Instance.Get();
        projectile.transform.position = transform.position + Vector3.up * 1.5f;
        
        Projectile proj = projectile.GetComponent<Projectile>();
        proj.Initialize(
            target.transform,
            AttackDamage,
            gameObject,
            () => isAttacking = false
        );
    }
    
    public override void Die()
    {
        base.Die();
        
        
        
        // Destroy sau animation
        Destroy(gameObject, 3f);
    }
    
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
    
    private void OnDrawGizmosSelected()
    {
        // Visualize ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
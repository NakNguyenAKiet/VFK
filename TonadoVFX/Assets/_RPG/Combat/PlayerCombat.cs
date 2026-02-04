using System.Collections;
using UnityEngine;

public class PlayerCombat : CombatEntity
{
    #region Settings
    [Header("Player Specific")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float comboResetTime = 1f;
    [SerializeField] private bool canMoveWhileAttacking = false;
    #endregion

    #region Components
    private PlayerInput playerInput;
    private PlayerController playerController;
    #endregion

    #region Combat State
    private int currentComboStep = 0;
    private float lastComboTime;
    
    [SerializeField]
    private Weapon currentWeapon;
    #endregion



    #region Properties
    public override float AttackRange => currentWeapon != null ? currentWeapon.attackRange : 2f;
    public bool IsAttacking => isAttacking;
    #endregion

    #region Lifecycle
    protected override void Awake()
    {
        base.Awake();
        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();
    }
    
    private void Start()
    {
        // Setup input events
        if (playerInput != null)
        {
            playerInput.OnAttackPressed += HandleAttackInput;
            playerInput.OnSkill1Pressed += () => UseSkill(0);
            playerInput.OnSkill2Pressed += () => UseSkill(1);
            playerInput.OnSkill3Pressed += () => UseSkill(2);
            playerInput.OnSkill4Pressed += () => UseSkill(3);
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup events
        if (playerInput != null)
        {
            playerInput.OnAttackPressed -= HandleAttackInput;
        }
    }
    #endregion

    #region Attack Handling
    private void HandleAttackInput()
    {
        // Reset combo nếu quá lâu
        if (Time.time - lastComboTime > comboResetTime)
        {
            currentComboStep = 0;
        }
        
        isAttacking = true;
        lastAttackTime = Time.time;
        lastComboTime = Time.time;
        
        // Stop movement nếu không cho phép move while attacking
        if (!canMoveWhileAttacking && playerController != null)
        {
            // Movement sẽ tự động slow down trong PlayerController khi isAttacking = true
        }
        
        // Trigger animation với combo step
        if (animator != null)
        {
            animator.SetInteger(comboStepHash, currentComboStep);
            animator.SetTrigger(attackHash);
        }
        
        // Tăng combo step
        currentComboStep = (currentComboStep + 1) % 3; // 3 hit combo
        
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackTimer());
        // Play attack sound
        // AudioManager.Instance?.PlaySound("PlayerAttack");
    }

    public override void PerformAttack(IDamageable target)
    {
        // Được gọi từ Animation Event
        if (target == null || target.IsDead) return;
        
        DamageInfo damageInfo = CreateDamageInfo();
        target.TakeDamage(damageInfo);
        
        // Trigger hit effects
        VFXManager.Instance?.PlayHitEffect(damageInfo.hitPoint, damageInfo.isCritical);
    }
    #endregion

    #region Damage Detection
    // Animation Event - gọi khi weapon swing animation đến điểm hit
    public void OnWeaponHit()
    {
        DetectAndDamageEnemies();
    }
    
    private void DetectAndDamageEnemies()
    {
        // Sphere cast from player forward
        Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up;
        
        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            AttackRange,
            enemyLayer
        );
        
        foreach (Collider hit in hits)
        {
            IDamageable enemy = hit.GetComponent<IDamageable>();
            if (enemy != null && !enemy.IsDead)
            {
                // Check nếu enemy ở phía trước player
                Vector3 directionToEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
                
                if (angle < 90f) // Attack cone 180 degrees
                {
                    PerformAttack(enemy);
                    
                    // Hit stop effect (optional)
                    // StartCoroutine(HitStop(0.05f));
                }
            }
        }
    }

    private DamageInfo CreateDamageInfo()
    {
        bool isCrit = Random.Range(0f, 100f) < currentStats.GetFinalStat(StatType.CriticalChance);
        
        float finalDamage = AttackDamage;
        
        return new DamageInfo
        {
            physicalDamage = finalDamage,
            magicalDamage = 0f,
            damageType = DamageInfo.DamageType.Physical,
            attacker = gameObject,
            isCritical = isCrit,
            criticalMultiplier = isCrit ? currentStats.GetFinalStat(StatType.CriticalDamage) : 100f,
            hitPoint = transform.position + transform.forward * 2f,
            hitDirection = transform.forward
        };
    }
    
    // Animation Event - kết thúc attack animation
    public void OnAttackComplete()
    {
        isAttacking = false;
    }
    #endregion

    #region Skills
    private void UseSkill(int skillIndex)
    {
        // TODO: Implement skill system
    }
    #endregion

    #region Weapon Management
    public void EquipWeapon(Weapon weapon)
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
        }
        
        currentWeapon = Instantiate(weapon, weaponSocket);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }

    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }
    #endregion

    #region Invulnerability
    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }
    #endregion

    #region Debug & Effects
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Attack range sphere
        Gizmos.color = Color.red;
        Vector3 attackPosition = transform.position + transform.forward * 1.5f + Vector3.up;
        Gizmos.DrawWireSphere(attackPosition, AttackRange);
        
        // Attack cone
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -90, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 90, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBoundary * AttackRange);
        Gizmos.DrawRay(transform.position, rightBoundary * AttackRange);
    }
    
    // Optional: Hit stop effect for better game feel
    private System.Collections.IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
    #endregion
}

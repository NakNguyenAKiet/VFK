using UnityEngine;

public class PlayerCombat : CombatEntity
{
    [Header("Player Specific")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float comboResetTime = 1f;
    
    private PlayerInput playerInput;
    private int currentComboStep = 0;
    private float lastComboTime;
    private Weapon currentWeapon;
    
    public override float AttackRange => currentWeapon != null ? currentWeapon.attackRange : 2f;
    
    protected override void Awake()
    {
        base.Awake();
        playerInput = GetComponent<PlayerInput>();
    }
    
    private void Start()
    {
        // Setup input
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
    
    private void HandleAttackInput()
    {
        if (CanAttack())
        {
            PerformComboAttack();
        }
    }
    
    private void PerformComboAttack()
    {
        // Reset combo nếu quá lâu
        if (Time.time - lastComboTime > comboResetTime)
        {
            currentComboStep = 0;
        }
        
        isAttacking = true;
        lastAttackTime = Time.time;
        lastComboTime = Time.time;
        
        // Trigger animation với combo step
        animator.SetInteger("ComboStep", currentComboStep);
        animator.SetTrigger("Attack");
        
        // Tăng combo step
        currentComboStep = (currentComboStep + 1) % 3; // 3 hit combo
    }
    
    public override void PerformAttack(IDamageable target)
    {
        // Được gọi từ Animation Event
        if (target == null || target.IsDead) return;
        
        DamageInfo damageInfo = CreateDamageInfo();
        target.TakeDamage(damageInfo);
        
        // Trigger hit effects
        VFXManager.Instance?.PlayHitEffect(damageInfo.hitPoint, damageInfo.isCritical);
        
        // Play sound (if you have AudioManager)
        // AudioManager.Instance?.PlaySound("PlayerAttack");
    }
    
    // Animation Event - gọi khi weapon swing animation đến điểm hit
    public void OnWeaponHit()
    {
        DetectAndDamageEnemies();
    }
    
    private void DetectAndDamageEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position + transform.forward * 1.5f,
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
                }
            }
        }
    }
    
    private DamageInfo CreateDamageInfo()
    {
        bool isCrit = Random.Range(0f, 100f) < currentStats.GetFinalStat(StatType.CriticalChance);
        
        return new DamageInfo
        {
            physicalDamage = AttackDamage,
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
    
    private void UseSkill(int skillIndex)
    {
        // TODO: Implement skill system later
        Debug.Log($"Skill {skillIndex} pressed - Not implemented yet");
    }
    
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
}
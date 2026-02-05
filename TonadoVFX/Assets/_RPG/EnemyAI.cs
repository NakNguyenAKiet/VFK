using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simplified Enemy AI - Only 4 states: Idle, Chase, Attack, Dead
/// </summary>
public class EnemyAI : MonoBehaviour
{
    #region Settings
    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    private float idleSpeed = 0f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Idle Behavior")]
    [SerializeField] private bool wanderWhenIdle = false;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderWaitTime = 3f;
    #endregion

    #region Components
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyCombat enemyCombat;
    #endregion

    #region State
    private AIState currentState = AIState.Idle;
    private Transform target;
    private Vector3 spawnPosition;
    private float stateTimer;
    #endregion

    #region AI State Enum
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }
    #endregion

    #region Properties
    public Transform Target => target;
    public AIState CurrentState => currentState;

    public float AttackRange { get => attackRange; set => attackRange = value; }
    #endregion

    #region Animation Parameters
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int deathHash = Animator.StringToHash("Death");
    #endregion

    #region Lifecycle
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyCombat = GetComponent<EnemyCombat>();
        
        spawnPosition = transform.position;
    }
    
    private void Start()
    {
        if (agent != null)
        {
            agent.speed = idleSpeed;
        }
        
        ChangeState(AIState.Idle);
        
        // Subscribe to combat events
        if (enemyCombat != null)
        {
            enemyCombat.OnDamageReceived += HandleDamageTaken;
        }
    }
    
    private void Update()
    {
        if (currentState == AIState.Dead) return;
        
        stateTimer += Time.deltaTime;
        UpdateCurrentState();
    }
    #endregion

    #region State Management
    private void UpdateCurrentState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        
        // Exit current state
        ExitState(currentState);
        
        // Change state
        currentState = newState;
        stateTimer = 0f;
        
        // Enter new state
        EnterState(newState);
    }

    private void EnterState(AIState state)
    {
        if (!agent.enabled) return;
        Debug.Log($"EnemyAI: Entering state {state}");
        switch (state)
        {
            case AIState.Idle:
                agent.isStopped = true;
                agent.speed = idleSpeed;
                break;
                
            case AIState.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                break;
                
            case AIState.Attack:
                agent.isStopped = true;
                agent.speed = idleSpeed;
                break;
                
            case AIState.Dead:
                agent.isStopped = true;
                agent.enabled = false;
                enabled = false;
                
                if (animator != null)
                {
                    animator.SetTrigger(deathHash);
                }
                break;
        }
        UpdateMoveAnim();
    }

    private void ExitState(AIState state)
    {
        // Cleanup nếu cần
    }
    #endregion

    #region Idle State
    private void UpdateIdle()
    {
        // Detect player
        if (DetectTarget())
        {
            ChangeState(AIState.Chase);
            return;
        }
    }

    #endregion

    #region Chase State
    private void UpdateChase()
    {
        // Lost target
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }
        
        // Check if target is dead
        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable != null && targetDamageable.IsDead)
        {
            target = null;
            ChangeState(AIState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Target too far, give up chase
        if (distanceToTarget > detectionRange * 1.5f)
        {
            target = null;
            ChangeState(AIState.Idle);
            return;
        }
        
        // Close enough to attack
        if (distanceToTarget <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }
        
        // Continue chasing
        if (agent.enabled && !agent.pathPending)
        {
            agent.SetDestination(target.position);
        }
        
        // Rotate towards target
        RotateTowards(target.position);
    }
    #endregion

    #region Attack State
    private void UpdateAttack()
    {
        // Lost target
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }
        
        // Check if target is dead
        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable != null && targetDamageable.IsDead)
        {
            target = null;
            ChangeState(AIState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Target moved away, chase again
        if (distanceToTarget > attackRange)
        {
            ChangeState(AIState.Chase);
            return;
        }
        
        // Stop and face target
        if (agent.enabled)
        {
            agent.isStopped = true;
        }
        
        // Always face target when attacking
        RotateTowards(target.position);
        
        enemyCombat?.TryAttackTarget();
    }
    #endregion

    #region Detection
    private bool DetectTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Line of sight check
                Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
                Vector3 rayStart = transform.position + Vector3.up;
                
                if (Physics.Raycast(rayStart, directionToTarget, out RaycastHit rayHit, detectionRange))
                {
                    if (rayHit.collider.CompareTag("Player"))
                    {
                        target = hit.transform;
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    #endregion

    #region Movement
    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    #endregion

    #region Animation
    private void UpdateMoveAnim()
    {
        if (animator == null || !agent.enabled) return;       
        
        animator.SetFloat(speedHash, agent.speed);
    }
    #endregion

    #region Event Handlers
    private void HandleDamageTaken(DamageInfo damageInfo)
    {
        // If we don't have a target, acquire the attacker
        if (target == null && damageInfo.attacker != null)
        {
            Transform attackerTransform = damageInfo.attacker.transform;
            if (attackerTransform.CompareTag("Player"))
            {
                SetTarget(attackerTransform);
            }
        }
        
        // Play hit animation if not already attacking
        if (animator != null && currentState != AIState.Attack)
        {
            animator.SetTrigger(hitHash);
        }
    }
    #endregion

    #region Public Methods
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        
        // Immediately start chasing if idle
        if (currentState == AIState.Idle)
        {
            ChangeState(AIState.Chase);
        }
    }

    public float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        return Vector3.Distance(transform.position, target.position);
    }

    public bool HasTarget()
    {
        return target != null;
    }

    public bool IsInAttackRange()
    {
        return GetDistanceToTarget() <= attackRange;
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Wander radius when idle (blue)
        if (wanderWhenIdle)
        {
            Gizmos.color = Color.blue;
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }
        
        // Line to target (green)
        if (Application.isPlaying && target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, target.position + Vector3.up);
        }
        
        // Current state indicator
        if (Application.isPlaying)
        {
            Vector3 textPos = transform.position + Vector3.up * 2.5f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, $"State: {currentState}");
            #endif
        }
    }
    #endregion
}
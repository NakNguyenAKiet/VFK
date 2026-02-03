using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waypointWaitTime = 2f;
    
    [Header("Combat Settings")]
    [SerializeField] private float retreatDistance = 8f;
    [SerializeField] private float strafeSpeed = 2f;
    [SerializeField] private bool canStrafe = false;
    
    // Components
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyCombat combat;
    
    // State
    private AIState currentState = AIState.Idle;
    private Transform target;
    private Vector3 startPosition;
    private Vector3 currentWaypoint;
    private float waypointTimer;
    private float lastStateChangeTime;
    private float strafeDirection = 1f;
    
    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Retreat,
        Strafe,
        Dead
    }
    
    public Transform Target => target;
    public AIState CurrentState => currentState;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        combat = GetComponent<EnemyCombat>();
        
        startPosition = transform.position;
    }
    
    private void Start()
    {
        if (agent != null)
        {
            agent.speed = patrolSpeed;
        }
        
        if (shouldPatrol)
        {
            ChangeState(AIState.Patrol);
            SetNewPatrolWaypoint();
        }
        else
        {
            ChangeState(AIState.Idle);
        }
        
        // Subscribe to combat events
        if (combat != null)
        {
            combat.OnDeath += () => ChangeState(AIState.Dead);
            combat.OnDamageReceived += (damageInfo) => OnDamageTaken(damageInfo);
        }
    }
    
    private void Update()
    {
        if (currentState == AIState.Dead) return;
        
        UpdateState();
        UpdateAnimator();
    }
    
    private void UpdateState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Retreat:
                UpdateRetreat();
                break;
            case AIState.Strafe:
                UpdateStrafe();
                break;
        }
    }
    
    private void UpdateIdle()
    {
        // Look for target
        if (DetectTarget())
        {
            ChangeState(AIState.Chase);
            return;
        }
        
        // Return to patrol if enabled
        if (shouldPatrol && Time.time - lastStateChangeTime > waypointWaitTime)
        {
            ChangeState(AIState.Patrol);
        }
    }
    
    private void UpdatePatrol()
    {
        if (DetectTarget())
        {
            ChangeState(AIState.Chase);
            return;
        }
        
        if (agent.enabled && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waypointTimer += Time.deltaTime;
            
            if (waypointTimer >= waypointWaitTime)
            {
                SetNewPatrolWaypoint();
                waypointTimer = 0f;
            }
        }
    }
    
    private void UpdateChase()
    {
        if (target == null || combat.IsDead)
        {
            ChangeState(AIState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Kiểm tra target ra khỏi detection range
        if (distanceToTarget > detectionRange * 1.5f)
        {
            target = null;
            ChangeState(AIState.Idle);
            return;
        }
        
        // Chuyển sang attack khi đủ gần
        if (distanceToTarget <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }
        
        // Chase target
        if (agent.enabled)
        {
            agent.SetDestination(target.position);
        }
        
        // Rotate towards target
        RotateTowards(target.position);
    }
    
    private void UpdateAttack()
    {
        if (target == null || combat.IsDead)
        {
            ChangeState(AIState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Target đi xa quá, quay lại chase
        if (distanceToTarget > attackRange * 1.5f)
        {
            ChangeState(AIState.Chase);
            return;
        }
        
        // Dừng di chuyển
        if (agent.enabled)
        {
            agent.isStopped = true;
        }
        
        // Quay về phía target
        RotateTowards(target.position);
        
        // Strafe nếu được phép và target quá gần
        if (canStrafe && distanceToTarget < attackRange * 0.7f)
        {
            ChangeState(AIState.Strafe);
        }
    }
    
    private void UpdateRetreat()
    {
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Đã retreat đủ xa
        if (distanceToTarget >= retreatDistance)
        {
            ChangeState(AIState.Chase);
            return;
        }
        
        // Move away from target
        Vector3 retreatDirection = (transform.position - target.position).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 5f;
        
        if (agent.enabled)
        {
            agent.SetDestination(retreatPosition);
        }
        
        RotateTowards(target.position);
    }
    
    private void UpdateStrafe()
    {
        if (target == null || combat.IsDead)
        {
            ChangeState(AIState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Quá xa, quay lại chase
        if (distanceToTarget > attackRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }
        
        // Strafe around target
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Vector3 strafeRight = Vector3.Cross(Vector3.up, directionToTarget);
        Vector3 strafePosition = transform.position + strafeRight * strafeDirection * strafeSpeed * Time.deltaTime;
        
        if (agent.enabled)
        {
            agent.Move(strafeRight * strafeDirection * strafeSpeed * Time.deltaTime);
        }
        
        // Đổi hướng strafe ngẫu nhiên
        if (Random.value < 0.02f)
        {
            strafeDirection *= -1f;
        }
        
        RotateTowards(target.position);
        
        // Quay lại attack sau một lúc
        if (Time.time - lastStateChangeTime > 3f)
        {
            ChangeState(AIState.Attack);
        }
    }
    
    private bool DetectTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Raycast check line of sight
                Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
                
                if (Physics.Raycast(transform.position + Vector3.up, directionToTarget, out RaycastHit rayHit, detectionRange))
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
    
    private void SetNewPatrolWaypoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            currentWaypoint = hit.position;
            
            if (agent.enabled)
            {
                agent.SetDestination(currentWaypoint);
            }
        }
    }
    
    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        
        // Exit current state
        OnStateExit(currentState);
        
        currentState = newState;
        lastStateChangeTime = Time.time;
        
        // Enter new state
        OnStateEnter(newState);
    }
    
    private void OnStateEnter(AIState state)
    {
        switch (state)
        {
            case AIState.Idle:
                if (agent.enabled)
                {
                    agent.isStopped = true;
                    agent.speed = patrolSpeed;
                }
                break;
                
            case AIState.Patrol:
                if (agent.enabled)
                {
                    agent.isStopped = false;
                    agent.speed = patrolSpeed;
                }
                break;
                
            case AIState.Chase:
                if (agent.enabled)
                {
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                }
                break;
                
            case AIState.Attack:
                if (agent.enabled)
                {
                    agent.isStopped = true;
                }
                break;
                
            case AIState.Retreat:
                if (agent.enabled)
                {
                    agent.isStopped = false;
                    agent.speed = chaseSpeed * 1.2f;
                }
                break;
                
            case AIState.Dead:
                if (agent.enabled)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }
                enabled = false;
                break;
        }
    }
    
    private void OnStateExit(AIState state)
    {
        // Cleanup khi exit state nếu cần
    }
    
    private void UpdateAnimator()
    {
        if (animator == null || !agent.enabled) return;
        
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsAttacking", currentState == AIState.Attack);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (currentState == AIState.Idle || currentState == AIState.Patrol)
        {
            ChangeState(AIState.Chase);
        }
    }
    
    private void OnDamageTaken(DamageInfo damageInfo)
    {
        // Phát hiện attacker nếu chưa có target
        if (target == null && damageInfo.attacker != null)
        {
            Transform attackerTransform = damageInfo.attacker.transform;
            if (attackerTransform.CompareTag("Player"))
            {
                SetTarget(attackerTransform);
            }
        }
        
        // Random retreat nếu health thấp
        if (combat.CurrentHealth < combat.MaxHealth * 0.3f && Random.value < 0.3f)
        {
            ChangeState(AIState.Retreat);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Patrol radius
        if (shouldPatrol)
        {
            Gizmos.color = Color.blue;
            Vector3 patrolCenter = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawWireSphere(patrolCenter, patrolRadius);
        }
        
        // Current waypoint
        if (Application.isPlaying && currentState == AIState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentWaypoint, 0.5f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}
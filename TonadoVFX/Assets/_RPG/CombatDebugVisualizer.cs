using UnityEngine;

public class CombatDebugVisualizer : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private bool showDetectionRange = true;
    [SerializeField] private Color attackRangeColor = Color.red;
    [SerializeField] private Color detectionRangeColor = Color.yellow;
    
    private PlayerCombat playerCombat;
    private EnemyCombat enemyCombat;
    private EnemyAI enemyAI;
    
    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        enemyCombat = GetComponent<EnemyCombat>();
        enemyAI = GetComponent<EnemyAI>();
    }
    
    private void OnDrawGizmos()
    {
        if (showAttackRange)
        {
            DrawAttackRange();
        }
        
        if (showDetectionRange)
        {
            DrawDetectionRange();
        }
    }
    
    private void DrawAttackRange()
    {
        Gizmos.color = attackRangeColor;
        
        float range = 2f;
        if (playerCombat != null)
        {
            range = playerCombat.AttackRange;
        }
        else if (enemyCombat != null)
        {
            range = enemyCombat.AttackRange;
        }
        
        Gizmos.DrawWireSphere(transform.position, range);
    }
    
    private void DrawDetectionRange()
    {
        if (enemyAI == null) return;
        
        Gizmos.color = detectionRangeColor;
        // Access detection range through reflection or make it public
        Gizmos.DrawWireSphere(transform.position, 15f);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }
}

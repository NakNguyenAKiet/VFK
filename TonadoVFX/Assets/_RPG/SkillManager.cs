using UnityEngine;

/// <summary>
/// Basic Skill Manager - Placeholder for future skill system implementation
/// </summary>
public class SkillManager : MonoBehaviour
{
    #region Singleton
    public static SkillManager Instance { get; private set; }
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Skill Management
    /// <summary>
    /// Use skill at given index
    /// </summary>
    public void UseSkill(int skillIndex, CombatEntity caster)
    {
        Debug.Log($"[SkillManager] Skill {skillIndex} used by {caster.name} - NOT IMPLEMENTED YET");
        
        // TODO: Implement skill system
        // - Check cooldown
        // - Check mana cost
        // - Execute skill effect
        // - Start cooldown
    }

    /// <summary>
    /// Check if skill is ready to use
    /// </summary>
    public bool IsSkillReady(int skillIndex)
    {
        // TODO: Implement cooldown check
        return true;
    }

    /// <summary>
    /// Get remaining cooldown for skill
    /// </summary>
    public float GetSkillCooldown(int skillIndex)
    {
        // TODO: Implement cooldown tracking
        return 0f;
    }
    #endregion
}
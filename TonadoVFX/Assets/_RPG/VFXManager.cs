using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }
    
    [System.Serializable]
    public class VFXData
    {
        public string name;
        public GameObject prefab;
        public int poolSize = 10;
    }
    
    [Header("VFX Prefabs")]
    [SerializeField] private VFXData[] vfxPrefabs;
    
    [Header("Default Effects")]
    [SerializeField] private GameObject defaultHitEffect;
    [SerializeField] private GameObject criticalHitEffect;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject levelUpEffect;
    
    private Dictionary<string, Queue<GameObject>> vfxPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> vfxPrefabDict = new Dictionary<string, GameObject>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePools();
    }
    
    private void InitializePools()
    {
        // Pool default effects
        if (defaultHitEffect != null)
            CreatePool("DefaultHit", defaultHitEffect, 20);
        if (criticalHitEffect != null)
            CreatePool("CriticalHit", criticalHitEffect, 15);
        if (bloodEffect != null)
            CreatePool("Blood", bloodEffect, 20);
        if (healEffect != null)
            CreatePool("Heal", healEffect, 10);
        if (levelUpEffect != null)
            CreatePool("LevelUp", levelUpEffect, 5);
        
        // Pool custom VFX
        foreach (VFXData vfxData in vfxPrefabs)
        {
            if (vfxData.prefab != null)
            {
                CreatePool(vfxData.name, vfxData.prefab, vfxData.poolSize);
            }
        }
    }
    
    private void CreatePool(string poolName, GameObject prefab, int poolSize)
    {
        Queue<GameObject> pool = new Queue<GameObject>();
        vfxPrefabDict[poolName] = prefab;
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        
        vfxPools[poolName] = pool;
    }
    
    public GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation = default, Transform parent = null, float duration = 2f)
    {
        if (rotation == default)
            rotation = Quaternion.identity;
        
        GameObject effect = GetFromPool(effectName);
        
        if (effect == null)
        {
            Debug.LogWarning($"VFX '{effectName}' not found in pool!");
            return null;
        }
        
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        effect.transform.parent = parent;
        effect.SetActive(true);
        
        // Auto return to pool
        StartCoroutine(ReturnToPoolAfterDelay(effectName, effect, duration));
        
        // Play particle system if exists
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
        }
        
        return effect;
    }
    
    public void PlayHitEffect(Vector3 position, bool isCritical = false)
    {
        string effectName = isCritical ? "CriticalHit" : "DefaultHit";
        PlayEffect(effectName, position, Quaternion.identity, null, 1f);
    }
    
    public void PlayBloodEffect(Vector3 position, Vector3 hitDirection)
    {
        Quaternion rotation = Quaternion.LookRotation(hitDirection);
        PlayEffect("Blood", position, rotation, null, 1.5f);
    }
    
    public void PlayHealEffect(Vector3 position, Transform parent = null)
    {
        PlayEffect("Heal", position, Quaternion.identity, parent, 1f);
    }
    
    public void PlayLevelUpEffect(Transform target)
    {
        PlayEffect("LevelUp", target.position, Quaternion.identity, target, 2f);
    }
    
    public void PlaySkillEffect(string skillName, Vector3 position, Quaternion rotation)
    {
        PlayEffect(skillName, position, rotation, null, 3f);
    }
    
    private GameObject GetFromPool(string poolName)
    {
        if (!vfxPools.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' does not exist!");
            return null;
        }
        
        Queue<GameObject> pool = vfxPools[poolName];
        
        if (pool.Count == 0)
        {
            // Expand pool if empty
            GameObject prefab = vfxPrefabDict[poolName];
            GameObject newObj = Instantiate(prefab, transform);
            newObj.SetActive(false);
            return newObj;
        }
        
        return pool.Dequeue();
    }
    
    private void ReturnToPool(string poolName, GameObject obj)
    {
        if (!vfxPools.ContainsKey(poolName))
        {
            Destroy(obj);
            return;
        }
        
        obj.SetActive(false);
        obj.transform.parent = transform;
        
        // Stop all particle systems
        ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
            ps.Clear();
        }
        
        vfxPools[poolName].Enqueue(obj);
    }
    
    private System.Collections.IEnumerator ReturnToPoolAfterDelay(string poolName, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null && obj.activeSelf)
        {
            ReturnToPool(poolName, obj);
        }
    }
    
    // Trail effects for moving objects
    public GameObject AttachTrailEffect(Transform target, string effectName)
    {
        GameObject trail = GetFromPool(effectName);
        if (trail != null)
        {
            trail.transform.parent = target;
            trail.transform.localPosition = Vector3.zero;
            trail.transform.localRotation = Quaternion.identity;
            trail.SetActive(true);
        }
        return trail;
    }
    
    public void DetachTrailEffect(string effectName, GameObject trail)
    {
        if (trail != null)
        {
            trail.transform.parent = transform;
            StartCoroutine(ReturnToPoolAfterDelay(effectName, trail, 2f));
        }
    }
}
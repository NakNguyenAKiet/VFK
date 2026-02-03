using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }
    
    [System.Serializable]
    public class ProjectilePoolData
    {
        public string poolName;
        public GameObject prefab;
        public int initialSize = 20;
        public bool canExpand = true;
    }
    
    [Header("Pool Settings")]
    [SerializeField] private ProjectilePoolData[] poolsData;
    
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, bool> canExpand = new Dictionary<string, bool>();
    private Dictionary<GameObject, string> activeObjects = new Dictionary<GameObject, string>();
    
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
        foreach (ProjectilePoolData data in poolsData)
        {
            CreatePool(data.poolName, data.prefab, data.initialSize, data.canExpand);
        }
    }
    
    public void CreatePool(string poolName, GameObject prefab, int size, bool expandable = true)
    {
        if (pools.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' already exists!");
            return;
        }
        
        Queue<GameObject> pool = new Queue<GameObject>();
        prefabs[poolName] = prefab;
        canExpand[poolName] = expandable;
        
        for (int i = 0; i < size; i++)
        {
            GameObject obj = CreatePooledObject(poolName, prefab);
            pool.Enqueue(obj);
        }
        
        pools[poolName] = pool;
    }
    
    private GameObject CreatePooledObject(string poolName, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.name = $"{poolName}_{pools[poolName].Count}";
        obj.SetActive(false);
        
        // Add PooledObject component để track
        PooledObject pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj == null)
        {
            pooledObj = obj.AddComponent<PooledObject>();
        }
        pooledObj.PoolName = poolName;
        
        return obj;
    }
    
    public GameObject Get(string poolName = "Default")
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' does not exist!");
            return null;
        }
        
        Queue<GameObject> pool = pools[poolName];
        
        // Pool empty và không thể expand
        if (pool.Count == 0 && !canExpand[poolName])
        {
            Debug.LogWarning($"Pool '{poolName}' is empty and cannot expand!");
            return null;
        }
        
        // Pool empty, tạo object mới
        if (pool.Count == 0)
        {
            GameObject newObj = CreatePooledObject(poolName, prefabs[poolName]);
            pool.Enqueue(newObj);
        }
        
        GameObject obj = pool.Dequeue();
        obj.SetActive(true);
        activeObjects[obj] = poolName;
        
        return obj;
    }
    
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        
        if (!activeObjects.ContainsKey(obj))
        {
            Debug.LogWarning($"Object '{obj.name}' is not from any pool!");
            Destroy(obj);
            return;
        }
        
        string poolName = activeObjects[obj];
        activeObjects.Remove(obj);
        
        obj.SetActive(false);
        obj.transform.parent = transform;
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        
        // Reset component nếu có
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Reset();
        }
        
        pools[poolName].Enqueue(obj);
    }
    
    public void ReturnAll()
    {
        List<GameObject> objectsToReturn = new List<GameObject>(activeObjects.Keys);
        
        foreach (GameObject obj in objectsToReturn)
        {
            Return(obj);
        }
    }
    
    public void ClearPool(string poolName)
    {
        if (!pools.ContainsKey(poolName)) return;
        
        Queue<GameObject> pool = pools[poolName];
        
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            Destroy(obj);
        }
        
        pools.Remove(poolName);
        prefabs.Remove(poolName);
        canExpand.Remove(poolName);
    }
    
    public void ClearAllPools()
    {
        List<string> poolNames = new List<string>(pools.Keys);
        
        foreach (string poolName in poolNames)
        {
            ClearPool(poolName);
        }
        
        activeObjects.Clear();
    }
    
    public int GetPoolSize(string poolName)
    {
        if (!pools.ContainsKey(poolName)) return 0;
        return pools[poolName].Count;
    }
    
    public int GetActiveCount(string poolName)
    {
        int count = 0;
        foreach (var kvp in activeObjects)
        {
            if (kvp.Value == poolName)
                count++;
        }
        return count;
    }
}

// Component để track pooled objects
public class PooledObject : MonoBehaviour
{
    public string PoolName { get; set; }
    
    public void ReturnToPool()
    {
        ProjectilePool.Instance?.Return(gameObject);
    }
}
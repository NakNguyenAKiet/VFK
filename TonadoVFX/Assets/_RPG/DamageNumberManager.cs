using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }
    
    [Header("Prefabs")]
    [SerializeField] private GameObject damageNumberPrefab;
    
    [Header("Settings")]
    [SerializeField] private int poolSize = 50;
    [SerializeField] private float normalDuration = 1f;
    [SerializeField] private float criticalDuration = 1.5f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color criticalDamageColor = Color.yellow;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private float normalFontSize = 24f;
    [SerializeField] private float criticalFontSize = 36f;
    
    [Header("Movement")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float randomOffsetRange = 0.5f;
    
    private Queue<DamageNumber> damageNumberPool = new Queue<DamageNumber>();
    private List<DamageNumber> activeDamageNumbers = new List<DamageNumber>();
    private Camera mainCamera;
    
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
        
        mainCamera = Camera.main;
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateDamageNumber();
        }
    }
    
    private DamageNumber CreateDamageNumber()
    {
        GameObject obj = Instantiate(damageNumberPrefab, transform);
        DamageNumber damageNum = obj.GetComponent<DamageNumber>();
        
        if (damageNum == null)
        {
            damageNum = obj.AddComponent<DamageNumber>();
        }
        
        obj.SetActive(false);
        damageNumberPool.Enqueue(damageNum);
        
        return damageNum;
    }
    
    public void ShowDamage(Vector3 worldPosition, float damage, bool isCritical = false)
    {
        DamageNumber damageNum = GetDamageNumber();
        
        // Random offset để các số không chồng lên nhau
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange, randomOffsetRange),
            Random.Range(0, randomOffsetRange),
            Random.Range(-randomOffsetRange, randomOffsetRange)
        );
        
        damageNum.Show(
            worldPosition + randomOffset,
            Mathf.RoundToInt(damage),
            isCritical ? criticalDamageColor : normalDamageColor,
            isCritical ? criticalFontSize : normalFontSize,
            isCritical ? criticalDuration : normalDuration,
            isCritical
        );
        
        activeDamageNumbers.Add(damageNum);
    }
    
    public void ShowHealing(Vector3 worldPosition, float healAmount)
    {
        DamageNumber damageNum = GetDamageNumber();
        
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange, randomOffsetRange),
            Random.Range(0, randomOffsetRange),
            Random.Range(-randomOffsetRange, randomOffsetRange)
        );
        
        damageNum.Show(
            worldPosition + randomOffset,
            Mathf.RoundToInt(healAmount),
            healColor,
            normalFontSize,
            normalDuration,
            false,
            "+"
        );
        
        activeDamageNumbers.Add(damageNum);
    }
    
    private DamageNumber GetDamageNumber()
    {
        if (damageNumberPool.Count == 0)
        {
            return CreateDamageNumber();
        }
        
        return damageNumberPool.Dequeue();
    }
    
    public void ReturnDamageNumber(DamageNumber damageNum)
    {
        damageNum.gameObject.SetActive(false);
        activeDamageNumbers.Remove(damageNum);
        damageNumberPool.Enqueue(damageNum);
    }
    
    private void Update()
    {
        // Update positions để luôn face camera
        for (int i = activeDamageNumbers.Count - 1; i >= 0; i--)
        {
            if (activeDamageNumbers[i].gameObject.activeSelf)
            {
                activeDamageNumbers[i].transform.LookAt(
                    activeDamageNumbers[i].transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up
                );
            }
        }
    }
}


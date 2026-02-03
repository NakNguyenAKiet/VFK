using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float displayDuration;
    private float spawnTime;
    private Vector3 startPosition;
    private float floatSpeed = 2f;
    private bool isCritical;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
            textMesh.alignment = TextAlignmentOptions.Center;
        }
    }
    
    public void Show(Vector3 position, int damage, Color color, float fontSize, float duration, bool critical, string prefix = "")
    {
        transform.position = position;
        startPosition = position;
        spawnTime = Time.time;
        displayDuration = duration;
        isCritical = critical;
        floatSpeed = DamageNumberManager.Instance != null ? 2f : floatSpeed;
        
        textMesh.text = prefix + damage.ToString();
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        
        if (critical)
        {
            textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            textMesh.fontStyle = FontStyles.Normal;
        }
        
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        if (!gameObject.activeSelf) return;
        
        float elapsed = Time.time - spawnTime;
        float progress = elapsed / displayDuration;
        
        // Float upward
        transform.position = startPosition + Vector3.up * (floatSpeed * elapsed);
        
        // Fade out
        Color currentColor = textMesh.color;
        currentColor.a = 1f - progress;
        textMesh.color = currentColor;
        
        // Scale effect for critical
        if (isCritical)
        {
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.3f;
            transform.localScale = Vector3.one * scale;
        }
        
        // Return to pool when done
        if (progress >= 1f)
        {
            DamageNumberManager.Instance?.ReturnDamageNumber(this);
        }
    }
}
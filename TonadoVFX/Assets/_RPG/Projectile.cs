using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool homing = false;
    [SerializeField] private float homingStrength = 3f;
    
    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem particles;
    
    [Header("Physics")]
    [SerializeField] private bool useGravity = false;
    [SerializeField] private float gravityMultiplier = 1f;
    
    private Transform target;
    private float damage;
    private GameObject attacker;
    private System.Action onHitCallback;
    private float spawnTime;
    private Vector3 velocity;
    private bool isActive = false;
    
    public void Initialize(Transform target, float damage, GameObject attacker, System.Action onHit)
    {
        this.target = target;
        this.damage = damage;
        this.attacker = attacker;
        this.onHitCallback = onHit;
        this.spawnTime = Time.time;
        this.isActive = true;
        
        // Reset velocity
        velocity = transform.forward * speed;
        
        // Reset trail
        if (trail != null)
        {
            trail.Clear();
        }
        
        // Play particles
        if (particles != null)
        {
            particles.Play();
        }
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Lifetime check
        if (Time.time - spawnTime > lifetime)
        {
            ReturnToPool();
            return;
        }
        
        // Target null check
        if (target == null)
        {
            ReturnToPool();
            return;
        }
        
        // Homing behavior
        if (homing && target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            velocity = Vector3.Lerp(velocity.normalized, directionToTarget, homingStrength * Time.deltaTime) * speed;
        }
        
        // Apply gravity
        if (useGravity)
        {
            velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }
        
        // Move
        transform.position += velocity * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(velocity);
        
        // Check collision
        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            Hit();
        }
    }
    
    private void Hit()
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            DamageInfo damageInfo = new DamageInfo
            {
                physicalDamage = damage,
                damageType = DamageInfo.DamageType.Physical,
                attacker = attacker,
                hitPoint = transform.position,
                hitDirection = velocity.normalized
            };
            
            damageable.TakeDamage(damageInfo);
        }
        
        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.LookRotation(velocity));
            Destroy(effect, 2f);
        }
        else
        {
            VFXManager.Instance?.PlayHitEffect(transform.position);
        }
        
        onHitCallback?.Invoke();
        ReturnToPool();
    }
    
    public void Reset()
    {
        isActive = false;
        target = null;
        attacker = null;
        onHitCallback = null;
        velocity = Vector3.zero;
        
        if (trail != null)
        {
            trail.Clear();
        }
        
        if (particles != null)
        {
            particles.Stop();
            particles.Clear();
        }
    }
    
    private void ReturnToPool()
    {
        Reset();
        ProjectilePool.Instance?.Return(gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Collision với terrain hoặc obstacles
        if (other.CompareTag("Terrain") || other.CompareTag("Obstacle"))
        {
            VFXManager.Instance?.PlayHitEffect(transform.position);
            ReturnToPool();
        }
    }
}
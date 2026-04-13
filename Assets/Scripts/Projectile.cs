using UnityEngine;
using WeaponSystem;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private float lifeTime;
    private float damage;
    private Rigidbody rb;
    private float spawnTime;
    public bool destroyOnImpact = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 velocity, float damage, float lifeTime)
    {
        this.damage = damage;
        this.lifeTime = lifeTime;
        this.spawnTime = Time.time;
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        if (Time.time - spawnTime > lifeTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Damageable.TryGetDamageable(other.gameObject, out var dmg))
        {
            dmg.TakeDamage(damage);
        }

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }
}

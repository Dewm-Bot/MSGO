using UnityEngine;
using System.Collections.Generic;

namespace WeaponSystem
{
    public class Damageable : MonoBehaviour
    {
        public float health = 100f;

        private static readonly Dictionary<EntityId, Damageable> Cache = new Dictionary<EntityId, Damageable>();

        void Awake()
        {
            Cache[gameObject.GetEntityId()] = this;
        }

        void OnDestroy()
        {
            Cache.Remove(gameObject.GetEntityId());
        }

        public static bool TryGetDamageable(GameObject obj, out Damageable damageable)
        {
            return Cache.TryGetValue(obj.GetEntityId(), out damageable);
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
            if (health <= 0f)
                Die();
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
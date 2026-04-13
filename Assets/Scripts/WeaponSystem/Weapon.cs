using UnityEngine;

namespace WeaponSystem
{
    public enum LaunchMode { Hitscan, Projectile }


    /// Base class for all weapons. Implements only the actual shot logic, not any player input or timing logic.
    public abstract class Weapon : ScriptableObject
    {
        [Header("Common Weapon Settings")]
        public string weaponName;
        public LaunchMode launchMode = LaunchMode.Hitscan;
        public Projectile projectilePrefab;         // Only used if launchMode == Projectile
        public Transform projectileSpawnPoint;      // Where to spawn the projectile (falls back to muzzle if null)
        public float projectileSpeed = 20f;         // Applied to Rigidbody if projectile instantiates
        public float projectileLifetime = 5f;       // Seconds before projectile is destroyed
        public float fireRate = 10f;               // Rounds per second (enforced by PlayerController)
        public float hitscanRange = 100f;          // Only used if launchMode == Hitscan
        public LayerMask hitLayers = ~0;           // Which layers are hittable via hitscan
        public float damage = 10f;
        
        // Called by PlayerController whenever it wants to actually shoot
        // It does not check any cooldown. PlayerController guarantees timing.
        public abstract void FireShot(Transform muzzleTransform, Vector3 aimPoint);


        // If the player switches away from this weapon mid-use (e.g. mid-charge), 
        // cancel any VFX. (Timing is still fully in PlayerController.)
        public virtual void OnDeselect() { }
    }
}
using UnityEngine;

namespace WeaponSystem
{
    [CreateAssetMenu(menuName = "Weapons/SemiAuto")]
    public class SemiAutoWeapon : Weapon
    {
        public override void FireShot(Transform muzzle, Vector3 aimPoint)
        {
            if (launchMode == LaunchMode.Hitscan)
            {
                Vector3 dir = (aimPoint - muzzle.position).normalized;
                if (Physics.Raycast(muzzle.position, dir, out RaycastHit hit, hitscanRange, hitLayers))
                {
                    if (Damageable.TryGetDamageable(hit.collider.gameObject, out var targetHealth))
                    {
                        targetHealth.TakeDamage(damage);
                    }
                    // (Optional) spawn impact VFX here
                }
            }
            else // Projectile
            {
                if (projectilePrefab)
                {
                    Transform spawnPt = projectileSpawnPoint ? projectileSpawnPoint : muzzle;
                    Vector3 toAimPoint = aimPoint - spawnPt.position;

                    // Use the computed aim direction, clamp if value is too small
                    Vector3 fireDirection = toAimPoint.sqrMagnitude < 0.0001f ? muzzle.forward : toAimPoint.normalized;

                    Projectile proj = Instantiate(
                        projectilePrefab,
                        spawnPt.position,
                        Quaternion.LookRotation(fireDirection)
                    );
                    if (proj)
                        proj.Initialize(fireDirection * projectileSpeed, damage, projectileLifetime);
                }
            }
        }
    }
}
using UnityEngine;

namespace WeaponSystem
{
    [CreateAssetMenu(menuName = "Weapons/ChargeBased")]
    public class ChargeBasedWeapon : Weapon
    {
        [Header("Charge Weapon Settings")]
        public float minChargeTime = 0.2f;  // Minimum time to charge before firing
        public float maxChargeTime = 2f;    // Seconds to fully charge
        public float minDamage = 5f;        // Damage at min charge time
        public float chargedDamage = 50f;   // Damage if fully charged
        [Tooltip("Curve to control damage scaling from min to max over charge time. X-axis is normalized charge time (0-1), Y-axis is damage multiplier (0-1).")]
        public AnimationCurve damageScaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public GameObject chargeVFX;        // "VFX" Object to display while charging up the beam
        public bool burst = false;          // Optional burst fire
        public float burstTime = 0.5f;      // Duration of the burst

        private float chargeStartTime;
        private bool isCharging = false;
        private bool fired = false;

        
        public void StartCharging()
        {
            if (chargeVFX)
                chargeVFX.SetActive(true);
            chargeStartTime = Time.time;
            isCharging = true;
            fired = false;
        }

        
        public void ReleaseCharge(Transform muzzle, Vector3 aimPoint)
        {
            if (!isCharging || fired) // Prevent rapid fire
                return;

            float heldTime = Time.time - chargeStartTime;
            if (heldTime < minChargeTime)
            {
                isCharging = false;
                if (chargeVFX)
                    chargeVFX.SetActive(false);
                return;
            }

            if (!burst)
            {
                FireSingleShot(muzzle, aimPoint);
            }

            fired = true; // Mark as fired to prevent re-firing
            isCharging = false;
            if (chargeVFX)
                chargeVFX.SetActive(false);
        }

        public bool IsCharging()
        {
            return isCharging; // Allows us to externally check charge state
        }

        public void FireSingleShot(Transform muzzle, Vector3 aimPoint)
        {
            float heldTime = Time.time - chargeStartTime;
            float normalizedCharge = Mathf.Clamp01((heldTime - minChargeTime) / (maxChargeTime - minChargeTime));
            float curveValue = damageScaleCurve.Evaluate(normalizedCharge); //Checks how much damage we do based on the animation curve
            float finalDamage = Mathf.Lerp(minDamage, chargedDamage, curveValue);

            if (launchMode == LaunchMode.Hitscan)
            {
                Vector3 dir = (aimPoint - muzzle.position).normalized;
                if (Physics.Raycast(muzzle.position, dir, out RaycastHit hit, hitscanRange, hitLayers))
                {
                    if (Damageable.TryGetDamageable(hit.collider.gameObject, out var targetHealth))
                    {
                        targetHealth.TakeDamage(finalDamage);
                    }
                }
            }
            else // Projectile
            {
                if (projectilePrefab)
                {
                    Transform spawnPt = projectileSpawnPoint ? projectileSpawnPoint : muzzle;
                    Vector3 toAimPoint = aimPoint - spawnPt.position;

                    Vector3 fireDirection = toAimPoint.sqrMagnitude < 0.0001f ? muzzle.forward : toAimPoint.normalized;

                    Projectile proj = Instantiate(
                        projectilePrefab,
                        spawnPt.position,
                        Quaternion.LookRotation(fireDirection)
                    );
                    if (proj)
                        proj.Initialize(fireDirection * projectileSpeed, finalDamage, projectileLifetime);
                }
            }
        }

        public override void OnDeselect()
        {
            isCharging = false;
            if (chargeVFX)
                chargeVFX.SetActive(false);
        }

        public override void FireShot(Transform muzzle, Vector3 aimPoint)
        {
            // Not used for CBWs; actual firing is entirely in ReleaseCharge(...)
        }
    }
}

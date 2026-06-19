using UnityEngine;

namespace WeaponSystem
{
    [CreateAssetMenu(menuName = "Weapons/ChargeBased")]
    public class ChargeBasedWeapon : Gun
    {
        /// <summary>
        /// A complex example of taking the standard "Gun" template
        /// 
        /// </summary>

        [Header("Charge Weapon Settings")]
        public float minChargeTime = 0.2f;  // Minimum time to charge before firing
        public float maxChargeTime = 2f;    // Seconds to fully charge
        public float minDamage = 5f;        // Damage at min charge time
        public float chargedDamage = 50f;   // Damage if fully charged

        [Tooltip("Curve to control damage scaling from min to max over charge time. X-axis is normalized charge time (0-1), Y-axis is damage multiplier (0-1).")]
        public AnimationCurve damageScaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public GameObject chargeVFX;        // "VFX" Object to display while charging up the beam
        public bool burst = false;          // Optional burst fire
        public int burstCount = 3;          // Shots in burst-
        public float burstTime = 0.5f;      // Duration of the burst

        private float chargeStartTime;
        private bool isCharging = false;
        private bool fired = false;

        [SerializeField] GameObject projectile_tap;
        [SerializeField] GameObject projectile_charge;
        [SerializeField] Projectile projectile_behavior;
        [SerializeField] Transform muzzle;

        private void Start()
        {
            projectile_behavior = projectile_charge.GetComponent<Projectile>();
        }

        public override void Activate(bool inputPress, bool inputHeld)
        {
            if (inputPress)
            {
                Debug.Log("Charging...");
                StartCharging();
            }
            else if (isCharging) 
            {
                Debug.Log("Charge is " + Mathf.Clamp01(((Time.time - chargeStartTime) - minChargeTime) / (maxChargeTime - minChargeTime)));
                if (!inputHeld) 
                {
                    // if the gun was charging but no more inputs can be found, assume we have fired and release the charge
                    FireGun();
                }
            }
        }

        public void StartCharging()
        {
            if (chargeVFX)
                chargeVFX.SetActive(true);
            chargeStartTime = Time.time;
            isCharging = true;
            fired = false;
        }

        
        public void ReleaseCharge()
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
                FireSingleShot();
            }
            else 
            {
                FireBurst();
            }

            fired = true; // Mark as fired to prevent re-firing
            isCharging = false;
            if (chargeVFX)
                chargeVFX.SetActive(false);
        }

        public void FireSingleShot()
        {
            float heldTime = Time.time - chargeStartTime;
            float normalizedCharge = Mathf.Clamp01((heldTime - minChargeTime) / (maxChargeTime - minChargeTime));
            float curveValue = damageScaleCurve.Evaluate(normalizedCharge); //Checks how much damage we do based on the animation curve
            float finalDamage = Mathf.Lerp(minDamage, chargedDamage, curveValue);

            projectile_behavior.damage = finalDamage;

            Transform shot = Instantiate(projectile_charge).transform;
            shot.position = muzzle.position;
            shot.rotation = Quaternion.LookRotation(aimPoint - muzzle.position, Vector3.up);
        }

        public void FireBurst()
        {
            float heldTime = Time.time - chargeStartTime;
            float normalizedCharge = Mathf.Clamp01((heldTime - minChargeTime) / (maxChargeTime - minChargeTime));
            float curveValue = damageScaleCurve.Evaluate(normalizedCharge); //Checks how much damage we do based on the animation curve
            float finalDamage = Mathf.Lerp(minDamage, chargedDamage, curveValue);

            projectile_behavior.damage = finalDamage;

            for (int burstShot = 0; burstShot < burstCount; burstShot++) 
            {
                Transform shot = Instantiate(projectile_charge).transform;
                shot.position = muzzle.position;
                shot.rotation = Quaternion.LookRotation(aimPoint - muzzle.position, Vector3.up);
            }
        }

        public override void FireBehavior()
        {
            ReleaseCharge();
            Debug.Log("Charge was " + Mathf.Clamp01(((Time.time - chargeStartTime) - minChargeTime) / (maxChargeTime - minChargeTime)));
        }

        public Transform stored_hardpoint;
        public Vector3 stored_offset;
        public Vector3 stored_rotation;

        public Transform equipped_hardpoint;
        public Vector3 equipped_offset;
        public Vector3 equipped_rotation;

        public override void OnSelect()
        {
            this.gameObject.transform.parent = equipped_hardpoint;
            this.gameObject.transform.localPosition = equipped_offset;
            this.gameObject.transform.localRotation = Quaternion.Euler(equipped_rotation);
            if (!stored_hardpoint)
            {
                this.gameObject.SetActive(true);
            }
        }

        public override void OnDeselect()
        {
            isCharging = false;
            if (chargeVFX)
                chargeVFX.SetActive(false);

            if (stored_hardpoint) {
                this.gameObject.transform.parent = stored_hardpoint;
                this.gameObject.transform.localPosition = stored_offset;
                this.gameObject.transform.localRotation = Quaternion.Euler(stored_rotation);
            }
            else
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}

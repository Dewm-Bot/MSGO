using UnityEngine;

namespace WeaponSystem
{
    [CreateAssetMenu(menuName = "Weapons/FullAuto")]
    public class FullAutoWeapon : Gun
    {
        [SerializeField] GameObject projectile;
        [SerializeField] Projectile projectile_behavior;
        [SerializeField] Transform muzzle;

        [SerializeField] float accuracy;
        private void Start()
        {
            projectile_behavior = projectile.GetComponent<Projectile>();
        }

        public override void Activate(bool inputPress, bool inputHeld)
        {
            if (inputPress || inputHeld) 
            {
                FireGun();
            }
        }

        public override void FireBehavior()
        {
            Transform shot = Instantiate(projectile).transform;
            shot.position = muzzle.position;
            shot.rotation = Deviation(muzzle.rotation, accuracy);
        }

        Quaternion Deviation(Quaternion initial_rotation, float deviance)
        {
            Vector3 to_euler = initial_rotation.eulerAngles;
            float offset_x = to_euler.x + Random.Range(-deviance, deviance);
            float offset_y = to_euler.y + Random.Range(-deviance, deviance);
            float offset_z = to_euler.z + Random.Range(-deviance, deviance);
            return Quaternion.Euler(new Vector3(offset_x, offset_y));
        }

        public Transform stored_hardpoint;
        public Vector3 stored_offset;
        public Vector3 stored_rotation;

        public Transform equipped_hardpoint;
        public Vector3 equipped_offset;
        public Vector3 equipped_rotation;

        public override void OnSelect()
        {
            if (stored_hardpoint)
            {
                this.gameObject.transform.parent = equipped_hardpoint;
                this.gameObject.transform.localPosition = equipped_offset;
                this.gameObject.transform.localRotation = Quaternion.Euler(equipped_rotation);
            }
            else
            {
                this.gameObject.SetActive(true);
            }

        }

        public override void OnDeselect()
        {
            if (stored_hardpoint)
            {
                this.gameObject.transform.parent = stored_hardpoint;
                this.gameObject.transform.localPosition = stored_offset;
                this.gameObject.transform.localRotation = Quaternion.Euler(stored_rotation);
            }
            else
            {
                this.gameObject.SetActive(false);
            }
        }

        public Transform GetMuzzle()
        {
            return muzzle;
        }
    }
}
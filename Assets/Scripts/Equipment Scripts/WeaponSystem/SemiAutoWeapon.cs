using UnityEngine;

namespace WeaponSystem
{
    [CreateAssetMenu(menuName = "Weapons/SemiAuto")]
    public class SemiAutoWeapon : Gun
    {
        [SerializeField] GameObject projectile;
        [SerializeField] Projectile projectile_behavior;
        [SerializeField] Transform muzzle;

        [SerializeField] float accuracy;

        private void Start()
        {
            projectile_behavior = projectile.GetComponent<Projectile>();
        }

        public override void FireBehavior()
        {
            projectile_behavior.SetInitialVelocity(Vector3.zero);

            Transform shot = Instantiate(projectile).transform;
            shot.position = muzzle.position;
            shot.rotation = muzzle.rotation;
        }

        public Transform stored_hardpoint;
        public Vector3 stored_offset;
        public Vector3 stored_rotation;

        public Transform equipped_hardpoint;
        public Vector3 equipped_offset;
        public Vector3 equipped_rotation;

        public override void OnSelect()
        {
            Debug.Log("Selected and equipping!");
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
            Debug.Log("Storing!");
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
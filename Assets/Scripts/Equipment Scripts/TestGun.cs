using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGun : Gun
{
    [SerializeField] GameObject projectile;
    [SerializeField] Projectile projectile_behavior;
    [SerializeField] Transform muzzle;

    [SerializeField] Entity inheret_object_velocity;

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
        projectile_behavior.SetInitialVelocity(inheret_object_velocity.GetVelocity());
        projectile_behavior.SetInitialVelocity(Vector3.zero);

        Transform shot = Instantiate(projectile).transform;
        shot.position = muzzle.position;
        shot.rotation = Deviation(muzzle.rotation, accuracy);
    }

    Quaternion Deviation(Quaternion initial_rotation, float deviance) 
    {
        Vector3 to_euler = initial_rotation.eulerAngles;
        float offset_x = to_euler.x + Random.RandomRange(-deviance, deviance);
        float offset_y = to_euler.y + Random.RandomRange(-deviance, deviance);
        float offset_z = to_euler.z + Random.RandomRange(-deviance, deviance);
        return Quaternion.Euler(new Vector3(offset_x, offset_y));
    }
}

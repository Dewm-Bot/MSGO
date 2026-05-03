using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : Entity
{
    /// <summary>
    /// A general projectile class
    /// This is usually what will be spawned when we want something to shoot
    /// By changing up the ProjectileBehavior() function
    /// We can make any kind of projectile we want.
    /// This is where the actual "bullet" is defined, if it is a physical gameobject or a hitscan beam.
    /// 
    ///If we wanted to make a rocket, we would:
    ///     attach this script to a gameobject with a rocket model, 
    ///     then define the movement and rotation in the ProjectileBehavior script,
    ///     check if/what the missile has hit,
    ///     and apply damage if possible,
    ///     then despawn
    /// 
    /// If we wanted to make a missile, we would:
    ///     attach this script to a gameobject with a rocket model, 
    ///     add a transformation variable called "target",
    ///     add a function to handle projectile rotation to the target,
    ///     then define the movement and rotation in the ProjectileBehavior script,
    ///     check if/what the missile has hit,
    ///     and apply damage if possible,
    ///     then despawn
    ///     
    /// If we wanted to make a hitscan beam, we would:
    ///     attach this script to an empty gameobject,
    ///     edit the projectile behavior to fire a raycast on spawn out some number of units,
    ///     then check if the raycast has hit,
    ///     check what the raycast hit if it hit,
    ///     then apply damage if possible,
    ///     then despawn
    ///     
    /// </summary>

    [SerializeField] float speed = 100f;
    [SerializeField] public float damage = 0f;
    [SerializeField] float lifetime = 30f;

    //Character hit_target;
    float projectile_size = 0.1f;

    Vector3 initial_velocity; // inheret movement by the firing object.

    private void Start()
    {
        StartCoroutine(ProjectileLifetime(lifetime));
    }

    public override Vector3 Movement()
    {
        return ProjectileBehavior();
    }

    virtual public Vector3 ProjectileBehavior() 
    {
        Vector3 velocity = this.transform.forward * speed + initial_velocity;
        RaycastHit hit;
        if (Physics.SphereCast(this.transform.position, projectile_size, this.transform.forward, out hit, speed, mask)) 
        {
            GameObject hitmarker = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Sphere), hit.point, Quaternion.Euler(hit.normal));
            /*
            if (hit_target = hit.transform.GetComponent<Character>()) 
            {
                Health target_health = hit_target.GetHealthScript();
                target_health.TakeDamage(damage);
            }
            */
            Destroy(this.gameObject);
        }
        return velocity;
    }

    public void SetInitialVelocity(Vector3 velocity) 
    {
        initial_velocity = velocity;
    }

    IEnumerator ProjectileLifetime(float seconds) 
    {
        yield return new WaitForSeconds(seconds);
        Destroy(this.gameObject);
    }
}

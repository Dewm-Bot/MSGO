using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    /// <summary>
    /// A generalized class to help define all moving objects, currently only used for projectiles
    /// </summary>

    [SerializeField] CharacterController temp_char;
    Vector3 current_velocity;
    [SerializeField] bool cap_velocities;
    [SerializeField] Vector3 max_velocity;

    float gravity_acceleration = 9.81f;
    Vector3 gravity_direction = -Vector3.up; // incase it needs to be changed

    private void FixedUpdate()
    {
        MoveEntity();
    }

    public virtual void MoveEntity() // can be overwritten for things like character controller implementation
    {
        current_velocity = Movement();
        current_velocity += Gravity();

        if (temp_char)
            temp_char.Move(current_velocity * Time.deltaTime);
        else
            this.transform.position += current_velocity * Time.deltaTime;
    }

    public virtual Vector3 Gravity() 
    {
        Vector3 gravity_velocity = Vector3.zero;
        gravity_velocity = gravity_direction * gravity_acceleration;
        return gravity_velocity;
    }

    public virtual Vector3 Movement() 
    {
        return Vector3.zero;
    }
    
    //// NON-WORKING CUSTOM COLLISION FUNCTION
    //RaycastHit hit;
    public LayerMask mask;
    //int n = 5;
    //[SerializeField] float max_angle = 55f;
    //private Vector3 Collision(Vector3 velocity, Vector3 position, bool gravity_pass=false, int depth=0)
    //{
    //    if (depth >= n)
    //    {
    //        Debug.Log("Hit max depth.");
    //        return Vector3.zero;
    //    }
    //    else 
    //    {
    //        float skin_width = 0.15f;
    //        float bound_size = this.GetComponent<Collider>().bounds.extents.x;

    //        if (Physics.SphereCast(position, bound_size, velocity.normalized, out hit, velocity.magnitude + skin_width, mask))
    //        {
    //            Debug.Log("Hit!");
    //            Vector3 closest_point = velocity.normalized * (hit.distance - skin_width);
    //            Vector3 remainder = velocity - closest_point;

    //            if (closest_point.magnitude <= (skin_width))
    //            {
    //                closest_point = Vector3.zero;
    //            }

    //            float current_angle = Vector3.Angle(Vector3.up, hit.normal);

    //            if (current_angle >= max_angle)
    //            {
    //                remainder = ProjectAndScale(remainder, hit.normal);
    //            }
    //            else
    //            {

    //                // hitting wall
    //                float scale = 1 - Vector3.Dot(new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
    //                    -new Vector3(velocity.x, 0, velocity.z).normalized);
    //                /*
    //                if (is_grounded && collide_index == 0)
    //                {
    //                    remainder = ProjectAndScale(new Vector3(hit.normal.x, 0, hit.normal.z),
    //                                    new Vector3(velocity_initial.x, 0, velocity_initial.z)).normalized;
    //                    remainder *= scale;
    //                }
    //                else
    //                {
    //                    remainder = ProjectAndScale(remainder, hit.normal) * scale;
    //                }
    //                */
    //                remainder = ProjectAndScale(remainder, hit.normal) * scale;
    //            }
    //            return closest_point + Collision(remainder, position, gravity_pass, ++depth);
    //        }
    //        Debug.Log("No hit...");
    //        return velocity;
    //    }
    //}

    public Vector3 ProjectAndScale(Vector3 remainder, Vector3 normal)
    {
        Debug.Log("Project and scale.");
        float magnitude = remainder.magnitude * 0.5f;
        remainder = Vector3.ProjectOnPlane(normal, remainder).normalized;
        remainder *= magnitude;
        return remainder;
    }

    Vector3 CapVelocityOnAxes(Vector3 velocity, Vector3 max_velocity, bool is_capped = true)
    {
        if (is_capped)
        {
            float capped_x = Mathf.Clamp(velocity.x, -max_velocity.x, max_velocity.x);
            float capped_y = Mathf.Clamp(velocity.y, -max_velocity.y, max_velocity.y);
            float capped_z = Mathf.Clamp(velocity.z, -max_velocity.z, max_velocity.z);
            velocity = new Vector3(capped_x, capped_y, capped_z);
        }
        return velocity;
    }

    public Vector3 GetVelocity() 
    {
        return current_velocity;
    }

    void DrawSphereCast(Vector3 velocity) 
    {
        float skin_width = 0.15f;
        float bound_size = this.GetComponent<Collider>().bounds.size.x;
        Vector3 current_position = this.transform.position;

        Gizmos.DrawWireSphere(current_position + velocity.normalized * (velocity.magnitude + skin_width), bound_size);
    }
}

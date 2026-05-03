using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float max_health, current_health;

    public void TakeDamage(float dmg)
    {
        Debug.Log("Ouch!");
        current_health -= dmg;
        LimitHealth();
        if (current_health <= 0)
        {
            Die();
        }
    }

    public void SetHealth(float new_health)
    {
        current_health = new_health;
        LimitHealth();
    }

    void LimitHealth()
    {
        if (current_health > max_health)
        {
            current_health = max_health;
        }
        else if (current_health < 0)
        {
            current_health = 0;
        }
    }

    void Die()
    {
        Debug.Log("Blarg! I'm dead!!!");
        // Remove player control,
        // then ragdoll or something?
    }
}

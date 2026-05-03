using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Equipment
{
	[SerializeField] public float fire_rate;
	[SerializeField] public float damage;
	[SerializeField] public float ammo;

	bool can_fire = true;

	// determines what happens when inputs are pressed, things like charging, mode switching, etc. can go here
	override public void Activate(bool inputPress, bool inputHeld) 
	{
		if (inputPress) 
		{
			FireGun();
		}
	}

	// fires the gun, then puts it on an arbitrary cooldown, set it to 0 if the gun has no firerate limit
    public void FireGun()
	{
		// fire that b****, blow their head schmoove off.
		if (can_fire) {
			Debug.Log("Firing...");
			FireBehavior();
			if (fire_rate > 0)
			{
				StartCoroutine(FireCooldown());
			}
		}
	}

	// controls what happens when the gun actually fires
	virtual public void FireBehavior() 
	{
		Debug.Log("Bang!");
	}

	IEnumerator FireCooldown()
	{
		can_fire = false;
		yield return new WaitForSeconds(60f / fire_rate);
		can_fire = true;
	}
}



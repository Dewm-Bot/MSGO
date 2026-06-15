using System.Collections.Generic;
using UnityEngine;

public class ActorCharacter_MobileSuit : ActorCharacter
{
    new protected enum CharacterState
    {
        Idle,
        Walking,
        Firing,
        Boosting,
        BoostingFiring
    }

    [Header("Mobile Suit References")]
    [Tooltip("For script-driven rotation handling.")]
    public Transform modelRoot;         // The "Root" of the model, this should rotate the entire player model
    public Transform legRoot;           // Leg bone, for handling independant movement direction relay.
    public Transform torsoRoot;         // Spine/torso bone, for aiming the upper body
    public Transform headRoot;          // Head bone, for relaying where the player is looking

    public TorsoRotationController_Test torso_script;

    public Transform muzzleTransform;      // Where shots/projectiles originate

    // Components
    public BoostBar boostBarUI;
	public HealthBar healthBarUI;

    [Header("Equipment")]
    [Tooltip("Up to 6 Equipment ScriptableObjects.")]
    public List<Equipment> weapons = new List<Equipment>();
    private int currentWeaponIndex = 0;

    [Header("Boost Settings")]
    public float maxBoostPool = 5f;
    public float forwardBoostBurstDuration = 0.2f;
    public float forwardBoostBurstSpeed = 12f;
    public float forwardBoostSpeed = 6f;
    public float upwardBoostBurstSpeed = 10f;
    public float upwardBoostSpeed = 4f;
    public float boostRechargeDelay = 3f;
    public float boostRechargeRate = 1f;

    private float boostPool;
    private bool isBoostingForward = false;
    private bool isBoostingUp = false;
    private float lastBoostUseTime = 0f;
    private float forwardBoostStartTime;

    [Header("Torso Aiming Settings")]
    [Tooltip("Maximum angle the torso can rotate from the legs' forward direction.")]
    public float maxTorsoAngle = 90f;
    [Tooltip("How quickly the torso rotates to aim.")]
    public float torsoRotationSpeed = 10f;
    [Tooltip("How quickly the legs catch up when torso is at max angle.")]
    public float legsCatchUpSpeed = 5f;
    private Quaternion currentTorsoRotation = Quaternion.identity;    // Current torso rotation in local space

    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    [Tooltip("If true, torso aiming uses the camera aim ray / aim point (recommended). If false, uses camera forward.")]
    public bool aimTorsoAtAimPoint = true;

    private Quaternion initialTorsoLocalRot;

    // Input System
    public bool firePressed = false;
    public bool fireHeld = false;
    public bool nextWeaponPressed = false;
    public bool prevWeaponPressed = false;
    public bool[] selectWeaponPressed = new bool[6];

    // State
    private CharacterState currentState = CharacterState.Walking;
    private float lastFireTime = 0f;
    private float firingStateTimeout = 1.0f; // How long to stay in firing state after last shot

    // Cached values
    public Vector3 lastAimPoint;
    private float lastPressTime = 0;

    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHealth;
        boostPool = maxBoostPool;
        boostBarUI.SetBoost(boostPool, maxBoostPool);
        healthBarUI.SetHealth(currentHealth, maxHealth);
    }

    override public void HandleUpdate()
	{
		HandleMovement();
		HandleBoost();
		HandleAnimation();
        HandleRotation();
        RechargeBoostPool();
    }

	override public void HandleAnimation() 
    {
        if (actorAnim)
        {
            float moveMag = new Vector2(moveInput.x, moveInput.y).magnitude;
            actorAnim.SetFloat("MoveSpeed", moveMag);
            actorAnim.SetFloat("DotForward", Vector3.Dot(desiredMove.normalized, modelRoot.forward));
            actorAnim.SetBool("IsBoosting", isBoostingForward || isBoostingUp);
            actorAnim.SetInteger("PlayerState", (int)currentState);
        }
    }

	void HandleBoost() 
    {
		if (isBoostingForward && boostPool > 0f)
		{
			boostBarUI.SetBoost(boostPool, maxBoostPool);

			float elapsedForward = Time.time - forwardBoostStartTime;
			horizontalVelocity = elapsedForward < forwardBoostBurstDuration ? forwardBoostBurstSpeed : forwardBoostSpeed;

			boostPool -= Time.deltaTime;
			lastBoostUseTime = Time.time;

			if (boostPool <= 0f)
			{
				boostPool = 0f;
				isBoostingForward = false;
			}

			if (!isBoostingUp)
				verticalVelocity = 0;
		}
		else
		{
			isBoostingForward = false;
		}

		if (isBoostingUp && boostPool > 0f)
		{
			boostBarUI.SetBoost(boostPool, maxBoostPool);

			if (verticalVelocity < upwardBoostSpeed)
			{
				verticalVelocity = (verticalVelocity != upwardBoostSpeed) ? upwardBoostBurstSpeed : upwardBoostSpeed;
			}

			if (verticalVelocity > upwardBoostSpeed) // decrease upward boost burst speed until it reaches upward boost speed
			{
				verticalVelocity += gravity * Time.deltaTime;
				verticalVelocity = (verticalVelocity < upwardBoostSpeed) ? upwardBoostSpeed : verticalVelocity;
			}
			else
			{
				verticalVelocity = upwardBoostSpeed;
			}

			boostPool -= Time.deltaTime;
			lastBoostUseTime = Time.time;

			if (boostPool <= 0f)
			{
				boostPool = 0f;
				isBoostingUp = false;
			}
		}
		else
		{
			isBoostingUp = false;
		}

		if (!isBoostingUp && !isBoostingForward) // Gravity only applies when we aren't boosting
		{
			if (!actorChar.isGrounded) { verticalVelocity += gravity * Time.deltaTime; }
			else { verticalVelocity = 0; }
		}

		velocity = desiredMove.normalized * horizontalVelocity + Vector3.up * verticalVelocity; // Build final move vector
	}

    public void OnBoostForwardPressed()
	{
		if (boostPool > 0f && !isBoostingForward)
		{
			isBoostingForward = true;
			forwardBoostStartTime = Time.time;
		}
	}

    public void OnBoostForwardReleased() => isBoostingForward = false;

    public void OnBoostUpPressed()
	{
		if (boostPool > 0f)
			isBoostingUp = true;
	}

    public void OnBoostUpReleased() => isBoostingUp = false;

    public void RechargeBoostPool()
	{
		if (!isBoostingForward && !isBoostingUp && Time.time - lastBoostUseTime > boostRechargeDelay)
		{
			boostPool += Time.deltaTime * boostRechargeRate;
			boostPool = Mathf.Min(boostPool, maxBoostPool);
			boostBarUI.SetBoost(boostPool, maxBoostPool);
		}
	}

    #region Rotation Handling

    private void HandleRotation()
    {
        if (!modelRoot || !torsoRoot)
            return;

        switch (currentState)
        {
            case CharacterState.Walking:
                HandleWalkingRotation();
                break;
            case CharacterState.Firing:
                HandleFiringRotation();
                break;
            case CharacterState.Boosting:
                HandleBoostingRotation();
                break;
            case CharacterState.BoostingFiring:
                HandleBoostingFiringRotation();
                break;
        }

        HandleHeadRotation();
    }

    // Passive state, entire body rotates in movement direction
    private void HandleWalkingRotation()
    {
        torso_script.enabled = false;

        Vector3 moveTarget = new Vector3(velocity.x, 0, velocity.z).normalized;
        moveTarget = new Vector3(moveTarget.x, 0, moveTarget.z) * horizontalVelocity;
        moveTarget = moveTarget.normalized;

        // rotate the model root
        // match the legs to the model root forward direction
        legRoot.localRotation = Quaternion.Lerp(legRoot.localRotation, Quaternion.Euler(0, 0, 0), 10.0f * Time.deltaTime);
        // match the torso to the model root forward direction
        torsoRoot.localRotation = Quaternion.Lerp(torsoRoot.localRotation, Quaternion.Euler(0, 0, 0), 10.0f * Time.deltaTime);

        if (moveTarget.magnitude != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, moveTarget, 1, 1));
            modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);
        }
    }

    // Fire state, torso aims in last fire direction, legs follow movement
    private void HandleFiringRotation()
    {
        torso_script.enabled = true;

        //modelRoot.rotation = Quaternion.Euler(0,rotationRoot.rotation.y,0);
        Vector3 lockForward = rotationRoot.forward;
        lockForward = new Vector3(lockForward.x, 0, lockForward.z);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, lockForward, 1, 1));
        modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);

        int invertDirection = (moveInput.y >= 0.0f) ? 1 : -1;
        float legYaw = invertDirection * (moveInput * horizontalVelocity).normalized.x * 90.0f;
        Quaternion targetLegRotation = Quaternion.Euler(0, legYaw, 0);
        legRoot.localRotation = Quaternion.Lerp(legRoot.localRotation, targetLegRotation, 10.0f * Time.deltaTime);
        // have the yaw of the legs influence the torso to a fractional degree to look more 'natural'
        Quaternion targetTorsoRotation = Quaternion.Euler(0, legYaw * 0.25f, 0); // here we are only getting a quarter of the overall rotation
        torsoRoot.localRotation = Quaternion.Lerp(torsoRoot.localRotation, targetTorsoRotation, 10.0f * Time.deltaTime);
    }

    // Boost movement state, body follows direction of movement at all times
    private void HandleBoostingRotation()
    {
        torso_script.enabled = false;

        Vector3 moveTarget = moveInput.x * rotationRoot.right + moveInput.y * rotationRoot.forward;
        moveTarget = new Vector3(moveTarget.x, 0, moveTarget.z);
        moveTarget = moveTarget.normalized;

        // since we want the legs and torso to match the root, just utilize the local rotation when moving them
        legRoot.localRotation = Quaternion.Euler(0, 0, 0);
        torsoRoot.localRotation = Quaternion.Euler(0, 0, 0);

        // rotate the model global rotation to the relative y rotation movement.
        if (moveTarget.magnitude != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, moveTarget, 1, 1));
            modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);
        }
    }

    // Boosting + Fire state, legs follow movement direction more heavily than usual
    private void HandleBoostingFiringRotation()
    {
        torso_script.enabled = true;

        //modelRoot.rotation = Quaternion.Euler(0,rotationRoot.rotation.y,0);
        Vector3 lockForward = rotationRoot.forward;
        lockForward = new Vector3(lockForward.x, 0, lockForward.z);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, lockForward, 1, 1));
        modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);

        int invertDirection = (moveInput.y >= 0.0f) ? 1 : -1;
        float legYaw = invertDirection * moveInput.x * 90.0f;
        Quaternion targetLegRotation = Quaternion.Euler(0, legYaw, 0);
        legRoot.localRotation = Quaternion.Lerp(legRoot.localRotation, targetLegRotation, 10.0f * Time.deltaTime);
        Quaternion targetTorsoRotation = Quaternion.Euler(0, legYaw * 0.25f, 0);
        torsoRoot.localRotation = Quaternion.Lerp(torsoRoot.localRotation, targetTorsoRotation, 10.0f * Time.deltaTime);
    }

    private void HandleHeadRotation()
    {
        // Smoothly rotate head to targets
        Quaternion targetHeadRotation = modelRoot.rotation;
        float headDot = Vector3.Dot(rotationRoot.forward, modelRoot.forward); // check if the model is facing the same direction as the camera
        bool headTrack = (headDot >= -0.1f) ? true : false; // if the model is not parallel (1.0) or perpendicular (0.0, -0.1 for tolerance issues) with the forward direction, disable headtracking
        if (headTrack)
        {
            Vector3 targetHeadVector = Vector3.RotateTowards(headRoot.forward, rotationRoot.forward, 1, 1);
            targetHeadRotation = Quaternion.LookRotation(targetHeadVector);
        }
        headRoot.rotation = Quaternion.Lerp(headRoot.rotation, targetHeadRotation, 10.0f * Time.deltaTime);
    }

    // Pain and suffering zone
    // Make sure that the torso is aiming where we are when firing
    private void ApplyTorsoAim(Vector3 worldAimDirection)
    {
        // 1. Convert aim direction into local bounding space of the model
        Vector3 localAim = modelRoot.InverseTransformDirection(worldAimDirection);
        if (localAim.sqrMagnitude < 0.0001f) localAim = Vector3.forward;
        localAim.Normalize();

        // 2. Extract yaw relative to the legs
        float currentYaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
        float clampedYaw = Mathf.Clamp(currentYaw, -maxTorsoAngle, maxTorsoAngle);

        // 3. Extract pitch (using Asin natively works for verticality relative to model)
        float pitch = Mathf.Asin(localAim.y) * Mathf.Rad2Deg;

        // 4. Construct a clamped local aim direction
        Vector3 clampedLocalAim = Quaternion.Euler(-pitch, clampedYaw, 0f) * Vector3.forward;

        // 5. Convert clamped aim back to world space
        Vector3 clampedWorldAim = modelRoot.TransformDirection(clampedLocalAim);

        // 6. Compute delta rotation from model's neutral forward to the clamped aim
        Quaternion aimDelta = Quaternion.FromToRotation(modelRoot.forward, clampedWorldAim);

        // 7. Apply this delta directly to the true rest pose of the torso bone in world space
        Quaternion restWorldRot = torsoRoot.parent.rotation * initialTorsoLocalRot;
        Quaternion targetWorldRot = aimDelta * restWorldRot;

        // 8. Convert to pure local rotation and interpolate
        Quaternion targetLocalRot = Quaternion.Inverse(torsoRoot.parent.rotation) * targetWorldRot;

        currentTorsoRotation = Quaternion.Slerp(currentTorsoRotation, targetLocalRot, Time.deltaTime * torsoRotationSpeed);
        torsoRoot.localRotation = currentTorsoRotation;
    }

    #endregion

    #region Weapon Selection & Firing

    private void HandleWeaponSelection()
    {
        if (nextWeaponPressed)
        {
            nextWeaponPressed = false;
            if (weapons.Count > 0)
                SelectWeapon((currentWeaponIndex + 1) % weapons.Count);
        }
        if (prevWeaponPressed)
        {
            prevWeaponPressed = false;
            if (weapons.Count > 0)
                SelectWeapon((currentWeaponIndex - 1 + weapons.Count) % weapons.Count);
        }

        for (int i = 0; i < 6; i++)
        {
            if (selectWeaponPressed[i] && i < weapons.Count)
            {
                SelectWeapon(i);
                selectWeaponPressed[i] = false;
            }
        }
    }

    private void SelectWeapon(int index)
    {
        if (index == currentWeaponIndex || index < 0 || index >= weapons.Count) return;
        actorAnim.SetBool("SwapWeapon", true);
        weapons[currentWeaponIndex]?.OnDeselect();  // deselect previous weapon
        muzzleTransform = null;                     // nullify the muzzle transform to forego torso adjustments
        currentWeaponIndex = index;                 // update index to current desired weapon
        weapons[currentWeaponIndex]?.OnSelect();    // select the current weapon
        actorAnim.SetBool("SwapWeapon", false);
    }

    public void OnFirePressed()
    {
        firePressed = true;
        lastPressTime = Time.time;
    }

    private void CheckFireHeld()
    {
        float timeTillHold = 0.1f;

        if (firePressed && (Time.time - lastPressTime > timeTillHold))
        {
            fireHeld = true;
            firePressed = false;
        }
    }

    public void OnFireReleased()
    {
        fireHeld = false;
        firePressed = false;
    }

    private void HandleFiring()
    {
        if (weapons.Count == 0)
            return;

        var currentWep = weapons[currentWeaponIndex];
        currentWep.Activate(firePressed, fireHeld); // calls the script on the current weapon to actually trigger weapon behavior (firing, spawning drones, whatever)

        // Always calculate aim point when firing or holding fire
        if (firePressed || fireHeld)
        {
            actorAnim.SetBool("IsFiring", true);
            lastAimPoint = aimPoint;
            lastFireTime = Time.time;
        }
        else
        {
            actorAnim.SetBool("IsFiring", false);
        }
    }

    public void SetMuzzle(Transform m)
    {
        muzzleTransform = m;
    }

    #endregion
}

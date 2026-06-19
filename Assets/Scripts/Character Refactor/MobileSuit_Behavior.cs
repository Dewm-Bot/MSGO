using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileSuit_Behavior : MonoBehaviour
{
    public enum CharacterState
    {
        Walking,
        Firing,
        Boosting,
        BoostingFiring
    }

    [Header("References")]
    public Transform rotationRoot;          // Where our camera will be pivoted around
    public Vector3 aimPoint;              // Our primary camera, should be a child of rotationRoot

    public Transform modelRoot;         // The "Root" of the model, this should rotate the entire player model
    public Transform legRoot;           // Leg bone, for handling independant movement direction relay.
    public Transform torsoRoot;         // Spine/torso bone, for aiming the upper body
    public Transform headRoot;          // Head bone, for relaying where the player is looking

    public Transform muzzleTransform;      // Where shots/projectiles originate

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float backwardWalkSpeed = 2f;
    public float turnSmoothTime = 0.1f;    // How quickly legs rotate to desired move dir
    public float firingTurnSmoothTime = 0.2f; // Slower turn when firing
    private float turnSmoothVelocity;

    [Header("Torso Aiming Settings")]
    [Tooltip("Maximum angle the torso can rotate from the legs' forward direction.")]
    public float maxTorsoAngle = 90f;
    [Tooltip("How quickly the torso rotates to aim.")]
    public float torsoRotationSpeed = 10f;
    [Tooltip("How quickly the legs catch up when torso is at max angle.")]
    public float legsCatchUpSpeed = 5f;
    private Quaternion currentTorsoRotation = Quaternion.identity;    // Current torso rotation in local space

    [Tooltip("If true, torso aiming uses the camera aim ray / aim point (recommended). If false, uses camera forward.")]
    public bool aimTorsoAtAimPoint = true;

    private Quaternion initialTorsoLocalRot;

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

    [Header("Gravity")]
    public float gravity = -9.81f;

    [Header("Camera / Look Settings")]
    public float lookSensitivity = 1.2f;
    public float minPitch = -35f;
    public float maxPitch = 60f;
    private float yaw;
    private float pitch;

    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    [Header("Weapons")]
    [Tooltip("Up to 6 Weapon ScriptableObjects.")]
    public List<Equipment> weapons = new List<Equipment>();
    private int currentWeaponIndex = 0;

    // State
    private CharacterState currentState = CharacterState.Walking;
    private float lastFireTime = 0f;
    private float firingStateTimeout = 1.0f; // How long to stay in firing state after last shot

    // Input System
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;
    public bool firePressed = false;
    public bool fireHeld = false;
    public bool nextWeaponPressed = false;
    public bool prevWeaponPressed = false;
    public bool[] selectWeaponPressed = new bool[6];

    // Torso-reset
    public float torsoResetDelay = 2f;
    public float torsoResetSpeed = 5f;

    // Components
    private CharacterController characterController;
    private Animator animator;

    public BoostBar boostBarUI;
    public HealthBar healthBarUI;

    // Cached values
    public Vector3 lastAimPoint;
    public Vector3 velocity;
    private float horizontalVelocity = 0;
    private float verticalVelocity = 0;
    private Vector3 initalCamRootPos;
    private float lastPressTime = 0;
    private bool canFire = true;

    public TorsoRotationController_Test torso_script;

    RaycastHit hit;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = modelRoot ? modelRoot.GetComponent<Animator>() : null;

        currentHealth = maxHealth;
        boostPool = maxBoostPool;
        boostBarUI.SetBoost(boostPool, maxBoostPool);
        healthBarUI.SetHealth(currentHealth, maxHealth);

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw to current camera direction
        yaw = rotationRoot.eulerAngles.y;

        initialTorsoLocalRot = torsoRoot.localRotation;
        currentTorsoRotation = initialTorsoLocalRot;

        initalCamRootPos = rotationRoot.localPosition;

        foreach (var weapon in weapons)
        {
            //Instantiate(weapon);                        // spawn current weapon prefabs
            weapon.parent_transform = this.transform;   // set the reference for it's "owner"
            weapon.OnDeselect();                        // hide the weapon prefab  
        }

        if (weapons.Count > 0)
            weapons[currentWeaponIndex].OnSelect();     // re-activate our starting weapon
    }

    #region State Management

    public void UpdateState()
    {
        bool isBoosting = isBoostingForward || isBoostingUp;
        bool isFiring = firePressed || fireHeld || (Time.time - lastFireTime < firingStateTimeout);

        if (isBoosting && isFiring)
            currentState = CharacterState.BoostingFiring;
        else if (isBoosting)
            currentState = CharacterState.Boosting;
        else if (isFiring)
            currentState = CharacterState.Firing;
        else
            currentState = CharacterState.Walking;
    }

    #endregion

    #region Camera / Look

    public void AddCameraLag()
    {
        rotationRoot.localPosition = Vector3.Lerp(rotationRoot.localPosition, initalCamRootPos - (velocity * 0.1f), 10.0f * Time.deltaTime);
    }

    public void HandleLook()
    {
        yaw += lookInput.x * lookSensitivity;
        pitch -= lookInput.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        rotationRoot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    #endregion

    #region Movement + Unified Boost

    public void HandleMovementAndBoost()
    {
        // Compute camera-relative forward & right (flatten Y)
        Vector3 camForward = rotationRoot.forward;
        Vector3 camRight = rotationRoot.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Determine desired move dir & base speed
        Vector3 desiredMove = camRight * moveInput.x + camForward * moveInput.y;
        horizontalVelocity = walkSpeed;

        // When we walk backwards, make sure we point forward still
        bool walkingBackward = (moveInput.y < -0.1f);
        bool pureBackward = (moveInput.y < 0f && Mathf.Abs(moveInput.x) < 0.1f);

        if ((currentState == CharacterState.Firing || currentState == CharacterState.BoostingFiring))
        {
            if (pureBackward && desiredMove.magnitude > 0.1f)
            {
                // Walk backward toward camera at reduced speed
                desiredMove = -camForward;
                horizontalVelocity = backwardWalkSpeed;
            }
            else if (walkingBackward)
            {
                // Diagonal backward - use slightly reduced speed
                horizontalVelocity = Mathf.Lerp(backwardWalkSpeed, walkSpeed, Mathf.Abs(moveInput.x));
            }
        }

        // Unified Boost Logic
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

            // causes bug where upward boost is applied at the beginning and end but is overwritten in the beginning, so it feels like it's only being applied at the end
            // verticalVelocity = (verticalVelocity < upwardBoostBurstSpeed && isBoostingUp)? upwardBoostBurstSpeed : upwardBoostSpeed;

            // if we are boosting upwards and our vertical velocity is lower than our minimum upward boost speed, we boost burst
            if (verticalVelocity < upwardBoostSpeed)
            {
                verticalVelocity = (verticalVelocity != upwardBoostSpeed) ? upwardBoostBurstSpeed : upwardBoostSpeed;
            }

            // decrease upward boost burst speed until it reaches upward boost speed
            if (verticalVelocity > upwardBoostSpeed)
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

        // Gravity only applies when we aren't boosting
        if (!isBoostingUp && !isBoostingForward)
        {
            if (!characterController.isGrounded) { verticalVelocity += gravity * Time.deltaTime; }
            else { verticalVelocity = 0; }
        }

        // Build final move vector
        velocity = desiredMove.normalized * horizontalVelocity + Vector3.up * verticalVelocity;

        // Move CharacterController
        characterController.Move(velocity * Time.deltaTime);

        // Feed Animator parameters
        if (animator)
        { // NEED TO BE REWORKED IIRC
          // could be replaced with a vector
            float moveMag = new Vector2(moveInput.x, moveInput.y).magnitude;
            animator.SetFloat("MoveSpeed", moveMag);
            animator.SetFloat("DotForward", Vector3.Dot(desiredMove.normalized, modelRoot.forward));
            animator.SetBool("IsBoosting", isBoostingForward || isBoostingUp);
            animator.SetInteger("CharacterState", (int)currentState);
        }
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

    #endregion

    #region Rotation Handling

    public void HandleRotation()
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
    }

    // Passive state, entire body rotates in movement direction
    public void HandleWalkingRotation()
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
    public void HandleFiringRotation()
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
    public void HandleBoostingRotation()
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
    public void HandleBoostingFiringRotation()
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

    public void HandleHeadRotation()
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
    public void ApplyTorsoAim(Vector3 worldAimDirection)
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

    public void HandleWeapon()
    {
        HandleWeaponSelection();
        CheckFireHeld();
        if (canFire)
            HandleFiring();
    }

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

        StartCoroutine(WeapSwapAnimWait());
        weapons[currentWeaponIndex]?.OnDeselect();  // deselect previous weapon
        muzzleTransform = null;                     // nullify the muzzle transform to forego torso adjustments
        currentWeaponIndex = index;                 // update index to current desired weapon
        weapons[currentWeaponIndex]?.OnSelect();    // select the current weapon

    }

    IEnumerator WeapSwapAnimWait()
    {
        animator.SetBool("SwapWeapon", true);
        canFire = false;
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(1).normalizedTime >= 1);
        animator.SetBool("SwapWeapon", false);
        canFire = true;
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
        if (currentWep.GetType().IsSubclassOf(typeof(Gun)))
        {
            Debug.Log("is gun.");
            Gun curr_gun = (Gun)currentWep;
            curr_gun.aimPoint = aimPoint;
        }
        else
        {
            Debug.Log("is not gun.");
        }
        currentWep.Activate(firePressed, fireHeld); // calls the script on the current weapon to actually trigger weapon behavior (firing, spawning drones, whatever)

        // Always calculate aim point when firing or holding fire
        if (firePressed || fireHeld)
        {
            animator.SetBool("IsFiring", true);
            lastAimPoint = aimPoint;
            lastFireTime = Time.time;
        }
        else
        {
            animator.SetBool("IsFiring", false);
        }
    }

    public void SetMuzzle(Transform m)
    {
        muzzleTransform = m;
    }

    #endregion

    #region Health Management

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        healthBarUI.SetHealth(currentHealth, maxHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    public bool IsDead => currentHealth <= 0f;

    #endregion

    #region Debug Gizmos

    public void OnDrawGizmosSelected()
    {
        if (muzzleTransform)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                muzzleTransform.position,
                muzzleTransform.position + muzzleTransform.forward * 2f
            );
        }

        // Draw torso angle limits
        if (modelRoot && torsoRoot)
        {
            Gizmos.color = Color.yellow;
            Vector3 forward = modelRoot.forward;
            Vector3 leftLimit = Quaternion.Euler(0, -maxTorsoAngle, 0) * forward;
            Vector3 rightLimit = Quaternion.Euler(0, maxTorsoAngle, 0) * forward;

            Gizmos.DrawLine(torsoRoot.position, torsoRoot.position + leftLimit * 3f);
            Gizmos.DrawLine(torsoRoot.position, torsoRoot.position + rightLimit * 3f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(torsoRoot.position, torsoRoot.position + torsoRoot.forward * 3f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.transform.position, this.transform.position + moveInput.x * rotationRoot.right + moveInput.y * rotationRoot.forward);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(this.transform.position, this.transform.position + velocity);
        }
    }

    #endregion
    public void SetMoveInput(Vector2 mi)
    {
        moveInput = mi;
    }

    public void SetLookInput(Vector2 li)
    {
        lookInput = li;
    }

    public void SetAimPoint(Vector3 v)
    {
        aimPoint = v;
    }

    public void SetRotationRoot(Transform t)
    {
        rotationRoot = t;
    }
}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    public enum PlayerState
    {
        Walking,
        Firing,
        Boosting,
        BoostingFiring
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraPivot;          // Where our camera will be pivoted around
        public Camera mainCamera;              // Our primary camera, should be a child of CameraPivot
        public Transform modelTransform;       // The "Root" of the model, this should rotate the entire player model
        public Transform upperBodyBone;        // Spine/torso bone for aiming the upper body
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
        private float verticalVelocity = 0f;

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
        private float fireCooldown = 0f;
        [Tooltip("Up to 6 Weapon ScriptableObjects.")]
        public List<WeaponSystem.Weapon> weapons = new List<WeaponSystem.Weapon>();
        private int currentWeaponIndex = 0;

        // State
        private PlayerState currentState = PlayerState.Walking;
        private float lastFireTime = 0f;
        private float firingStateTimeout = 0.5f; // How long to stay in firing state after last shot

        // Input System
        private PlayerControls.PlayerControlsClass controls;
        private Vector2 moveInput = Vector2.zero;
        private Vector2 lookInput = Vector2.zero;
        private bool firePressed = false;
        private bool fireHeld = false;
        private bool nextWeaponPressed = false;
        private bool prevWeaponPressed = false;
        private bool[] selectWeaponPressed = new bool[6];

        // Torso-reset
        public float torsoResetDelay = 2f;
        public float torsoResetSpeed = 5f;

        // Components
        private CharacterController characterController;
        private Animator animator;

        public BoostBar boostBarUI;
        public HealthBar healthBarUI;

        // Cached values
        private Vector3 lastAimPoint;
        private float targetLegsYaw;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = modelTransform ? modelTransform.GetComponent<Animator>() : null;

            currentHealth = maxHealth;
            boostPool = maxBoostPool;
            boostBarUI.SetBoost(boostPool, maxBoostPool);
            healthBarUI.SetHealth(currentHealth, maxHealth);

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Initialize yaw to current camera direction
            yaw = cameraPivot.eulerAngles.y;
            targetLegsYaw = modelTransform.eulerAngles.y;

            initialTorsoLocalRot = upperBodyBone.localRotation;
            currentTorsoRotation = initialTorsoLocalRot;

            controls = new PlayerControls.PlayerControlsClass();

            controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

            controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

            controls.Player.Fire.performed += ctx => OnFirePressed();
            controls.Player.Fire.canceled += ctx => OnFireReleased();

            controls.Player.NextWeapon.performed += ctx => nextWeaponPressed = true;
            controls.Player.PrevWeapon.performed += ctx => prevWeaponPressed = true;

            controls.Player.SelectWeapon1.performed += ctx => selectWeaponPressed[0] = true;
            controls.Player.SelectWeapon1.canceled += ctx => selectWeaponPressed[0] = false;
            controls.Player.SelectWeapon2.performed += ctx => selectWeaponPressed[1] = true;
            controls.Player.SelectWeapon2.canceled += ctx => selectWeaponPressed[1] = false;
            controls.Player.SelectWeapon3.performed += ctx => selectWeaponPressed[2] = true;
            controls.Player.SelectWeapon3.canceled += ctx => selectWeaponPressed[2] = false;
            controls.Player.SelectWeapon4.performed += ctx => selectWeaponPressed[3] = true;
            controls.Player.SelectWeapon4.canceled += ctx => selectWeaponPressed[3] = false;
            controls.Player.SelectWeapon5.performed += ctx => selectWeaponPressed[4] = true;
            controls.Player.SelectWeapon5.canceled += ctx => selectWeaponPressed[4] = false;
            controls.Player.SelectWeapon6.performed += ctx => selectWeaponPressed[5] = true;
            controls.Player.SelectWeapon6.canceled += ctx => selectWeaponPressed[5] = false;

            controls.Player.BoostForward.performed += ctx => OnBoostForwardPressed();
            controls.Player.BoostForward.canceled += ctx => OnBoostForwardReleased();
            controls.Player.BoostUp.performed += ctx => OnBoostUpPressed();
            controls.Player.BoostUp.canceled += ctx => OnBoostUpReleased();
        }

        void OnEnable() => controls.Enable();
        void OnDisable() => controls.Disable();

        void Update()
        {
            // Toggle cursor lock with Escape
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            UpdateState();
            HandleLook();
            HandleMovementAndBoost();
            HandleWeaponSelection();
            HandleFiring();
            HandleRotation();
            RechargeBoostPool();
        }

        #region State Management

        private void UpdateState()
        {
            bool isBoosting = isBoostingForward || isBoostingUp;
            bool isFiring = fireHeld || (Time.time - lastFireTime < firingStateTimeout);

            if (isBoosting && isFiring)
                currentState = PlayerState.BoostingFiring;
            else if (isBoosting)
                currentState = PlayerState.Boosting;
            else if (isFiring)
                currentState = PlayerState.Firing;
            else
                currentState = PlayerState.Walking;
        }

        #endregion

        #region Camera / Look

        private void HandleLook()
        {
            yaw += lookInput.x * lookSensitivity;
            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        #endregion

        #region Movement + Unified Boost

        private void HandleMovementAndBoost()
        {
            // Compute camera-relative forward & right (flatten Y)
            Vector3 camForward = cameraPivot.forward;
            Vector3 camRight = cameraPivot.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Determine desired move dir & base speed
            Vector3 desiredMove = camRight * moveInput.x + camForward * moveInput.y;
            float targetSpeed = walkSpeed;
            
            // When we walk backwards, make sure we point forward still
            bool walkingBackward = (moveInput.y < -0.1f);
            bool pureBackward = (moveInput.y < 0f && Mathf.Abs(moveInput.x) < 0.1f);

            if (pureBackward && desiredMove.magnitude > 0.1f)
            {
                // Walk backward toward camera at reduced speed
                desiredMove = -camForward;
                targetSpeed = backwardWalkSpeed;
            }
            else if (walkingBackward)
            {
                // Diagonal backward - use slightly reduced speed
                targetSpeed = Mathf.Lerp(backwardWalkSpeed, walkSpeed, Mathf.Abs(moveInput.x));
            }

            // Unified Boost Logic
            if (isBoostingForward && boostPool > 0f)
            {
                boostBarUI.SetBoost(boostPool, maxBoostPool);

                float elapsedForward = Time.time - forwardBoostStartTime;
                targetSpeed = elapsedForward < forwardBoostBurstDuration ? forwardBoostBurstSpeed : forwardBoostSpeed;

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

                verticalVelocity = verticalVelocity < upwardBoostBurstSpeed ? upwardBoostBurstSpeed : upwardBoostSpeed;

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
                verticalVelocity += gravity * Time.deltaTime;

            // Build final move vector
            Vector3 moveVector = desiredMove.normalized * targetSpeed + Vector3.up * verticalVelocity;

            // Move CharacterController
            characterController.Move(moveVector * Time.deltaTime);

            // Calculate target legs yaw based on movement and state
            if (desiredMove.magnitude > 0.1f)
            {
                if (!pureBackward)
                {
                    // Legs face movement direction
                    targetLegsYaw = Mathf.Atan2(desiredMove.x, desiredMove.z) * Mathf.Rad2Deg;
                }
                else
                {
                    // Walking backward: legs face camera direction
                    targetLegsYaw = cameraPivot.eulerAngles.y;
                }
            }

            // Feed Animator parameters
            if (animator)
            { // NEED TO BE REWORKED IIRC
                float moveMag = new Vector2(moveInput.x, moveInput.y).magnitude;
                animator.SetFloat("MoveSpeed", moveMag);
                animator.SetBool("IsMovingBackward", pureBackward);
                animator.SetBool("IsBoostingForward", isBoostingForward);
                animator.SetBool("IsBoostingUp", isBoostingUp);
                animator.SetInteger("PlayerState", (int)currentState);
            }
        }

        private void OnBoostForwardPressed()
        {
            if (boostPool > 0f && !isBoostingForward)
            {
                isBoostingForward = true;
                forwardBoostStartTime = Time.time;
            }
        }

        private void OnBoostForwardReleased() => isBoostingForward = false;

        private void OnBoostUpPressed()
        {
            if (boostPool > 0f)
                isBoostingUp = true;
        }

        private void OnBoostUpReleased() => isBoostingUp = false;

        private void RechargeBoostPool()
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

        private void HandleRotation()
        {
            if (!modelTransform || !upperBodyBone)
                return;

            switch (currentState)
            {
                case PlayerState.Walking:
                    HandleWalkingRotation();
                    break;
                case PlayerState.Firing:
                    HandleFiringRotation();
                    break;
                case PlayerState.Boosting:
                    HandleBoostingRotation();
                    break;
                case PlayerState.BoostingFiring:
                    HandleBoostingFiringRotation();
                    break;
            }
        }


        // Passive state, entire body rotates in movement direction
        private void HandleWalkingRotation()
        {
            // Smoothly rotate legs to target
            float currentLegsYaw = modelTransform.eulerAngles.y;
            float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, targetLegsYaw, ref turnSmoothVelocity, turnSmoothTime);
            modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);
            
            // Gradually reset torso to its initial rest pose When walking, either keep aiming with camera, or gradually reset torso.
            if (Time.time - lastFireTime > torsoResetDelay)
            {
                currentTorsoRotation = Quaternion.Slerp(currentTorsoRotation, initialTorsoLocalRot, Time.deltaTime * torsoResetSpeed);
                upperBodyBone.localRotation = currentTorsoRotation;
            }
        }

        // Fire state, torso aims in last fire direction, legs follow movement
        private void HandleFiringRotation()
        {
            // Prefer aiming torso at the *same target* the weapon uses
            // If we're not currently firing, fall back to camera forward (passive state)
            Vector3 aimDirection;
            if (aimTorsoAtAimPoint)
            {
                Vector3 target = (lastAimPoint == Vector3.zero) ? (cameraPivot.position + cameraPivot.forward * 1000f) : lastAimPoint;
                aimDirection = (target - upperBodyBone.position);
            }
            else
            {
                aimDirection = cameraPivot.forward;
            }

            // Clamp values
            if (aimDirection.sqrMagnitude < 0.0001f)
                aimDirection = cameraPivot.forward;
            aimDirection.Normalize();

            // Get current legs forward direction
            float currentLegsYaw = modelTransform.eulerAngles.y;
            bool movingBackward = moveInput.y < -0.1f;

            // Determine torso yaw relative to legs
            Vector3 localAimForYaw = modelTransform.InverseTransformDirection(aimDirection);
            float torsoYaw = Mathf.Atan2(localAimForYaw.x, localAimForYaw.z) * Mathf.Rad2Deg;

            if (movingBackward)
            {
                // When moving backward and firing, align legs with camera direction
                if (Mathf.Abs(torsoYaw) > maxTorsoAngle * 0.5f)
                {
                    // Blend legs toward aim direction
                    float blendedLegsTarget = Mathf.LerpAngle(targetLegsYaw, cameraPivot.eulerAngles.y, 0.7f);
                    float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, blendedLegsTarget, ref turnSmoothVelocity, firingTurnSmoothTime);
                    modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);
                }
                else
                {
                    // Small angle difference, let legs follow movement slowly
                    float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, targetLegsYaw, ref turnSmoothVelocity, firingTurnSmoothTime * 1.5f);
                    modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);
                }
            }
            else
            {
                // Normal movement while firing
                if (Mathf.Abs(torsoYaw) > maxTorsoAngle)
                {
                    // Legs need to catch up to keep torso within limits
                    float overflow = Mathf.Abs(torsoYaw) - maxTorsoAngle;
                    float legsCatchUpYaw = currentLegsYaw + Mathf.Sign(torsoYaw) * overflow;

                    float smoothLegsYaw = Mathf.MoveTowardsAngle(currentLegsYaw, legsCatchUpYaw, legsCatchUpSpeed * Time.deltaTime * 60f);
                    modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);
                }
                else
                {
                    // Torso can reach target without moving legs
                    if (moveInput.magnitude > 0.1f)
                    {
                        float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, targetLegsYaw, ref turnSmoothVelocity, firingTurnSmoothTime);
                        modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);
                    }
                    else
                    {
                        // When not moving, legs support the aiming direction
                        float aimYaw = Mathf.Atan2(aimDirection.x, aimDirection.z) * Mathf.Rad2Deg;
                        float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, aimYaw, ref turnSmoothVelocity, firingTurnSmoothTime);
                        modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);
                    }
                }
            }

            ApplyTorsoAim(aimDirection);
        }
        
        // Boost movement state, body follows direction of movement at all times
        private void HandleBoostingRotation()
        {
            // When boosting, legs should face the direction of movement
            float currentLegsYaw = modelTransform.eulerAngles.y;
            float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, targetLegsYaw, ref turnSmoothVelocity, turnSmoothTime);
            modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);

            if (Time.time - lastFireTime > torsoResetDelay * 0.5f)
            {
                currentTorsoRotation = Quaternion.Slerp(currentTorsoRotation, initialTorsoLocalRot, Time.deltaTime * torsoResetSpeed * 2f);
                upperBodyBone.localRotation = currentTorsoRotation;
            }
        }

        // Boosting + Fire state, legs follow movement direction more heavily than usual
        private void HandleBoostingFiringRotation()
        {
            Vector3 aimDirection;
            if (aimTorsoAtAimPoint)
            {
                Vector3 target = (lastAimPoint == Vector3.zero) ? (cameraPivot.position + cameraPivot.forward * 1000f) : lastAimPoint;
                aimDirection = (target - upperBodyBone.position);
            }
            else
            {
                aimDirection = cameraPivot.forward;
            }

            if (aimDirection.sqrMagnitude < 0.0001f)
                aimDirection = cameraPivot.forward;
            aimDirection.Normalize();

            float currentLegsYaw = modelTransform.eulerAngles.y;

            // While boosting, legs strongly want to face movement direction
            float smoothLegsYaw = Mathf.SmoothDampAngle(currentLegsYaw, targetLegsYaw, ref turnSmoothVelocity, turnSmoothTime);
            modelTransform.rotation = Quaternion.Euler(0f, smoothLegsYaw, 0f);

            ApplyTorsoAim(aimDirection);
        }

        // Pain and suffering zone
        // Make sure that the torso is aiming where we are when firing
        private void ApplyTorsoAim(Vector3 worldAimDirection)
        {
            // 1. Convert aim direction into local bounding space of the model
            Vector3 localAim = modelTransform.InverseTransformDirection(worldAimDirection);
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
            Vector3 clampedWorldAim = modelTransform.TransformDirection(clampedLocalAim);

            // 6. Compute delta rotation from model's neutral forward to the clamped aim
            Quaternion aimDelta = Quaternion.FromToRotation(modelTransform.forward, clampedWorldAim);

            // 7. Apply this delta directly to the true rest pose of the torso bone in world space
            Quaternion restWorldRot = upperBodyBone.parent.rotation * initialTorsoLocalRot;
            Quaternion targetWorldRot = aimDelta * restWorldRot;

            // 8. Convert to pure local rotation and interpolate
            Quaternion targetLocalRot = Quaternion.Inverse(upperBodyBone.parent.rotation) * targetWorldRot;

            currentTorsoRotation = Quaternion.Slerp(currentTorsoRotation, targetLocalRot, Time.deltaTime * torsoRotationSpeed);
            upperBodyBone.localRotation = currentTorsoRotation;
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

            weapons[currentWeaponIndex]?.OnDeselect();
            currentWeaponIndex = index;
        }

        private void OnFirePressed()
        {
            firePressed = true;
            fireHeld = true;
            lastFireTime = Time.time;

            if (weapons.Count > 0 && weapons[currentWeaponIndex] is WeaponSystem.ChargeBasedWeapon cb)
            {
                cb.StartCharging();
            }
        }

        private void OnFireReleased()
        {
            fireHeld = false;
        }

        private void HandleFiring()
        {
            if (fireCooldown > 0f)
                fireCooldown -= Time.deltaTime;

            if (weapons.Count == 0)
                return;

            var currentWep = weapons[currentWeaponIndex];

            // Always calculate aim point when firing or holding fire
            if (firePressed || fireHeld)
            {
                lastAimPoint = CalculateAimPoint();
            }

            if (currentWep is WeaponSystem.SemiAutoWeapon semi)
            {
                if (firePressed && fireCooldown <= 0f)
                {
                    semi.FireShot(muzzleTransform, lastAimPoint);
                    fireCooldown = 1f / semi.fireRate;
                    firePressed = false;
                    lastFireTime = Time.time;
                }
            }
            else if (currentWep is WeaponSystem.FullAutoWeapon full)
            {
                if (fireHeld && fireCooldown <= 0f)
                {
                    full.FireShot(muzzleTransform, lastAimPoint);
                    fireCooldown = 1f / full.fireRate;
                    lastFireTime = Time.time;
                }
            }
            else if (currentWep is WeaponSystem.ChargeBasedWeapon cb)
            {
                if (firePressed && fireCooldown <= 0f)
                {
                    cb.StartCharging();
                    lastFireTime = Time.time;
                    firePressed = false;
                }

                if (!fireHeld && cb.IsCharging())
                {
                    cb.ReleaseCharge(muzzleTransform, lastAimPoint);
                    fireCooldown = 1f / cb.fireRate;
                    lastFireTime = Time.time;

                    if (cb.burst)
                    {
                        StartCoroutine(BurstFire(cb, lastAimPoint));
                    }
                }
            }
        }

        private System.Collections.IEnumerator BurstFire(WeaponSystem.ChargeBasedWeapon weapon, Vector3 aimPoint)
        {
            float burstEndTime = Time.time + weapon.burstTime;
            while (Time.time < burstEndTime)
            {
                weapon.FireSingleShot(muzzleTransform, aimPoint);
                yield return new WaitForSeconds(1f / weapon.fireRate);
            }
        }
        
        // Raycast from camera center to find aimpoint, handled separately from torso
        private Vector3 CalculateAimPoint()
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hitCollider = hits[i].collider;
                    if (hitCollider != null && !hitCollider.transform.IsChildOf(transform))
                    {
                        return hits[i].point;
                    }
                }
            }

            return ray.GetPoint(1000f);
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

        private void Die()
        {
            Destroy(gameObject);
        }

        public bool IsDead => currentHealth <= 0f;

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
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
            if (modelTransform && upperBodyBone)
            {
                Gizmos.color = Color.yellow;
                Vector3 forward = modelTransform.forward;
                Vector3 leftLimit = Quaternion.Euler(0, -maxTorsoAngle, 0) * forward;
                Vector3 rightLimit = Quaternion.Euler(0, maxTorsoAngle, 0) * forward;

                Gizmos.DrawLine(upperBodyBone.position, upperBodyBone.position + leftLimit * 3f);
                Gizmos.DrawLine(upperBodyBone.position, upperBodyBone.position + rightLimit * 3f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(upperBodyBone.position, upperBodyBone.position + upperBodyBone.forward * 3f);
            }
        }

        #endregion
    }
}

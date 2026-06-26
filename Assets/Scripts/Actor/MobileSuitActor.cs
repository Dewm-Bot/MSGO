using UnityEngine;
using System.Collections.Generic;

public class MobileSuitActor : MonoBehaviour
{
    [Header("Configuration")]
    public MobileSuitActorStats stats;

    [Header("References")]
    public Transform rotationRoot;          // Where our camera will be pivoted around
    public Transform modelRoot;         // The "Root" of the model
    public Transform legRoot;           // Leg bone
    public Transform torsoRoot;         // Spine/torso bone
    public Transform headRoot;          // Head bone
    public Transform muzzleTransform;      // Where shots originate

    [Header("Components")]
    public CharacterController characterController;
    public Animator animator;

    [Header("UI References")]
    public BoostBar boostBarUI;
    public HealthBar healthBarUI;

    // Internal State
    private float currentHealth;
    private float boostPool;
    private float lastBoostUseTime = 0f;
    private float forwardBoostStartTime;
    private float turnSmoothVelocity;
    private float yaw;
    private float pitch;
    private Quaternion initialTorsoLocalRot;
    private Quaternion currentTorsoRotation = Quaternion.identity;
    private Vector3 velocity;
    private float horizontalVelocity = 0;
    private float verticalVelocity = 0;
    private Vector3 initialCamRootPos;

    // Intents (Driven by an external Driver script)
    public Vector2 MoveIntent = Vector2.zero;
    public Vector2 LookIntent = Vector2.zero;
    public bool FirePressed = false;
    public bool FireHeld = false;
    public bool BoostForwardPressed = false;
    public bool BoostForwardReleased = false;
    public bool BoostUpPressed = false;
    public bool BoostUpReleased = false;
    public bool nextWeaponPressed = false;
    public bool prevWeaponPressed = false;
    public bool[] selectWeaponPressed = new bool[6];

    public enum ActorState { Walking, Firing, Boosting, BoostingFiring }
    public ActorState CurrentState = ActorState.Walking;
    private float lastFireTime = 0f;
    private float firingStateTimeout = 1.0f;

    private bool isBoostingForward = false;
    private bool isBoostingUp = false;

    void Awake()
    {
        if (stats == null)
        {
            Debug.LogError("MobileSuitActor: Stats ScriptableObject is missing!", this);
            return;
        }

        characterController = GetComponent<CharacterController>();
        animator = modelRoot ? modelRoot.GetComponent<Animator>() : null;

        currentHealth = stats.maxHealth;
        boostPool = stats.maxBoostPool;

        boostBarUI.SetBoost(boostPool, stats.maxBoostPool);
        healthBarUI.SetHealth(currentHealth, stats.maxHealth);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = rotationRoot.eulerAngles.y;
        initialTorsoLocalRot = torsoRoot.localRotation;
        currentTorsoRotation = initialTorsoLocalRot;
        initialCamRootPos = rotationRoot.localPosition;
    }

    void Update()
    {
        UpdateState();
        HandleLook();
        HandleMovementAndBoost();
        RechargeBoostPool();
        AddCameraLag();
        HandleRotation();
        HandleWeapon();
    }

    private void UpdateState()
    {
        bool isBoosting = isBoostingForward || isBoostingUp;
        bool isFiring = FirePressed || FireHeld || (Time.time - lastFireTime < firingStateTimeout);

        if (isBoosting && isFiring) CurrentState = ActorState.BoostingFiring;
        else if (isBoosting) CurrentState = ActorState.Boosting;
        else if (isFiring) CurrentState = ActorState.Firing;
        else CurrentState = ActorState.Walking;
    }

    private void HandleLook()
    {
        yaw += LookIntent.x * stats.lookSensitivity;
        pitch -= LookIntent.y * stats.lookSensitivity;
        pitch = Mathf.Clamp(pitch, stats.minPitch, stats.maxPitch);
        rotationRoot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovementAndBoost()
    {
        Vector3 camForward = rotationRoot.forward;
        Vector3 camRight = rotationRoot.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredMove = camRight * MoveIntent.x + camForward * MoveIntent.y;
        horizontalVelocity = stats.walkSpeed;

        bool walkingBackward = (MoveIntent.y < -0.1f);
        bool pureBackward = (MoveIntent.y < 0f && Mathf.Abs(MoveIntent.x) < 0.1f);

        if ((CurrentState == ActorState.Firing || CurrentState == ActorState.BoostingFiring))
        {
            if (pureBackward && desiredMove.magnitude > 0.1f)
            {
                desiredMove = -camForward;
                horizontalVelocity = stats.backwardWalkSpeed;
            }
            else if (walkingBackward)
            {
                horizontalVelocity = Mathf.Lerp(stats.backwardWalkSpeed, stats.walkSpeed, Mathf.Abs(MoveIntent.x));
            }
        }

        // Boost Logic
        if (isBoostingForward && boostPool > 0f)
        {
            boostBarUI.SetBoost(boostPool, stats.maxBoostPool);
            float elapsedForward = Time.time - forwardBoostStartTime;
            horizontalVelocity = elapsedForward < stats.forwardBoostBurstDuration ? stats.forwardBoostBurstSpeed : stats.forwardBoostSpeed;
            boostPool -= Time.deltaTime;
            lastBoostUseTime = Time.time;
            if (boostPool <= 0f) { boostPool = 0f; isBoostingForward = false; }
            if (!isBoostingUp) verticalVelocity = 0;
        }
        else
        {
            isBoostingForward = false;
        }

        if (isBoostingUp && boostPool > 0f)
        {
            boostBarUI.SetBoost(boostPool, stats.maxBoostPool);
            if (verticalVelocity < stats.upwardBoostSpeed)
            {
                verticalVelocity = (verticalVelocity != stats.upwardBoostSpeed) ? stats.upwardBoostBurstSpeed : stats.upwardBoostSpeed;
            }
            if (verticalVelocity > stats.upwardBoostSpeed)
            {
                verticalVelocity += stats.gravity * Time.deltaTime;
                verticalVelocity = (verticalVelocity < stats.upwardBoostSpeed) ? stats.upwardBoostSpeed : verticalVelocity;
            }
            else
            {
                verticalVelocity = stats.upwardBoostSpeed;
            }
            boostPool -= Time.deltaTime;
            lastBoostUseTime = Time.time;
            if (boostPool <= 0f) { boostPool = 0f; isBoostingUp = false; }
        }
        else
        {
            isBoostingUp = false;
        }

        if (!isBoostingUp && !isBoostingForward)
        {
            if (!characterController.isGrounded) { verticalVelocity += stats.gravity * Time.deltaTime; }
            else { verticalVelocity = 0; }
        }

        velocity = desiredMove.normalized * horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        if (animator)
        {
            float moveMag = new Vector2(MoveIntent.x, MoveIntent.y).magnitude;
            animator.SetFloat("MoveSpeed", moveMag);
            animator.SetFloat("DotForward", Vector3.Dot(desiredMove.normalized, modelRoot.forward));
            animator.SetBool("IsBoosting", isBoostingForward || isBoostingUp);
            animator.SetInteger("CharacterState", (int)CurrentState);
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
    public void OnBoostUpPressed() { if (boostPool > 0f) isBoostingUp = true; }
    public void OnBoostUpReleased() => isBoostingUp = false;

    private void RechargeBoostPool()
    {
        if (!isBoostingForward && !isBoostingUp && Time.time - lastBoostUseTime > stats.boostRechargeDelay)
        {
            boostPool += Time.deltaTime * stats.boostRechargeRate;
            boostPool = Mathf.Min(boostPool, stats.maxBoostPool);
            boostBarUI.SetBoost(boostPool, stats.maxBoostPool);
        }
    }

    private void AddCameraLag()
    {
        rotationRoot.localPosition = Vector3.Lerp(rotationRoot.localPosition, initialCamRootPos - (velocity * 0.1f), 10.0f * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (!modelRoot || !torsoRoot) return;
        switch (CurrentState)
        {
            case ActorState.Walking: HandleWalkingRotation(); break;
            case ActorState.Firing: HandleFiringRotation(); break;
            case ActorState.Boosting: HandleBoostingRotation(); break;
            case ActorState.BoostingFiring: HandleBoostingFiringRotation(); break;
        }
    }

    private void HandleWalkingRotation()
    {
        torsoRoot.parent.GetComponent<TorsoRotationController_Test>().enabled = false;
        Vector3 moveTarget = new Vector3(velocity.x, 0, velocity.z).normalized;
        moveTarget = new Vector3(moveTarget.x, 0, moveTarget.z) * horizontalVelocity;
        moveTarget = moveTarget.normalized;

        legRoot.localRotation = Quaternion.Lerp(legRoot.localRotation, Quaternion.Euler(0, 0, 0), 10.0f * Time.deltaTime);
        torsoRoot.localRotation = Quaternion.Lerp(torsoRoot.localRotation, Quaternion.Euler(0, 0, 0), 10.0f * Time.deltaTime);

        if (moveTarget.magnitude != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, moveTarget, 1, 1));
            modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);
        }
    }

    private void HandleFiringRotation()
    {
        torsoRoot.parent.GetComponent<TorsoRotationController_Test>().enabled = true;
        Vector3 lockForward = rotationRoot.forward;
        lockForward = new Vector3(lockForward.x, 0, lockForward.z);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, lockForward, 1, 1));
        modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);

        int invertDirection = (MoveIntent.y >= 0.0f) ? 1 : -1;
        float legYaw = invertDirection * (MoveIntent * horizontalVelocity).normalized.x * 90.0f;
        legRoot.localRotation = Quaternion.Lerp(legRoot.localRotation, Quaternion.Euler(0, legYaw, 0), 10.0f * Time.deltaTime);
        torsoRoot.localRotation = Quaternion.Lerp(torsoRoot.localRotation, Quaternion.Euler(0, legYaw * 0.25f, 0), 10.0f * Time.deltaTime);
    }

    private void HandleBoostingRotation()
    {
        torsoRoot.parent.GetComponent<TorsoRotationController_Test>().enabled = false;
        Vector3 moveTarget = MoveIntent.x * rotationRoot.right + MoveIntent.y * rotationRoot.forward;
        moveTarget = new Vector3(moveTarget.x, 0, moveTarget.z);
        moveTarget = moveTarget.normalized;
        legRoot.localRotation = Quaternion.Euler(0, 0, 0);
        torsoRoot.localRotation = Quaternion.Euler(0, 0, 0);
        if (moveTarget.magnitude != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, moveTarget, 1, 1));
            modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);
        }
    }

    private void HandleBoostingFiringRotation()
    {
        torsoRoot.parent.GetComponent<TorsoRotationController_Test>().enabled = true;
        Vector3 lockForward = rotationRoot.forward;
        lockForward = new Vector3(lockForward.x, 0, lockForward.z);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.RotateTowards(modelRoot.forward, lockForward, 1, 1));
        modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRotation, 10.0f * Time.deltaTime);

        int invertDirection = (MoveIntent.y >= 0.0f) ? 1 : -1;
        float legYaw = invertDirection * MoveIntent.x * 90.0f;
        legRoot.localRotation = Quaternion.Lerp(legRoot.localRotation, Quaternion.Euler(0, legYaw, 0), 10.0f * Time.deltaTime);
        torsoRoot.localRotation = Quaternion.Lerp(torsoRoot.localRotation, Quaternion.Euler(0, legYaw * 0.25f, 0), 10.0f * Time.deltaTime);
    }

    public void HandleHeadRotation()
    {
        Quaternion targetHeadRotation = modelRoot.rotation;
        float headDot = Vector3.Dot(rotationRoot.forward, modelRoot.forward);
        bool headTrack = (headDot >= -0.1f);
        if (headTrack)
        {
            Vector3 targetHeadVector = Vector3.RotateTowards(headRoot.forward, rotationRoot.forward, 1, 1);
            targetHeadRotation = Quaternion.LookRotation(targetHeadVector);
        }
        headRoot.rotation = Quaternion.Lerp(headRoot.rotation, targetHeadRotation, 10.0f * Time.deltaTime);
    }

    private void HandleWeapon()
    {
        if (nextWeaponPressed || prevWeaponPressed)
        {
            // Logic for switching weapons will go here
            nextWeaponPressed = false;
            prevWeaponPressed = false;
        }

        for (int i = 0; i < 6; i++)
        {
            if (selectWeaponPressed[i])
            {
                // Logic for specific weapon selection
                selectWeaponPressed[i] = false;
            }
        }
    }
}

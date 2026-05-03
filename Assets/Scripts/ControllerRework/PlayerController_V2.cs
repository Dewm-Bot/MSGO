using UnityEngine;

public class PlayerController_V2 : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerControls.PlayerControlsClass controls;

    public GameObject legRoot;
    public GameObject torsoRoot;
    public GameObject headRoot;

    public GameObject cameraRoot;

    private Vector2 moveInput;
    private Vector2 lookInput;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        controls = new PlayerControls.PlayerControlsClass();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        /*
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
        */
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    private void Update()
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

        // Build final move vector
        //Vector3 moveVector = desiredMove.normalized * targetSpeed + Vector3.up * verticalVelocity;

        // Move CharacterController
        //characterController.Move(moveVector * Time.deltaTime);
    }
}

using UnityEngine;

//Driver script for local player
//Changes raw input to intents for the MobileSuitActor
public class HumanMobileSuitDriver : MonoBehaviour
{
    [Header("Actor Reference")]
    public MobileSuitActor actor;

    private PlayerControls.PlayerControlsClass controls;

    void Awake()
    {
        if (actor == null)
        {
            Debug.LogError("HumanMobileSuitDriver: No MobileSuitActor assigned!", this);
            return;
        }

        controls = new PlayerControls.PlayerControlsClass();

        //Map inputs to the Actor's Intents
        controls.Player.Move.performed += ctx => actor.MoveIntent = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => actor.MoveIntent = Vector2.zero;

        controls.Player.Look.performed += ctx => actor.LookIntent = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => actor.LookIntent = Vector2.zero;

        controls.Player.Fire.performed += ctx => actor.FirePressed = true;
        controls.Player.Fire.canceled += ctx => actor.FirePressed = false;

        // We probably need to track 'held' states for weapons here.

        controls.Player.NextWeapon.performed += ctx => actor.nextWeaponPressed = true;
        controls.Player.PrevWeapon.performed += ctx => actor.prevWeaponPressed = true;

        controls.Player.SelectWeapon1.performed += ctx => actor.selectWeaponPressed[0] = true;
        controls.Player.SelectWeapon1.canceled += ctx => actor.selectWeaponPressed[0] = false;
        controls.Player.SelectWeapon2.performed += ctx => actor.selectWeaponPressed[1] = true;
        controls.Player.SelectWeapon2.canceled += ctx => actor.selectWeaponPressed[1] = false;
        controls.Player.SelectWeapon3.performed += ctx => actor.selectWeaponPressed[2] = true;
        controls.Player.SelectWeapon3.canceled += ctx => actor.selectWeaponPressed[2] = false;
        controls.Player.SelectWeapon4.performed += ctx => actor.selectWeaponPressed[3] = true;
        controls.Player.SelectWeapon4.canceled += ctx => actor.selectWeaponPressed[3] = false;
        controls.Player.SelectWeapon5.performed += ctx => actor.selectWeaponPressed[4] = true;
        controls.Player.SelectWeapon5.canceled += ctx => actor.selectWeaponPressed[4] = false;
        controls.Player.SelectWeapon6.performed += ctx => actor.selectWeaponPressed[5] = true;
        controls.Player.SelectWeapon6.canceled += ctx => actor.selectWeaponPressed[5] = false;

        controls.Player.BoostForward.performed += ctx => actor.OnBoostForwardPressed();
        controls.Player.BoostForward.canceled += ctx => actor.OnBoostForwardReleased();

        controls.Player.BoostUp.performed += ctx => actor.OnBoostUpPressed();
        controls.Player.BoostUp.canceled += ctx => actor.OnBoostUpReleased();
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
    }
}

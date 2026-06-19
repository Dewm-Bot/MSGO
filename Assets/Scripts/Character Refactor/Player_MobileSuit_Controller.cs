using UnityEngine;

public class Player_MobileSuit_Controller : MonoBehaviour
{
    MobileSuit_Behavior ms;

    public Transform cameraPivot;
    public Camera mainCamera;

    private PlayerControls.PlayerControlsClass controls;

    void Awake()
    {
        controls = new PlayerControls.PlayerControlsClass();

        controls.Player.Move.performed += ctx => ms.SetMoveInput(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled += ctx => ms.SetMoveInput(Vector2.zero);

        controls.Player.Look.performed += ctx => ms.SetLookInput(ctx.ReadValue<Vector2>());
        controls.Player.Look.canceled += ctx => ms.SetLookInput(Vector2.zero);

        controls.Player.Fire.performed += ctx => ms.OnFirePressed();
        controls.Player.Fire.canceled += ctx => ms.OnFireReleased();

        controls.Player.NextWeapon.performed += ctx => ms.nextWeaponPressed = true;
        controls.Player.PrevWeapon.performed += ctx => ms.prevWeaponPressed = true;

        controls.Player.SelectWeapon1.performed += ctx => ms.selectWeaponPressed[0] = true;
        controls.Player.SelectWeapon1.canceled += ctx => ms.selectWeaponPressed[0] = false;
        controls.Player.SelectWeapon2.performed += ctx => ms.selectWeaponPressed[1] = true;
        controls.Player.SelectWeapon2.canceled += ctx => ms.selectWeaponPressed[1] = false;
        controls.Player.SelectWeapon3.performed += ctx => ms.selectWeaponPressed[2] = true;
        controls.Player.SelectWeapon3.canceled += ctx => ms.selectWeaponPressed[2] = false;
        controls.Player.SelectWeapon4.performed += ctx => ms.selectWeaponPressed[3] = true;
        controls.Player.SelectWeapon4.canceled += ctx => ms.selectWeaponPressed[3] = false;
        controls.Player.SelectWeapon5.performed += ctx => ms.selectWeaponPressed[4] = true;
        controls.Player.SelectWeapon5.canceled += ctx => ms.selectWeaponPressed[4] = false;
        controls.Player.SelectWeapon6.performed += ctx => ms.selectWeaponPressed[5] = true;
        controls.Player.SelectWeapon6.canceled += ctx => ms.selectWeaponPressed[5] = false;

        controls.Player.BoostForward.performed += ctx => ms.OnBoostForwardPressed();
        controls.Player.BoostForward.canceled += ctx => ms.OnBoostForwardReleased();
        controls.Player.BoostUp.performed += ctx => ms.OnBoostUpPressed();
        controls.Player.BoostUp.canceled += ctx => ms.OnBoostUpReleased();

        ms.SetRotationRoot(cameraPivot);
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

        ms.UpdateState();
        ms.HandleLook();
        ms.HandleMovementAndBoost();
        ms.RechargeBoostPool();
        ms.AddCameraLag();
        ms.HandleWeapon();
    }

    private void LateUpdate()
    {
        ms.HandleRotation();
        ms.HandleHeadRotation();
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
}

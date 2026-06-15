using UnityEngine;

public class Player_MobileSuit : MonoBehaviour
{
    // Divergent from diagram out of necessity, I don't know what I was thinking with the node-based system
    [Header("References")]
    public ActorCharacter_MobileSuit ActorMS;
    public Transform cameraPivot;          // Where our camera will be pivoted around
    public Camera mainCamera;              // Our primary camera, should be a child of CameraPivot

    private PlayerControls.PlayerControlsClass controls;

    void Awake()
    {
        controls = new PlayerControls.PlayerControlsClass();

        controls.Player.Move.performed += ctx => ActorMS.SetMoveInput(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled += ctx => ActorMS.SetMoveInput(Vector2.zero);

        controls.Player.Look.performed += ctx => ActorMS.SetLookInput(ctx.ReadValue<Vector2>());
        controls.Player.Look.canceled += ctx => ActorMS.SetLookInput(Vector2.zero);

        controls.Player.Fire.performed += ctx => ActorMS.OnFirePressed();
        controls.Player.Fire.canceled += ctx => ActorMS.OnFireReleased();

        controls.Player.NextWeapon.performed += ctx => ActorMS.nextWeaponPressed = true;
        controls.Player.PrevWeapon.performed += ctx => ActorMS.prevWeaponPressed = true;

        controls.Player.SelectWeapon1.performed += ctx => ActorMS.selectWeaponPressed[0] = true;
        controls.Player.SelectWeapon1.canceled += ctx => ActorMS.selectWeaponPressed[0] = false;
        controls.Player.SelectWeapon2.performed += ctx => ActorMS.selectWeaponPressed[1] = true;
        controls.Player.SelectWeapon2.canceled += ctx => ActorMS.selectWeaponPressed[1] = false;
        controls.Player.SelectWeapon3.performed += ctx => ActorMS.selectWeaponPressed[2] = true;
        controls.Player.SelectWeapon3.canceled += ctx => ActorMS.selectWeaponPressed[2] = false;
        controls.Player.SelectWeapon4.performed += ctx => ActorMS.selectWeaponPressed[3] = true;
        controls.Player.SelectWeapon4.canceled += ctx => ActorMS.selectWeaponPressed[3] = false;
        controls.Player.SelectWeapon5.performed += ctx => ActorMS.selectWeaponPressed[4] = true;
        controls.Player.SelectWeapon5.canceled += ctx => ActorMS.selectWeaponPressed[4] = false;
        controls.Player.SelectWeapon6.performed += ctx => ActorMS.selectWeaponPressed[5] = true;
        controls.Player.SelectWeapon6.canceled += ctx => ActorMS.selectWeaponPressed[5] = false;

        controls.Player.BoostForward.performed += ctx => ActorMS.OnBoostForwardPressed();
        controls.Player.BoostForward.canceled += ctx => ActorMS.OnBoostForwardReleased();
        controls.Player.BoostUp.performed += ctx => ActorMS.OnBoostUpPressed();
        controls.Player.BoostUp.canceled += ctx => ActorMS.OnBoostUpReleased();
        ActorMS.SetRotationRoot(cameraPivot);
    }

    void Update()
    {
        ActorMS.SetAimPoint(CalculateAimPoint());
    }

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

using UnityEngine;

public class InputController : MonoBehaviour
{
    private PlayerControls.PlayerControlsClass controls;

    public Vector2 moveInput = Vector2.zero;
    public Vector2 lookInput = Vector2.zero;

    public Transform root;
    public Transform relative_direction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controls = new PlayerControls.PlayerControlsClass();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(this.transform.position, this.transform.position);
    }
}

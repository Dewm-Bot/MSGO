using UnityEngine;

public class ActorCharacter : MonoBehaviour
{
	protected enum CharacterState
	{
		// to be filled by inhereted classes
	}

	[Header("References")]
	protected Transform rotationRoot;          // Where our camera will be pivoted around
	protected Vector3 aimPoint;              // Our primary camera, should be a child of rotationRoot

	// Components
	public CharacterController actorChar;  // actual character controller for moving characters
	public Animator actorAnim;             // animation handler

	public Vector2 moveInput = Vector2.zero;
	public Vector2 lookInput = Vector2.zero;
	public Vector3 desiredMove = Vector3.zero;
	public Vector3 velocity;

	[Header("Movement Settings")]
	public float walkSpeed = 4f;
	public float backwardWalkSpeed = 2f;
	public float turnSmoothTime = 0.1f;    // How quickly legs rotate to desired move dir
	public float firingTurnSmoothTime = 0.2f; // Slower turn when firing

	[Header("Gravity")]
	public float gravity = -9.81f;

	// Cached values
	public float horizontalVelocity = 0;
	public float verticalVelocity = 0;

	// Input System
	protected PlayerControls.PlayerControlsClass controls;

	virtual public void AssignControls() 
	{
		controls = new PlayerControls.PlayerControlsClass();

		controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
		controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

		controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
		controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
	}

	virtual protected void Awake()
	{
		actorChar = (actorChar == null) ? GetComponent<CharacterController>() : actorChar;
		actorAnim = GetComponent<Animator>();

		//AssignControls();
	}

	virtual protected void Update()
	{
		HandleUpdate();

		// Move CharacterController
		velocity = desiredMove.normalized * horizontalVelocity + Vector3.up * verticalVelocity;
		actorChar.Move(velocity * Time.deltaTime);
	}

	virtual protected void LateUpdate() 
	{
	
	}

	public void HandleMovement()
	{
		// Compute camera-relative forward & right (flatten Y)
		Vector3 lookForward = rotationRoot.forward;
		Vector3 lookRight = rotationRoot.right;
		lookForward.y = 0f;
		lookRight.y = 0f;
		lookForward.Normalize();
		lookRight.Normalize();

		// Determine desired move dir & base speed
		desiredMove = lookRight * moveInput.x + lookForward * moveInput.y;
		horizontalVelocity = walkSpeed;

		bool walkingBackward = (moveInput.y < -0.1f);
		bool pureBackward = (moveInput.y < 0f && Mathf.Abs(moveInput.x) < 0.1f);
	}

	virtual public void HandleAnimation(){

	}

	virtual public void HandleUpdate() 
	{
		HandleMovement();
		HandleAnimation();
	}

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

using UnityEngine;

public class ActorCharacter : MonoBehaviour
{
	protected enum CharacterState
	{
		// to be filled by inhereted classes
	}

	// script for taking generalized inputs and translating it to proper articulation
	[Header("Actor References")]
	public ActorInput actorInputs;  // universal input acceptance for bot or player
	public Transform look_pos;

	// Components
	public CharacterController actorChar;  // actual character controller for moving characters
	public Animator actorAnim;             // animation handler

	protected Vector2 moveInput = Vector2.zero;
	protected Vector2 lookInput = Vector2.zero;
	protected Vector3 desiredMove = Vector3.zero;
	protected Vector3 velocity;

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

	virtual protected void AssignControls() 
	{
		controls = new PlayerControls.PlayerControlsClass();

		controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
		controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

		controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
		controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
	}

	virtual protected void Awake()
	{
		actorChar = GetComponent<CharacterController>();
		actorAnim = GetComponent<Animator>();

		AssignControls();
	}

	void Update()
	{
		look_pos = actorInputs.look_pos;

		HandleUpdate();

		// Move CharacterController
		velocity = desiredMove.normalized * horizontalVelocity + Vector3.up * verticalVelocity;
		actorChar.Move(velocity * Time.deltaTime);
	}

	protected void HandleMovement()
	{
		// Compute camera-relative forward & right (flatten Y)
		Vector3 lookForward = look_pos.forward;
		Vector3 lookRight = look_pos.right;
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

	virtual protected void HandleAnimation(){

	}

	virtual protected void HandleUpdate() 
	{
		HandleMovement();
		HandleAnimation();
	}
}

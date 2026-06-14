ActorInput.cs

public ActorInput : MonoBehavior
{
	public Transform aim_pos;
	public Transform move_pos;
	public Transform look_pos;

	public bool isFire;
	public bool isJump;
	public bool isCrouch;
}

Player_ActorInput.cs

public Player_ActorInput : ActorInput
{
	void Update()
	{

	}
}

Bot_ActorInput.cs

public Bot_ActorInput : ActorInput
{
	void Update()
	{

	}
}

ActorController.cs

public class ActorController : MonoBehavior
{
	// script for taking generalized inputs and translating it to proper articulation

	public ActorInput actorInputs;  // universal input acceptance for bot or player

	public Transform look_pos;

	CharacterController actorChar;  // actual character controller for moving characters
	Animator actorAnim;             // animation handler


	float move_speed;
	float gravity;

	Vector3 velocity;

	void Awake()
	{
		actorChar = GetComponent<CharacterController>();
		actorAnim = GetComponent<Animator>();
	}

	void Update()
	{
		look_pos = actorInputs.look_pos;

		HandleMovement();

		// Feed Animator parameters
		if (animator)
		{
			float moveMag = new Vector2(moveInput.x, moveInput.y).magnitude;
			animator.SetFloat("MoveSpeed", moveMag);
			animator.SetFloat("DotForward", Vector3.Dot(desiredMove.normalized, modelRoot.forward));
			animator.SetBool("IsBoosting", isBoostingForward || isBoostingUp);
			animator.SetInteger("PlayerState", (int)currentState);
		}

		// Move CharacterController
		actorChar.Move(velocity * Time.deltaTime);
	}

	private void HandleMovement()
	{
		// Compute camera-relative forward & right (flatten Y)
		Vector3 lookForward = look_pos.forward;
		Vector3 lookRight = look_pos.right;
		lookForward.y = 0f;
		lookRight.y = 0f;
		lookForward.Normalize();
		lookRight.Normalize();

		// Determine desired move dir & base speed
		Vector3 desiredMove = lookRight * moveInput.x + lookForward * moveInput.y;
		horizontalVelocity = walkSpeed;

		bool walkingBackward = (moveInput.y < -0.1f);
		bool pureBackward = (moveInput.y < 0f && Mathf.Abs(moveInput.x) < 0.1f);
	}
}

MobileSuit_Actor.cs

class MobileSuit_Actor : ActorController
{
	// specific to mobilesuits, controller for the mobilesuit character
	// intermediary between inputs and the equipment/weapon controller

	float boost_speed;
	float lift_speed;

	void HandleBoost()
	{
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
}

ActorEquipmentController.cs

// just a tweaked version of the weapon controller
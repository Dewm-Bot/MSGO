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
    horizontalVelocity = walkSpeed;

    // When we walk backwards, make sure we point forward still
    bool walkingBackward = (moveInput.y < -0.1f);
    bool pureBackward = (moveInput.y < 0f && Mathf.Abs(moveInput.x) < 0.1f);

    if ((currentState == PlayerState.Firing || currentState == PlayerState.BoostingFiring))
    {
        if (pureBackward && desiredMove.magnitude > 0.1f)
        {
            // Walk backward toward camera at reduced speed
            desiredMove = -camForward;
            horizontalVelocity = backwardWalkSpeed;
        }
        else if (walkingBackward)
        {
            // Diagonal backward - use slightly reduced speed
            horizontalVelocity = Mathf.Lerp(backwardWalkSpeed, walkSpeed, Mathf.Abs(moveInput.x));
        }
    }

    // Unified Boost Logic


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
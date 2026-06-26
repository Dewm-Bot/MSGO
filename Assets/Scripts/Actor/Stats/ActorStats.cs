using UnityEngine;

[CreateAssetMenu(fileName = "NewMobileSuitStats", menuName = "MSGO/Actor/MobileSuit Stats")]
public class MobileSuitActorStats : ScriptableObject
{
    //Allows us to store stats for a mobile suit actor.

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float backwardWalkSpeed = 2f;
    public float turnSmoothTime = 0.1f;
    public float firingTurnSmoothTime = 0.2f;
    public float torsoRotationSpeed = 10f;
    public float legsCatchUpSpeed = 5f;
    public float maxTorsoAngle = 90f;

    [Header("Boost")]
    public float maxBoostPool = 5f;
    public float forwardBoostBurstDuration = 0.2f;
    public float forwardBoostBurstSpeed = 12f;
    public float forwardBoostSpeed = 6f;
    public float upwardBoostBurstSpeed = 10f;
    public float upwardBoostSpeed = 4f;
    public float boostRechargeDelay = 3f;
    public float boostRechargeRate = 1f;

    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float lookSensitivity = 1.2f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("Health")]
    public float maxHealth = 100f;
}

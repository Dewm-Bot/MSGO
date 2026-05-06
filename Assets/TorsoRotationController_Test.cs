using UnityEngine;

public class TorsoRotationController_Test : MonoBehaviour
{
    public Transform modelRoot;
    public Transform torsoRoot;
    public Transform camera;
    public Transform muzzle;

    public Vector3 distance;
    Vector3 muzzleAim;
    Vector3 cameraAim;
    Quaternion targetRotation;

    float maxTorsoAngle = 90.0f;
    Quaternion initialTorsoLocalRot;
    Quaternion currentTorsoRotation;

    private void Awake()
    {
        initialTorsoLocalRot = torsoRoot.localRotation;
        currentTorsoRotation = torsoRoot.localRotation;
    }

    private void Update()
    {
        RaycastHit muzzle_hit;
        Physics.Raycast(muzzle.position, muzzle.forward * 1000f, out muzzle_hit);
        muzzleAim = muzzle_hit.point;
        if (muzzle_hit.transform == null) { muzzleAim = muzzle.position + muzzle.forward * 1000f; }

        RaycastHit camera_hit;
        Physics.Raycast(camera.position, camera.forward * 1000f, out camera_hit);
        cameraAim = camera_hit.point;
        if (camera_hit.transform == null) { cameraAim = camera.position + camera.forward * 1000f; }
    }

    private void LateUpdate()
    {
        TorsoDirectAim();
    }

    private void ApplyTorsoAim(Vector3 worldAimDirection)
    {
        // 1. Convert aim direction into local bounding space of the model
        Vector3 localAim = modelRoot.InverseTransformDirection(worldAimDirection);
        if (localAim.sqrMagnitude < 0.0001f) localAim = Vector3.forward;
        localAim.Normalize();

        // 2. Extract yaw relative to the legs
        float currentYaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
        float clampedYaw = Mathf.Clamp(currentYaw, -maxTorsoAngle, maxTorsoAngle);

        // 3. Extract pitch (using Asin natively works for verticality relative to model)
        float pitch = Mathf.Asin(localAim.y) * Mathf.Rad2Deg;

        // 4. Construct a clamped local aim direction
        Vector3 clampedLocalAim = Quaternion.Euler(-pitch, clampedYaw, 0f) * Vector3.forward;

        // 5. Convert clamped aim back to world space
        Vector3 clampedWorldAim = modelRoot.TransformDirection(clampedLocalAim);

        // 6. Compute delta rotation from model's neutral forward to the clamped aim
        Quaternion aimDelta = Quaternion.FromToRotation(modelRoot.forward, clampedWorldAim);

        // 7. Apply this delta directly to the true rest pose of the torso bone in world space
        Quaternion restWorldRot = torsoRoot.parent.rotation * initialTorsoLocalRot;
        Quaternion targetWorldRot = aimDelta * restWorldRot;

        // 8. Convert to pure local rotation and interpolate
        Quaternion targetLocalRot = Quaternion.Inverse(torsoRoot.parent.rotation) * targetWorldRot;

        currentTorsoRotation = Quaternion.Slerp(currentTorsoRotation, targetLocalRot, Time.deltaTime * 10.0f);
        torsoRoot.localRotation = currentTorsoRotation;
    }

    private void TorsoDirectAim() 
    {
        //problem with this solution: Assumes muzzle is parallel to the torso
        distance = (cameraAim - muzzle.position).normalized;
        Vector3 offset = (torsoRoot.position - muzzle.position).normalized;
        //distance -= offset;

        targetRotation = Quaternion.LookRotation(distance);
        Quaternion inheretRotation = Quaternion.FromToRotation(torsoRoot.forward, muzzle.forward);
        torsoRoot.rotation = Quaternion.Lerp(torsoRoot.rotation, targetRotation, 10.0f * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawLine(torsoRoot.position, torsoRoot.position + torsoRoot.forward * 10.0f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * 10.0f);

    }

}

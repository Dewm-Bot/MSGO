using UnityEngine;

public class TorsoRotationAim_CameraPivot : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform torsoRoot;
    public Transform muzzle;

    Vector3 distance;

    // differs from other torso rotation controller by purely matching the muzzle forward to the camera pivot forward by rotating the torso;
    private void Update()
    {
        Vector3 cam_aim = cameraPivot.position + cameraPivot.forward * 1000.0f;
        distance = (cam_aim - muzzle.position).normalized;
        Vector3 offset = (torsoRoot.position - muzzle.position).normalized;
        //distance -= offset;

        Quaternion targetRotation = Quaternion.LookRotation(distance);
        torsoRoot.rotation = Quaternion.Lerp(torsoRoot.rotation, targetRotation, 100.0f * Time.deltaTime);
    }
}

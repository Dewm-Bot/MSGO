using UnityEngine;

public class TorsoRotationController_Test : MonoBehaviour
{
    public Transform torsoRoot;
    public Transform camera;
    public Transform muzzle;

    public Vector3 distance;
    Vector3 muzzleAim;
    Vector3 cameraAim; 

    private void Update()
    {
        RaycastHit muzzle_hit;
        Physics.Raycast(muzzle.position, muzzle.forward * 1000f, out muzzle_hit);
        muzzleAim = muzzle_hit.point;
        if (muzzle_hit.transform == null) { muzzleAim = muzzle.position + muzzle.forward * 1000f;  }

        RaycastHit camera_hit;
        Physics.Raycast(camera.position, camera.forward * 1000f, out camera_hit);
        cameraAim = camera_hit.point;
        if (camera_hit.transform == null) { cameraAim = camera.position + camera.forward * 1000f; }

        distance = (cameraAim - muzzle.position).normalized;
        Vector3 offset = (torsoRoot.position - muzzle.position).normalized;
        //distance -= offset;

        Quaternion targetRotation = Quaternion.LookRotation(distance);
        torsoRoot.rotation = Quaternion.Lerp(torsoRoot.rotation, targetRotation, 100.0f * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawLine(torsoRoot.position, torsoRoot.position + torsoRoot.forward * 10.0f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * 10.0f);

    }

}

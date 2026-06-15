using UnityEngine;

public class TorsoRotationController_Test : MonoBehaviour
{
    public Transform modelRoot;
    public Transform torsoRoot;
    public Transform currentCamera;
    public Transform muzzle;

    public Vector3 distToTurn;
    Vector3 muzzleAim;
    Vector3 cameraAim;
    Quaternion targetRotation;

    public Vector3 rotationOffset;

    Quaternion inheretRotation;

    private void Awake()
    {
        inheretRotation = Quaternion.Inverse(Quaternion.FromToRotation(torsoRoot.forward, muzzle.forward));
    }

    private void Update()
    {
        RaycastHit muzzle_hit;
        Physics.Raycast(muzzle.position, muzzle.forward * 1000f, out muzzle_hit);
        muzzleAim = muzzle_hit.point;
        if (muzzle_hit.transform == null) { muzzleAim = muzzle.position + muzzle.forward * 1000f; }

        RaycastHit camera_hit;
        Physics.Raycast(currentCamera.position, currentCamera.forward * 1000f, out camera_hit);
        cameraAim = camera_hit.point;
        if (camera_hit.transform == null) { cameraAim = currentCamera.position + currentCamera.forward * 1000f; }
    }

    private void LateUpdate()
    {
        Vector3 distance = (cameraAim - muzzle.position);
        if (distance.magnitude > 0.1f)
        {
            TorsoDirectAim();
        } else {
            
        }
    }

    private void TorsoDirectAim() 
    {
        //problem with this solution: Assumes muzzle is parallel to the torso
        distToTurn = (cameraAim - torsoRoot.position).normalized;
        Vector3 offset = (torsoRoot.position - muzzle.position).normalized;
        //distance -= offset;

        targetRotation = Quaternion.LookRotation(distToTurn);
        //torsoRoot.rotation = Quaternion.Lerp(torsoRoot.rotation, targetRotation, 10.0f * Time.deltaTime);
        //this.transform.rotation = Quaternion.Lerp(torsoRoot.rotation, targetRotation * inheretRotation * Quaternion.Euler(rotationOffset), 100.0f * Time.deltaTime);
        this.transform.rotation = targetRotation;
    }

    private void OnDrawGizmos()
    {

        float distToCam = (cameraAim - torsoRoot.position).magnitude;

        Gizmos.color = Color.green;

        Gizmos.DrawLine(currentCamera.position, cameraAim);
        Gizmos.DrawSphere(cameraAim, 2.0f);

        Gizmos.color = Color.blue;

        Gizmos.DrawLine(torsoRoot.position, torsoRoot.position + torsoRoot.forward * distToCam);

        Gizmos.color = Color.orange;
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * distToCam);

        Gizmos.color = Color.red;

        Gizmos.DrawLine(muzzle.position + muzzle.forward * distToCam, cameraAim);

    }

}

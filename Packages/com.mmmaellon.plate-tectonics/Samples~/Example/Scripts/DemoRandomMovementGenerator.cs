
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoRandomMovementGenerator : UdonSharpBehaviour
{
    void Start()
    {
        enabled = Networking.LocalPlayer.isMaster;//have the instance owner sync to everyone else. No one else runs this script
        SetNewTarget();
        SpinToFaceTarget();
        firstFrameAfterMove = true;
    }
    public SmartObjectSync sync;
    Vector3 targetPos = Vector3.zero;
    float lastDistance = -1001f;
    float currDistance = -1001f;
    float lastAngle = -1001f;
    float currAngle = -1001f;
    public float randomRange = 20f;

    bool firstFrameAfterMove = true;
    bool turning = false;
    public void Update()
    {
        currDistance = Vector3.Distance(transform.parent.TransformPoint(targetPos), sync.rigid.position);
        currAngle = Vector3.SignedAngle(Vector3.forward, sync.transform.InverseTransformPoint(transform.parent.TransformPoint(targetPos)), Vector3.up);
        if (firstFrameAfterMove)
        {
            firstFrameAfterMove = false;
        }
        else if (turning)
        {
            if (Mathf.Abs(currAngle) > Mathf.Abs(lastAngle))
            {
                MoveTowardsTarget();
                firstFrameAfterMove = true;
            }
        }
        else
        {
            if (currDistance > lastDistance)
            {
                SetNewTarget();
                SpinToFaceTarget();
                firstFrameAfterMove = true;
            }
        }
        lastDistance = currDistance;
        lastAngle = currAngle;
    }

    Vector3 idealVelocity;
    public void SetNewTarget()
    {
        targetPos = Random.onUnitSphere * randomRange;
        targetPos.y = sync.transform.localPosition.y;
    }
    public void SpinToFaceTarget()
    {
        currAngle = Vector3.SignedAngle(Vector3.forward, sync.transform.InverseTransformPoint(transform.parent.TransformPoint(targetPos)), Vector3.up);
        sync.rigid.angularVelocity = currAngle > 0 ? Vector3.up * 0.5f : Vector3.up * -0.5f;
        sync.rigid.velocity = Vector3.zero;
        sync.RequestSerialization();
        turning = true;
    }

    public void MoveTowardsTarget()
    {
        idealVelocity = transform.parent.TransformPoint(targetPos) - sync.rigid.position;
        currDistance = Vector3.Distance(transform.parent.TransformPoint(targetPos), sync.rigid.position);
        sync.rigid.angularVelocity = Vector3.zero;
        sync.rigid.velocity = Vector3.Normalize(idealVelocity) * 3;
        sync.RequestSerialization();
        turning = false;
    }
}

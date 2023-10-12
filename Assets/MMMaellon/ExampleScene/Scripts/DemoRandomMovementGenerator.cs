
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoRandomMovementGenerator : UdonSharpBehaviour
{
    void Start()
    {
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
        if (Networking.LocalPlayer.IsOwner(gameObject))
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
        else
        {
            sync.rigid.velocity = transform.parent.rotation * sync.vel;
            sync.rigid.angularVelocity = transform.parent.rotation * sync.spin;
        }
    }

    Vector3 idealVelocity;
    public void SetNewTarget()
    {
        targetPos = Random.onUnitSphere * randomRange;
        targetPos.y = sync.transform.localPosition.y;
    }
    public void SpinToFaceTarget()
    {
        currAngle = Vector3.SignedAngle(Vector3.forward, sync.transform.InverseTransformPoint(transform.parent.TransformPoint(targetPos)), transform.parent.rotation * Vector3.up);
        sync.rigid.angularVelocity = currAngle > 0 ? transform.parent.rotation * Vector3.up * 0.5f : transform.parent.rotation * Vector3.up * -0.5f;
        sync.rigid.velocity = Vector3.zero;
        sync.state = SmartObjectSync.STATE_INTERPOLATING;
        sync.RequestSerialization();
        turning = true;
    }

    public void MoveTowardsTarget()
    {
        idealVelocity = transform.parent.TransformPoint(targetPos) - sync.rigid.position;
        currDistance = idealVelocity.magnitude;
        sync.rigid.angularVelocity = Vector3.zero;
        sync.rigid.velocity = Vector3.Normalize(idealVelocity) * 3;
        sync.state = SmartObjectSync.STATE_INTERPOLATING;
        sync.RequestSerialization();
        turning = false;
    }
}

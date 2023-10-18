
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DemoRandomMovementGenerator : UdonSharpBehaviour
{
    void Start()
    {
        SetNewTarget();
    }
    public Transform debugObj;
    public Rigidbody rigid;
    Vector3 posControl1;
    Vector3 posControl2;
    float lagTime = 0.5f;
    public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel, float interpolation)
    {//Shout out to Kit Kat for suggesting the improved hermite interpolation
        posControl1 = startPos + startVel * lagTime * interpolation / 3f;
        posControl2 = endPos - endVel * lagTime * (1.0f - interpolation) / 3f;
        return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
    }
    public Quaternion HermiteInterpolateRotation(Quaternion startRot, Vector3 startSpin, Quaternion endRot, Vector3 endSpin, float interpolation)
    {
        // rotControl1 = startRot * Quaternion.Euler(startSpin * lagTime * interpolation / 3f);
        // rotControl2 = endRot * Quaternion.Euler(-1.0f * endSpin * lagTime * (1.0f - interpolation) / 3f);
        // return Quaternion.Slerp(rotControl1, rotControl2, interpolation);


        //we aren't actually doing hermite. It turns out higher order stuff isn't necessary just do a slerp
        return Quaternion.Slerp(startRot, endRot, interpolation);
    }
    [UdonSynced, FieldChangeCallback(nameof(targetPos))]
    Vector3 _targetPos = Vector3.zero;
    [UdonSynced, FieldChangeCallback(nameof(startPos))]
    Vector3 _startPos = Vector3.zero;
    public Vector3 startPos
    {
        get => _startPos;
        set
        {
            _startPos = value;
            if(!Networking.LocalPlayer.IsOwner(gameObject)){
                transform.position = value;
            }
        }
    }
    [UdonSynced, FieldChangeCallback(nameof(startRot))]
    Quaternion _startRot;
    public Quaternion startRot
    {
        get => _startRot;
        set
        {
            _startRot = value;
            if(!Networking.LocalPlayer.IsOwner(gameObject)){
                transform.rotation = value;
            }
        }
    }
    public Vector3 targetPos
    {
        get => _targetPos;
        set
        {
            _targetPos = value;
            lastDistance = -1001f;
            lastAngle = -1001f;
            if (Utilities.IsValid(debugObj))
            {
                debugObj.transform.position = value;
            }
        }
    }
    float lastDistance = -1001f;
    float currDistance = -1001f;
    float lastAngle = -1001f;
    float currAngle = -1001f;
    public float randomRange = 20f;

    bool firstFrameAfterMove = true;
    bool turning = false;
    public void Update()
    {
        if (!Utilities.IsValid(Networking.LocalPlayer))
        {
            return;
        }
        currDistance = Vector3.Distance(transform.parent.TransformPoint(targetPos), transform.position);
        currAngle = Vector3.SignedAngle(Vector3.forward, transform.InverseTransformPoint(transform.parent.TransformPoint(targetPos)), Vector3.up);

        rigid.velocity += Vector3.Lerp(Vector3.zero, transform.rotation * Vector3.forward * 3 * Mathf.Clamp(currDistance / 5f, 1f, 5), (15 - currAngle) / 15) * Time.deltaTime;
        rigid.angularVelocity += (Vector3.up * Mathf.Lerp(-1f, 1f, Mathf.Pow(currAngle / 180f * 8, 3) + 0.5f) - rigid.angularVelocity * Mathf.Clamp(currAngle / 15, 1, 3)) * Time.deltaTime;

        if (Networking.LocalPlayer.IsOwner(gameObject) && lastDistance > 0 && currDistance < 5f)
        {
            SetNewTarget();
        }
        lastDistance = currDistance;
        lastAngle = currAngle;
        startPos = transform.position;
        startRot = transform.rotation;
    }
    public void SetNewTarget()
    {
        targetPos = new Vector3(Random.Range(-randomRange, randomRange), transform.position.y, Random.Range(-randomRange, randomRange));
        RequestSerialization();
    }
}

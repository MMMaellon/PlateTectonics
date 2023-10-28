
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
    bool startPosSet = false;
    public Vector3 startPos
    {
        get => _startPos;
        set
        {
            _startPos = value;
            if(!Networking.LocalPlayer.IsOwner(gameObject) && !startPosSet)
            {
                startPosSet = true;
                transform.position = value;
            }
        }
    }
    [UdonSynced, FieldChangeCallback(nameof(startRot))]
    Quaternion _startRot;
    bool startRotSet = false;
    public Quaternion startRot
    {
        get => _startRot;
        set
        {
            _startRot = value;
            if(!Networking.LocalPlayer.IsOwner(gameObject) && !startRotSet)
            {
                startRotSet = true;
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
            lastTargetSwitch = Time.timeSinceLevelLoad;
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
    Vector3 acceleration;
    Vector3 angularAcceleration;
    public void Update()
    {
        if (!Utilities.IsValid(Networking.LocalPlayer))
        {
            return;
        }
        currDistance = Vector3.Distance(transform.parent.TransformPoint(targetPos), transform.position);
        currAngle = Vector3.SignedAngle(Vector3.forward, transform.InverseTransformPoint(transform.parent.TransformPoint(targetPos)), Vector3.up);
        if (lastTargetSwitch + 2f > Time.timeSinceLevelLoad)
        {
            acceleration = Vector3.zero;
            angularAcceleration = Vector3.zero;
        }
        else
        {
            acceleration = Mathf.Clamp01(Time.timeSinceLevelLoad - (lastTargetSwitch + 2f)) * (transform.rotation * Vector3.forward * Mathf.Pow((45f - Mathf.Clamp(Mathf.Abs(currAngle), 0, 45)) / 45f, 2) * Mathf.Clamp(currDistance / 3, 0, 15) * 5 - rigid.velocity * 2) * Time.deltaTime;
            angularAcceleration = Mathf.Clamp01(Time.timeSinceLevelLoad - (lastTargetSwitch + 2f)) * (Vector3.up * Mathf.Lerp(-1f, 1f, Mathf.Pow(currAngle / 180f * 8, 3) + 0.5f) * 2 - rigid.angularVelocity * 5) * Time.deltaTime;
        }
        rigid.velocity += acceleration;
        rigid.angularVelocity += angularAcceleration;

        if (Networking.LocalPlayer.IsOwner(gameObject) && lastDistance > 0 && currDistance < 5f)
        {
            SetNewTarget();
        }
        lastDistance = currDistance;
        lastAngle = currAngle;
        startPos = transform.position;
        startRot = transform.rotation;
    }
    float lastTargetSwitch;
    public void SetNewTarget()
    {
        targetPos = new Vector3(Random.Range(-randomRange, randomRange), transform.position.y, Random.Range(-randomRange, randomRange));
        RequestSerialization();
    }
}

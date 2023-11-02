
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
        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            SetNewTarget();
        }
        else
        {
            // transform.position = new Vector3(Random.Range(-randomRange * 10f, randomRange * 10f), transform.position.y, Random.Range(-randomRange * 10f, randomRange * 10f));
        }
    }
    public Transform debugObj;
    public Rigidbody rigid;
    [UdonSynced, FieldChangeCallback(nameof(targetPos))]
    Vector3 _targetPos = Vector3.zero;
    [UdonSynced, FieldChangeCallback(nameof(startPos))]
    Vector3 _startPos = Vector3.zero;
    Vector3 startPosCorrection = Vector3.zero;
    bool startSetPos;
    bool startSetRot;
    public Vector3 startPos
    {
        get => _startPos;
        set
        {
            _startPos = value;
            if(!Networking.LocalPlayer.IsOwner(gameObject))
            {
                if (!startSetPos)
                {
                    transform.position = value;
                    startSetPos = true;
                }
                startPosCorrection = value - transform.position;
            }
        }
    }
    [UdonSynced, FieldChangeCallback(nameof(startRot))]
    Quaternion _startRot;
    Quaternion startRotCorrection = Quaternion.identity;
    public Quaternion startRot
    {
        get => _startRot;
        set
        {
            _startRot = value;
            if(!Networking.LocalPlayer.IsOwner(gameObject))
            {
                if (!startSetRot)
                {
                    transform.rotation = value;
                    startSetRot = true;
                }
                startRotCorrection = value * Quaternion.Inverse(transform.rotation);
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
    Vector3 axis;
    float angle;
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



        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            if (lastDistance > 0 && currDistance < 5f)
            {
                SetNewTarget();
            }
        }
        else
        {
            //corrections
            if (startPosCorrection.magnitude > 0)
            {
                acceleration += startPosCorrection * Time.deltaTime * 0.5f;
                startPosCorrection = 0.5f * startPosCorrection;
            }
            startRotCorrection.ToAngleAxis(out angle, out axis);
            if (angle > 0)
            {
                angularAcceleration += Mathf.Clamp(angle, -90f, 90f) * axis * Time.deltaTime * 0.5f;
                startRotCorrection = Quaternion.Slerp(Quaternion.identity, startRotCorrection, 0.5f);
            }
        }



        rigid.velocity += acceleration;
        rigid.angularVelocity += angularAcceleration;



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

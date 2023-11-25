
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoSpin : UdonSharpBehaviour
{
    public Rigidbody rigid;
    [UdonSynced, FieldChangeCallback(nameof(startRot))]
    Quaternion _startRot;
    Quaternion startRotCorrection = Quaternion.identity;
    bool startSet;
    public Quaternion startRot
    {
        get => _startRot;
        set
        {
            _startRot = value;
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                if (!startSet)
                {
                    transform.rotation = value;
                    startSet = true;
                }
                startRotCorrection = value * Quaternion.Inverse(transform.rotation);
            }
        }
    }
    Vector3 axis;
    float angle;
    Vector3 angularAcceleration;
    public void Update()
    {
        angularAcceleration = Vector3.forward;
        rigid.angularVelocity = angularAcceleration;
        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            startRot = transform.rotation;
        }
        else
        {
            startRotCorrection.ToAngleAxis(out angle, out axis);
            if (angle > 0)
            {
                angularAcceleration += angle * axis * Time.deltaTime * 0.5f;
                startRotCorrection = Quaternion.Slerp(Quaternion.identity, startRotCorrection, 0.5f);
            }
        }
    }
}

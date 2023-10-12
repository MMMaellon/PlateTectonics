
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoSpin : UdonSharpBehaviour
{
    public SmartObjectSync sync;
    public void Update()
    {
        if (sync.IsLocalOwner())
        {
            GetComponent<Rigidbody>().angularVelocity = Vector3.forward + Vector3.left / 50;
            if (sync.state != SmartObjectSync.STATE_INTERPOLATING)
            {
                sync.state = SmartObjectSync.STATE_INTERPOLATING;
            }
        }
        else
        {
            sync.rigid.angularVelocity = transform.parent.rotation * sync.spin;
        }
    }
}

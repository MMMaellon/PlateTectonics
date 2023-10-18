
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoSpin : UdonSharpBehaviour
{
    public Rigidbody rigid;
    public void Update()
    {
        rigid.angularVelocity = Vector3.forward;
    }
}

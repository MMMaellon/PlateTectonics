using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.PlateTectonics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerSync : Cyan.PlayerObjectPool.CyanPlayerObjectPoolObject
    {
        public PlayerAttachmentSync attachment;
        public VRCStation chair;
        [System.NonSerialized, UdonSynced]
        public Vector3 position;
        [System.NonSerialized, UdonSynced]
        public Vector3 velocity;
        [System.NonSerialized, UdonSynced]
        public Quaternion rotation;
        [System.NonSerialized, UdonSynced]
        public short updateCounter;

        [System.NonSerialized]
        public float networkUpdateInterval = 0.2f;
        [System.NonSerialized]
        public float syncTime;
#if !UNITY_EDITOR
        public float lagTime
        {
            get => Mathf.Max(networkUpdateInterval, Time.realtimeSinceStartup - Networking.SimulationTime(gameObject));
        }
#else
        public float lagTime
        {
            get => 0.25f;
        }
#endif
        public float interpolation
        {
            get
            {
                // return lagTime <= 0 ? 1 : Mathf.Lerp(0, 1, (Time.timeSinceLevelLoad - syncTime) / lagTime);
                return (Time.timeSinceLevelLoad - syncTime) / networkUpdateInterval;
            }
        }

        [System.NonSerialized]
        public VRCPlayerApi localPlayerAPI;
        public void Start()
        {
            localPlayerAPI = Networking.LocalPlayer;
        }
        [System.NonSerialized]
        public bool local = false;
        public override void OnDeserialization()
        {

            if (updateCounter == attachment.updateCounter)
            {
                attachment.Sync();
                Sync();
            }
        }
        public void Sync()
        {
            syncTime = Time.timeSinceLevelLoad;
            attachment.startTransform.position = transform.position;
            attachment.startTransform.rotation = transform.rotation;
            attachment.startVelTransform.position = lastVel + attachment.transform.position;
            attachment.endTransform.localPosition = position;
            attachment.endTransform.localRotation = rotation;
            attachment.endVelTransform.localPosition = velocity;
        }

        [System.NonSerialized]
        public bool needSync = false;
        [System.NonSerialized]
        public bool syncRequested = false;
        public override void OnPreSerialization()
        {
            attachment.endTransform.position = localPlayerAPI.GetPosition();
            attachment.endTransform.rotation = localPlayerAPI.GetRotation();
            attachment.endVelTransform.position = localPlayerAPI.GetVelocity() + attachment.transform.position;
            position = attachment.endTransform.localPosition;
            rotation = attachment.endTransform.localRotation;
            velocity = localPlayerAPI.GetVelocity();
            Debug.LogWarning("Vel Sent: " + velocity);
            syncTime = Time.timeSinceLevelLoad;
            needSync = false;
            syncRequested = false;
            updateCounter = attachment.updateCounter;
        }
        Vector3 lastPos;
        Vector3 lastVel;
        float lastTime;
        public void LateUpdate()
        {
            if (!Utilities.IsValid(Owner))
            {
                return;
            }
            if (local)
            {
                if (needSync && !syncRequested && syncTime + networkUpdateInterval <= Time.timeSinceLevelLoad)
                {
                    syncRequested = true;
                    RequestSerialization();
                }
            }
            else
            {
                lastPos = transform.position;
                transform.position = HermiteInterpolatePosition();
                lastVel = (transform.position - lastPos) * (Time.timeSinceLevelLoad - lastTime);
                lastTime = Time.timeSinceLevelLoad;
                transform.rotation = Quaternion.Slerp(attachment.startTransform.rotation, attachment.endTransform.rotation, interpolation);
                //force upright
                transform.rotation = Quaternion.FromToRotation(transform.rotation * Vector3.up, Vector3.up) * transform.rotation;
            }
        }
        Vector3 posControl1;
        Vector3 posControl2;
        public Vector3 HermiteInterpolatePosition()
        {//Shout out to Kit Kat for suggesting the improved hermite interpolation
            if (interpolation < 1)
            {
                posControl1 = attachment.startTransform.position + (attachment.startVelTransform.position - attachment.transform.position) * lagTime * interpolation / 3f;
                posControl2 = attachment.endTransform.position - (attachment.endVelTransform.position - attachment.transform.position) * lagTime * (1.0f - interpolation) / 3f;
                return Vector3.Lerp(Vector3.Lerp(posControl1, attachment.endTransform.position, interpolation), Vector3.Lerp(attachment.startTransform.position, posControl2, interpolation), interpolation);
                // return Vector3.Lerp(startPos, endPos, interpolation);
            }
            return attachment.endTransform.position + (attachment.endVelTransform.position - attachment.transform.position) * lagTime * (interpolation - 1);
        }

        public override void _OnOwnerSet()
        {
            if (Utilities.IsValid(Owner) && Owner.isLocal)
            {
                Networking.SetOwner(Owner, attachment.gameObject);
                chair.disableStationExit = true;
                local = true;
                velocity = Vector3.zero;//init
            }
        }

        public override void _OnCleanup()
        {

        }
    }
}

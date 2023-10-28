
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;
using TMPro;
using System.Net;
using UnityEngine.UIElements;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlateTectonicsPlayerSync : CyanPlayerObjectPoolObject
    {
        public PlateTectonicsPlayerAttachment attachment;
        public VRCStation chair;
        [System.NonSerialized]
        public bool needSync;
        public float networkUpdateInterval = 0.33333333333333333f;
        [System.NonSerialized]
        bool local = false;
        [System.NonSerialized]
        public Vector3 _lastSyncedPos;
        [System.NonSerialized]
        public Quaternion _lastSyncedRot;
        [System.NonSerialized]
        public Vector3 _lastSyncedVel;
        public override void OnPreSerialization()
        {
            RequestSerialization();
            _syncedPos = _localPosition;
            _syncedRot = _localRotation;
            _syncedVel = _velocity;
            if (Utilities.IsValid(attachment.plateTectonics))
            {
                attachment.plateTectonics.moved = false;
                attachment.plateTectonics.landedChanged = false;
            }
            attachment.transform.position = transform.position;
            syncTime = Time.timeSinceLevelLoad;
            needSync = false;
            updateCounter++;
        }
        Transform _syncedTransform;
        [System.NonSerialized]
        public Vector3 _syncedPos;
        [System.NonSerialized]
        public Quaternion _syncedRot;
        [System.NonSerialized]
        public Vector3 _syncedVel;
        [System.NonSerialized, UdonSynced]
        public uint updateCounter = 0;
        [System.NonSerialized]
        public uint _syncedUpdateCounter = 0;
        public override void OnDeserialization()
        {
            _syncedUpdateCounter = updateCounter;
            Debug.LogWarning("Sync OnDeserialization " + updateCounter + ">" + attachment.updateCounter);
            _localPosition = localPosition;
            _localRotation = localRotation;
            if (_syncedUpdateCounter > attachment._syncedUpdateCounter)
            {
                JointOnDeserialization();
            }
        }

        public void JointOnDeserialization()
        {
            Debug.LogWarning("JointOnDeserialization");
            _syncedPos = _localPosition;
            _syncedRot = _localRotation;
            _syncedVel = _velocity;
            _syncedTransform = attachment.parentTransform;
            attachment.transform.position = transform.position;
            if (Utilities.IsValid(_syncedTransform))
            {
                _lastSyncedPos = Quaternion.Inverse(_syncedTransform.rotation) * (transform.position - _syncedTransform.position);
                _lastSyncedRot = Quaternion.Inverse(_syncedTransform.rotation) * transform.rotation;
                _lastSyncedVel = Quaternion.Inverse(_syncedTransform.rotation) * lastGlobalVel;
            }
            else
            {
                _lastSyncedPos = transform.position;
                _lastSyncedRot = transform.rotation;
                _lastSyncedVel = lastGlobalVel;
            }
            syncTime = Time.timeSinceLevelLoad - Time.deltaTime;
        }
        [System.NonSerialized]
        public Vector3 _localPosition;
        [System.NonSerialized, UdonSynced]
        public short _localPositionX;
        [System.NonSerialized, UdonSynced]
        public short _localPositionY;
        [System.NonSerialized, UdonSynced]
        public short _localPositionZ;
        public Vector3 localPosition
        {
            get
            {
                return new Vector3(_localPositionX, _localPositionY, _localPositionZ) / 100f;
            }
            set
            {
                _localPositionX = (short) Mathf.Clamp(value.x * 100f, -32000, 32000);
                _localPositionY = (short) Mathf.Clamp(value.y * 100f, -32000, 32000);
                _localPositionZ = (short) Mathf.Clamp(value.z * 100f, -32000, 32000);
                _localPosition = value;
            }
        }

        Vector3 rotationVector;
        float rotationAngle;
        [System.NonSerialized]
        public Quaternion _localRotation;
        [System.NonSerialized, UdonSynced]
        public short _localRotationX;
        [System.NonSerialized, UdonSynced]
        public short _localRotationY;
        [System.NonSerialized, UdonSynced]
        public short _localRotationZ;
        public Quaternion localRotation
        {
            get {
                rotationVector = new Vector3(_localRotationX, _localRotationY, _localRotationZ) / 50f;
                return Quaternion.AngleAxis(rotationVector.magnitude, rotationVector);
            }
            set
            {
                _localRotation = value;
                value.ToAngleAxis(out rotationAngle, out rotationVector);
                rotationVector *= 50f * rotationAngle;
                _localRotationX = (short) Mathf.Clamp(System.Single.IsNaN(rotationVector.x) ? 0 : rotationVector.x, -32000, 32000);
                _localRotationY = (short) Mathf.Clamp(System.Single.IsNaN(rotationVector.y) ? 0 : rotationVector.y, -32000, 32000);
                _localRotationZ = (short) Mathf.Clamp(System.Single.IsNaN(rotationVector.z) ? 0 : rotationVector.z, -32000, 32000);
            }
        }
        [System.NonSerialized]
        public float syncTime;
        float velDiff;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(velocity))]
        public Vector3 _velocity;
        Vector3 lastVel;
        public Vector3 velocity
        {
            get => _velocity;
            set
            {
                lastVel = _velocity;
                _velocity = value;
            }
        }
        Vector3 lastGlobalVel;
        Vector3 nextGlobalPos;
        Vector3 startPos;
        Vector3 startVel;
        Vector3 endPos;
        Vector3 endVel;
        Quaternion startRot;
        Quaternion endRot;
        public void Update()
        {
            if (local)
            {
                if (syncTime + networkUpdateInterval < Time.timeSinceLevelLoad && needSync)
                {
                    RequestSerialization();
                    needSync = false;
                }
            }
                else
            {
                Physics.SyncTransforms();
                if (Utilities.IsValid(_syncedTransform))
                {
                    startPos = _syncedTransform.position + _syncedTransform.rotation * _lastSyncedPos;
                    startRot = _syncedTransform.rotation * _lastSyncedRot;
                    startVel = _syncedTransform.rotation * _lastSyncedVel;
                    endPos = _syncedTransform.position + _syncedTransform.rotation * _syncedPos;
                    endRot = _syncedTransform.rotation * _syncedRot;
                    endVel = _syncedTransform.rotation * _syncedVel;
                }
                else
                {
                    startPos = _lastSyncedPos;
                    startRot = _lastSyncedRot;
                    startVel = _lastSyncedVel;
                    endPos = _syncedPos;
                    endRot = _syncedRot;
                    endVel = _syncedVel;
                }
                nextGlobalPos = HermiteInterpolatePosition(startPos, startVel, endPos, endVel);
                lastGlobalVel = (nextGlobalPos - transform.position) / Time.deltaTime;
                transform.position = nextGlobalPos;
                transform.rotation = Quaternion.Slerp(startRot, endRot, interpolation);
            }
        }

        public override void _OnOwnerSet()
        {
            if (Utilities.IsValid(Owner) && Owner.isLocal)
            {
                // chair.PlayerMobility = VRCStation.Mobility.Mobile;
                Networking.SetOwner(Owner, attachment.gameObject);
                chair.disableStationExit = true;
                local = true;
                velocity = Vector3.zero;//init
            }
        }

        public override void _OnCleanup()
        {

        }

        public void SitInChairDelayed()
        {
            transform.position = Networking.LocalPlayer.GetPosition();
            SendCustomEventDelayedFrames(nameof(SitInChair), 1, VRC.Udon.Common.Enums.EventTiming.Update);
        }

        public string GetFullPath(Transform target)
        {
            Transform pathBuilder = target;
            string tempName = "";
            while (Utilities.IsValid(pathBuilder))
            {
                tempName = "/" + pathBuilder.name + tempName;
                pathBuilder = pathBuilder.parent;
            }
            return tempName;
        }

        // public void RecordTransforms()
        // {
        //     if (Utilities.IsValid(attachment.parentTransform))
        //     {
        //         // localPosition = attachment.parentTransform.InverseTransformPoint(transform.position);
        //         // localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
        //         localPosition = Quaternion.Inverse(attachment.parentTransform.rotation) * (transform.position - attachment.parentTransform.position);
        //         localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
        //     }
        //     else
        //     {
        //         localPosition = transform.position;
        //         localRotation = transform.rotation;
        //     }
        // }

        public void SitInChair()
        {
            // Networking.LocalPlayer.UseAttachedStation();
            chair.UseStation(Networking.LocalPlayer);
        }
        Vector3 posControl1;
        Vector3 posControl2;
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
                return Mathf.Clamp((Time.timeSinceLevelLoad - syncTime) / networkUpdateInterval, 0, 100) / 1.5f;//the 1.5f makes it smoother and gives us room for 
            }
        }
        public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel)
        {//Shout out to Kit Kat for suggesting the improved hermite interpolation
            if (Utilities.IsValid(_syncedTransform))
            {
                if (interpolation < 1)
                {
                    posControl1 = startPos + _syncedTransform.rotation * startVel * lagTime * interpolation / 3f;
                    posControl2 = endPos - _syncedTransform.rotation * endVel * lagTime * (1.0f - interpolation) / 3f;
                    return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
                    // return Vector3.Lerp(startPos, endPos, interpolation);
                }
                return endPos + _syncedTransform.rotation * endVel * lagTime * (interpolation - 1);
            }
            else
            {
                if (interpolation < 1)
                {
                    posControl1 = startPos + startVel * lagTime * interpolation / 3f;
                    posControl2 = endPos - endVel * lagTime * (1.0f - interpolation) / 3f;
                    return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
                    // return Vector3.Lerp(startPos, endPos, interpolation);
                }
                return endPos + endVel * lagTime * (interpolation - 1);
            }
        }
    }
}
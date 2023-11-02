
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
        public Transform startTransform;
        public Transform endTransform;
        public PlateTectonicsPlayerAttachment attachment;
        public VRCStation chair;
        [System.NonSerialized]
        public bool needSync;
        [System.NonSerialized]
        public bool syncRequested;
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
            _syncedPos = _localPosition;
            _syncedRot = _localRotation;
            _syncedVel = _velocity;
            syncTime = Time.timeSinceLevelLoad;
            needSync = false;
            syncRequested = false;
            updateCounter++;
        }
        Transform _syncedTransform;
        [System.NonSerialized]
        public Vector3 _syncedPos;
        [System.NonSerialized]
        public Quaternion _syncedRot;
        [System.NonSerialized]
        public Vector3 _syncedVel;
        // [System.NonSerialized]
        // public Vector3 _syncedGlobalVel;
        [System.NonSerialized, UdonSynced]
        public uint updateCounter = 0;
        [System.NonSerialized]
        public uint _syncedUpdateCounter = 0;
        public override void OnDeserialization()
        {
            _syncedUpdateCounter = updateCounter;
            _localPosition = localPosition;
            _localRotation = localRotation;
            _velocity = velocity;
            if (_syncedUpdateCounter > attachment._syncedUpdateCounter)
            {
                JointOnDeserialization();
            }
        }

        public void JointOnDeserialization()
        {
            _syncedVel = _velocity;
            _syncedTransform = attachment.parentTransform;
            endTransform.SetParent(_syncedTransform, true);
            if (Utilities.IsValid(_syncedTransform))
            {
                endTransform.position = _syncedTransform.position + _syncedTransform.rotation * _localPosition;
                endTransform.rotation = _syncedTransform.rotation * _localRotation;
                _lastSyncedVel = Quaternion.Inverse(_syncedTransform.rotation) * lastGlobalVel;
                // _syncedGlobalVel = _syncedTransform.rotation * _velocity;
            }
            else
            {
                endTransform.position = _localPosition;
                endTransform.rotation = _localRotation;
                _lastSyncedVel = lastGlobalVel;
                // _syncedGlobalVel = _velocity;
            }
            startTransform.SetParent(_syncedTransform, true);
            startTransform.position = transform.position;
            startTransform.rotation = transform.rotation;
            // syncTime = Time.timeSinceLevelLoad - Time.deltaTime;
            syncTime = Time.timeSinceLevelLoad;
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
            get => new Vector3(_localPositionX, _localPositionY, _localPositionZ) / 100f;
            set
            {
                _localPositionX = (short) Mathf.Clamp(value.x * 100f, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localPositionY = (short) Mathf.Clamp(value.y * 100f, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localPositionZ = (short) Mathf.Clamp(value.z * 100f, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
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
                value.ToAngleAxis(out rotationAngle, out rotationVector);
                rotationVector *= 50f * rotationAngle;
                _localRotationX = (short) Mathf.Clamp(System.Single.IsNaN(rotationVector.x) ? 0 : rotationVector.x, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localRotationY = (short) Mathf.Clamp(System.Single.IsNaN(rotationVector.y) ? 0 : rotationVector.y, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localRotationZ = (short) Mathf.Clamp(System.Single.IsNaN(rotationVector.z) ? 0 : rotationVector.z, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localRotation = value;
            }
        }
        [System.NonSerialized]
        public float syncTime;
        float velDiff;
        [System.NonSerialized]
        public Vector3 _velocity;
        [System.NonSerialized, UdonSynced]
        public short _localVelX;
        [System.NonSerialized, UdonSynced]
        public short _localVelY;
        [System.NonSerialized, UdonSynced]
        public short _localVelZ;
        public Vector3 velocity
        {
            get => _velocity = new Vector3(_localVelX, _localVelY, _localVelZ) / 100f;
            set
            {
                _localVelX = (short) Mathf.Clamp(value.x * 100f, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localVelY = (short) Mathf.Clamp(value.y * 100f, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
                _localVelZ = (short) Mathf.Clamp(value.z * 100f, short.MinValue + 10, short.MaxValue - 10);//adding buffer of 10 because fuck unity
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
                if (syncTime + networkUpdateInterval < Time.timeSinceLevelLoad && needSync && !syncRequested)
                {
                    RequestSerialization();
                    syncRequested = true;
                }
            } else
            {
                Physics.SyncTransforms();
                if (Utilities.IsValid(_syncedTransform))
                {
                    startVel = _syncedTransform.rotation * _lastSyncedVel;
                    endVel = _syncedTransform.rotation * _syncedVel;
                }
                else
                {
                    startVel = _lastSyncedVel;
                    endVel = _syncedVel;
                }
                nextGlobalPos = HermiteInterpolatePosition(startTransform.position, startVel, endTransform.position, endVel);
                lastGlobalVel = (nextGlobalPos - transform.position) / Time.deltaTime;
                transform.position = nextGlobalPos;
                transform.rotation = Quaternion.Slerp(startTransform.rotation, endTransform.rotation, interpolation);

                //force upright
                if (attachment.plateTectonics.forceUpright)
                {
                    transform.rotation = Quaternion.FromToRotation(transform.rotation * Vector3.up, Vector3.up) * transform.rotation;
                }
            }
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

        public void SitInChairDelayed()
        {
            attachment.plateTectonics.transform.position = Networking.LocalPlayer.GetPosition();
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
                return (Time.timeSinceLevelLoad - syncTime) / networkUpdateInterval;
            }
        }
        public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel)
        {//Shout out to Kit Kat for suggesting the improved hermite interpolation
            if (Utilities.IsValid(_syncedTransform))
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
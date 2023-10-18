﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;
using TMPro;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlateTectonicsPlayerSync : CyanPlayerObjectPoolObject
    {
        public PlateTectonicsPlayerAttachment attachment;
        public VRCStation chair;
        public float networkUpdateInterval = 0.25f;
        [System.NonSerialized]
        bool local = false;
        [System.NonSerialized]
        public Vector3 _lastSyncedPos;
        [System.NonSerialized]
        public Quaternion _lastSyncedRot;
        public override void OnPreSerialization()
        {
            _lastSyncedPos = _localPosition;
            _lastSyncedRot = _localRotation;
            if (Utilities.IsValid(attachment.plateTectonics))
            {
                attachment.plateTectonics.moved = false;
                attachment.plateTectonics.landedChanged = false;
            }
            syncTime = Time.timeSinceLevelLoad;
        }
        public override void OnDeserialization()
        {
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
                _localRotationX = (short) Mathf.Clamp(rotationVector.x, -32000, 32000);
                _localRotationY = (short) Mathf.Clamp(rotationVector.y, -32000, 32000);
                _localRotationZ = (short) Mathf.Clamp(rotationVector.z, -32000, 32000);
            }
        }
        [System.NonSerialized]
        public float syncTime;
        float velDiff;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(velocity))]
        public Vector3 _velocity;
        public Vector3 velocity
        {
            get => _velocity;
            set
            {
                _velocity = value;
                // if (local)
                // {
                //     velDiff = Vector3.Distance(_velocity, value);
                //     if ((value.magnitude < 0.01f && _velocity.magnitude > 0.01f) || velDiff > value.magnitude * 0.05f)
                //     {
                //         if (Time.timeSinceLevelLoad - syncTime > networkUpdateInterval || value == Vector3.zero)
                //         {
                //             _velocity = value;
                //             RequestSerialization();
                //         }
                //     }
                // }
                // else
                // {
                //     _velocity = value;
                //     syncTime = Time.timeSinceLevelLoad;
                // }
            }
        }
        Transform lastTransform;
        Vector3 lastLocalPos;
        Quaternion lastLocalRot;
        Vector3 calcedVel;
        bool firstFrame;
        Vector3 startPos;
        Vector3 startVel;
        Vector3 endPos;
        Quaternion startRot;
        Quaternion endRot;
        public void Update()
        {
            if (local)
            {
                // RecordTransforms();
                // if (Utilities.IsValid(parentTransform))
                // {
                //     velocity = Quaternion.Inverse(parentTransform.rotation) * Owner.GetVelocity();
                // }
                // else
                // {
                //     velocity = Owner.GetVelocity();
                // }
                // transform.localPosition = localPosition;
                // transform.localRotation = localRotation;
            }
            else
            {
                Physics.SyncTransforms();
                // transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition + _velocity * (Time.timeSinceLevelLoad - syncTime), 0.1f);
                // transform.localRotation = Quaternion.Slerp(transform.localRotation, localRotation, 0.1f);
                // localPosition = attachment.parentTransform.InverseTransformPoint(transform.position);
                // localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
                if (Utilities.IsValid(attachment.parentTransform))
                {
                    if (lastTransform == attachment.parentTransform)
                    {
                        // transform.position = Vector3.Lerp(attachment.parentTransform.position + attachment.parentTransform.rotation * lastLocalPos, attachment.parentTransform.position + attachment.parentTransform.rotation * (localPosition + velocity * (Time.timeSinceLevelLoad - syncTime)), 0.1f);
                        if (firstFrame)
                        {
                            transform.position = Vector3.Lerp(transform.position, attachment.parentTransform.position + attachment.parentTransform.rotation * localPosition, 0.1f);
                            transform.rotation = Quaternion.Slerp(transform.rotation, attachment.parentTransform.rotation * localRotation, 0.1f);
                        } else {
                            transform.position = Vector3.Lerp(attachment.parentTransform.position + attachment.parentTransform.rotation * lastLocalPos, attachment.parentTransform.position + attachment.parentTransform.rotation * localPosition, 0.1f);
                            transform.rotation = Quaternion.Slerp(attachment.parentTransform.rotation * lastLocalRot, attachment.parentTransform.rotation * localRotation, 0.1f);
                        }
                        lastLocalPos = Quaternion.Inverse(attachment.parentTransform.rotation) * (transform.position - attachment.parentTransform.position);
                        lastLocalRot = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
                    }
                    else
                    {
                        // transform.position = Vector3.Lerp(transform.position, attachment.parentTransform.position + attachment.parentTransform.rotation * (localPosition + velocity * (Time.timeSinceLevelLoad - syncTime)), 0.1f);
                        transform.position = Vector3.Lerp(transform.position, attachment.parentTransform.position + attachment.parentTransform.rotation * localPosition, 0.1f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, attachment.parentTransform.rotation * localRotation, 0.1f);
                        firstFrame = true;
                    }
                    lastTransform = attachment.parentTransform;
                } else
                {
                    transform.position = Vector3.Lerp(transform.position, localPosition + _velocity * (Time.timeSinceLevelLoad - syncTime), 0.1f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, localRotation, 0.1f);
                    lastTransform = null;
                }
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

        public void RecordTransforms()
        {
            if (Utilities.IsValid(attachment.parentTransform))
            {
                // localPosition = attachment.parentTransform.InverseTransformPoint(transform.position);
                // localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
                localPosition = Quaternion.Inverse(attachment.parentTransform.rotation) * (transform.position - attachment.parentTransform.position);
                localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
            }
            else
            {
                localPosition = transform.position;
                localRotation = transform.rotation;
            }
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
            get => Time.realtimeSinceStartup - Networking.SimulationTime(gameObject);
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
                return lagTime <= 0 ? 1 : Mathf.Lerp(0, 1, (Time.timeSinceLevelLoad - syncTime) / lagTime);
            }
        }
        public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel)
        {//Shout out to Kit Kat for suggesting the improved hermite interpolation
            posControl1 = startPos + startVel * lagTime * interpolation / 3f;
            posControl2 = endPos - endVel * lagTime * (1.0f - interpolation) / 3f;
            return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
        }
    }
}
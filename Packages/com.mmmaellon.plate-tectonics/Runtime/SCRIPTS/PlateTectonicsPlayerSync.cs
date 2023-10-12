
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlateTectonicsPlayerSync : CyanPlayerObjectPoolObject
    {
        public VRCStation chair;
        [System.NonSerialized, UdonSynced]
        public Vector3 localPosition;
        bool local = false;
        public float networkUpdateInterval = 0.25f;

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(localRotation))]
        public Quaternion _localRotation;
        Quaternion checkQuat;
        public Quaternion localRotation
        {
            get => _localRotation;
            set
            {
                if (local)
                {
                    if (Vector3.Angle(_localRotation * Vector3.forward, value * Vector3.forward) > 15f)//arbitrarily picked limit
                    {
                        _localRotation = value;
                        RequestSerialization();
                    }
                }
                else
                {
                    _localRotation = value;
                }
            }
        }
        float syncTime;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(velocity))]
        public Vector3 _velocity;
        public Vector3 velocity
        {
            get => _velocity;
            set
            {
                if (local)
                {
                    if (Vector3.Distance(_velocity, value) > 0.01f)
                    {
                        if (Time.timeSinceLevelLoad - syncTime > networkUpdateInterval || value == Vector3.zero)
                        {
                            _velocity = value;
                            RecordTransforms();
                            RequestSerialization();
                            syncTime = Time.timeSinceLevelLoad;
                        }
                    }
                }
                else
                {
                    _velocity = value;
                    syncTime = Time.timeSinceLevelLoad;
                }
            }
        }

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(parentTransformName))] string _parentTransformName = "";
        public string parentTransformName
        {
            get => _parentTransformName;
            set
            {
                if (!Utilities.IsValid(value) || value == "")
                {
                    _parentTransformName = "";
                    parentTransform = Utilities.IsValid(plateTectonics) ? plateTectonics.transform : null;
                    return;
                }
                GameObject parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {
                    _parentTransformName = value;
                    parentTransform = parentObj.transform;
                    return;
                }
                _parentTransformName = "";
            }
        }

        [System.NonSerialized]
        public PlateTectonics plateTectonics;
        [System.NonSerialized, FieldChangeCallback(nameof(parentTransform))]
        public Transform _parentTransform;
        public Transform parentTransform
        {
            get => _parentTransform;
            set
            {
                _parentTransform = value;
                transform.SetParent(value, true);
                if (Utilities.IsValid(plateTectonics))
                {
                    plateTectonics.moveWorld = Utilities.IsValid(transform.parent);
                }
                if (Utilities.IsValid(Owner))
                {
                    velocity = Utilities.IsValid(_parentTransform) ? Quaternion.Inverse(_parentTransform.rotation) * Owner.GetVelocity() : Owner.GetVelocity();
                    RequestSerialization();
                }
            }
        }

        public override void PostLateUpdate()
        {
            if (local)
            {
                RecordTransforms();
                if (Utilities.IsValid(parentTransform))
                {
                    velocity = Quaternion.Inverse(parentTransform.rotation) * Owner.GetVelocity();
                }
                else
                {
                    velocity = Owner.GetVelocity();
                }
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition + _velocity * (Time.timeSinceLevelLoad - syncTime), 0.1f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, localRotation, 0.1f);
            }
        }

        public override void _OnOwnerSet()
        {
            if (Utilities.IsValid(Owner) && Owner.isLocal)
            {
                chair.PlayerMobility = VRCStation.Mobility.Mobile;
                chair.disableStationExit = true;
                local = true;
                SitInChairDelayed();
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
            if (Utilities.IsValid(parentTransform))
            {
                localPosition = parentTransform.InverseTransformPoint(Owner.GetPosition());
                localRotation = Quaternion.Inverse(parentTransform.rotation) * Owner.GetRotation();
            }
            else
            {
                localPosition = Owner.GetPosition();
                localRotation = Owner.GetRotation();
            }
        }

        public void SitInChair()
        {
            // Networking.LocalPlayer.UseAttachedStation();
            chair.UseStation(Networking.LocalPlayer);
        }
    }
}
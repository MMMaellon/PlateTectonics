
using UdonSharp;
using UnityEngine;
using UnityEngine.Diagnostics;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlateTectonicsPlayerAttachment : UdonSharpBehaviour
    {
        [System.NonSerialized, UdonSynced]
        public uint updateCounter = 0;
        [System.NonSerialized]
        public uint _syncedUpdateCounter = 0;
        public override void OnDeserialization()
        {
            _syncedUpdateCounter = updateCounter;
            if (sync._syncedUpdateCounter > _syncedUpdateCounter)
            {
                sync.JointOnDeserialization();
            }
        }

        [System.NonSerialized]
        public PlateTectonics plateTectonics;
        public PlateTectonicsPlayerSync sync;
        public Transform localTransform;

        [System.NonSerialized, FieldChangeCallback(nameof(parentTransform))]
        public Transform _parentTransform;
        public Transform parentTransform
        {
            get => _parentTransform;
            set
            {
                _parentTransform = value;
                localTransform.SetParent(null, true);
                transform.SetParent(null, true);
                transform.localScale = Vector3.one;
                if (Utilities.IsValid(value))
                {
                    transform.position = value.position;
                    transform.rotation = value.rotation;
                    transform.SetParent(value, true);
                }
                else
                {
                    transform.position = Vector3.zero;
                    transform.rotation = Quaternion.identity;
                }
                localTransform.SetParent(transform, true);
                if (Utilities.IsValid(Networking.LocalPlayer) && Networking.LocalPlayer.IsOwner(gameObject))
                {
                    updateCounter = sync.updateCounter;
                    plateTectonics.RecordTransforms();
                    RequestSerialization();
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
                    parentTransform = null;
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
    }
}
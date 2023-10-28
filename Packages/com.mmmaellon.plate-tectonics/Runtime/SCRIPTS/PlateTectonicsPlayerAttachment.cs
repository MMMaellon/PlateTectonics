
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
            Debug.LogWarning("attachment OnDeserialization " + sync.updateCounter + ">" + updateCounter);
            if (sync._syncedUpdateCounter > _syncedUpdateCounter)
            {
                sync.JointOnDeserialization();
            }
        }

        [System.NonSerialized]
        public PlateTectonics plateTectonics;
        public PlateTectonicsPlayerSync sync;

        [System.NonSerialized, FieldChangeCallback(nameof(parentTransform))]
        public Transform _parentTransform;
        public Transform parentTransform
        {
            get => _parentTransform;
            set
            {
                _parentTransform = value;
                transform.position = sync.transform.position;
                transform.SetParent(value, true);
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
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlateTectonicsPlayerAttachment : UdonSharpBehaviour
    {

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
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    // plateTectonics.transform.SetParent(value, true);
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
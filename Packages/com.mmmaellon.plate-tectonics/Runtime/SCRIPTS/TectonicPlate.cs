
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Utility;

namespace MMMaellon
{
    public class TectonicPlate : UdonSharpBehaviour
    {
        [Tooltip("Leave blank to use this object as the anchor")]
        public Transform plateAnchor = null;
        [System.NonSerialized]
        public PlateTectonics plateTectonics;
        public bool disablePlateTectonicsOnEnter = false;
        public bool disablePlateTectonicsOnExit = false;
        string transformName = "";
        Vector3 startPos;
        Quaternion startRot;
        public void Start()
        {
            plateTectonics = GetComponentInParent<PlateTectonics>();
            transformName = Utilities.IsValid(plateAnchor) ? GetFullPath(plateAnchor) : GetFullPath(transform);
        }
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                if (disablePlateTectonicsOnEnter)
                {
                    DisableTectonicPlate();
                }
                else
                {
                    EnableTectonicPlate();
                }
            }
        }
        public override void OnPlayerCollisionEnter(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                if (disablePlateTectonicsOnEnter)
                {
                    DisableTectonicPlate();
                }
                else
                {
                    EnableTectonicPlate();
                }
            }
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal && disablePlateTectonicsOnExit)
            {
                DisableTectonicPlate();
            }
        }
        public override void OnPlayerCollisionExit(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal && disablePlateTectonicsOnExit)
            {
                DisableTectonicPlate();
            }
        }

        public void EnableTectonicPlate()
        {
            if (Utilities.IsValid(plateTectonics) && Utilities.IsValid(plateTectonics.localPlayer))
            {
                plateTectonics.localPlayer.parentTransformName = transformName;
            }
        }
        public void DisableTectonicPlate()
        {
            if (Utilities.IsValid(plateTectonics) && Utilities.IsValid(plateTectonics.localPlayer))
            {
                plateTectonics.localPlayer.parentTransformName = "";
            }
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
    }
}
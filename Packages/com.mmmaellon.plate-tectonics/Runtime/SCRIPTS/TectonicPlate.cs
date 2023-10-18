
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
        // public bool disablePlateTectonicsOnEnter = false;
        // public bool disablePlateTectonicsOnExit = false;
        [System.NonSerialized]
        public string transformName = "";
        // Vector3 startPos;
        // Quaternion startRot;
        public void Start()
        {
            transformName = Utilities.IsValid(plateAnchor) ? GetFullPath(plateAnchor) : GetFullPath(transform);
        }
        // public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        // {
        // }
        // public override void OnPlayerCollisionEnter(VRCPlayerApi player)
        // {
        //     if (Utilities.IsValid(player) && player.isLocal)
        //     {
        //         if (disablePlateTectonicsOnEnter)
        //         {
        //             DisableTectonicPlate();
        //         }
        //         else
        //         {
        //             EnableTectonicPlate();
        //         }
        //     }
        // }
        // public override void OnPlayerTriggerExit(VRCPlayerApi player)
        // {
        //     if (Utilities.IsValid(player) && player.isLocal && disablePlateTectonicsOnExit)
        //     {
        //         DisableTectonicPlate();
        //     }
        // }
        // public override void OnPlayerCollisionExit(VRCPlayerApi player)
        // {
        //     if (Utilities.IsValid(player) && player.isLocal && disablePlateTectonicsOnExit)
        //     {
        //         DisableTectonicPlate();
        //     }
        // }
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
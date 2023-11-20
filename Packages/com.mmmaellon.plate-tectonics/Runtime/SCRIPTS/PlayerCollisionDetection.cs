
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.PlateTectonics
{
    public class PlayerCollisionDetection : UdonSharpBehaviour
    {
        public PlateTectonics plateTectonics;

        TectonicPlate hitPlate;

        public void OnTriggerStay(Collider hit)
        {
            if (!Utilities.IsValid(hit) || !Utilities.IsValid(plateTectonics.localPlayer) || !plateTectonics.localPlayerAPI.IsPlayerGrounded())
            {
                return;
            }
            hitPlate = hit.GetComponent<TectonicPlate>();
            if (!Utilities.IsValid(hitPlate) || plateTectonics.attachment.plateName == hitPlate.transformName)
            {
                return;
            }
            plateTectonics.attachment.SetParent(hitPlate.transformName);
        }
        public void OnTriggerExit(Collider hit)
        {
            if (!Utilities.IsValid(hit) || !Utilities.IsValid(plateTectonics.attachment))
            {
                return;
            }
            hitPlate = hit.GetComponent<TectonicPlate>();
            if (!Utilities.IsValid(hitPlate) || hitPlate.transformName != plateTectonics.attachment.plateName)
            {
                return;
            }
            plateTectonics.attachment.SetParent(plateTectonics.worldParentName);
        }
    }
}

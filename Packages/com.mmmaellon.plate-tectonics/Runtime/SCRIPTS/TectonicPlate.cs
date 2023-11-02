
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
        public string transformName = "";
        public void Start()
        {
            transformName = Utilities.IsValid(plateAnchor) ? GetFullPath(plateAnchor) : GetFullPath(transform);
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
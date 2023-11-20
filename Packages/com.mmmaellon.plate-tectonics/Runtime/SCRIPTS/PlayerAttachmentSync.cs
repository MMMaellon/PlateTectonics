
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.PlateTectonics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerAttachmentSync : UdonSharpBehaviour
    {
        public PlayerSync sync;
        public Transform plateTransform;
        public Transform startTransform;
        public Transform endTransform;
        public Transform startVelTransform;
        public Transform endVelTransform;
        [System.NonSerialized, UdonSynced]
        public string plateName;
        [System.NonSerialized, UdonSynced]
        public short updateCounter;

        public override void OnPreSerialization(){
            // updateCounter = (short)((updateCounter + 1) % 1001);
        }

        public override void OnDeserialization()
        {
            if (updateCounter == sync.updateCounter)
            {
                Sync();
                sync.Sync();
            }
        }

        public void Sync()
        {
            ClearParent();
            SyncPlateFromName();
            AttachToSyncedPlate();
        }

        void SyncPlateFromName()
        {
            if (!Utilities.IsValid(plateName) || plateName == "")
            {
                plateTransform = null;
                return;
            }
            GameObject parentObj = GameObject.Find(plateName);
            if (Utilities.IsValid(parentObj))
            {
                plateTransform = parentObj.transform;
                return;
            }
        }

        void ClearParent()
        {
            startTransform.SetParent(null, true);
            endTransform.SetParent(null, true);
            startVelTransform.SetParent(null, true);
            startVelTransform.position -= transform.position;
            endVelTransform.SetParent(null, true);
            endVelTransform.position -= transform.position;
            // transform.SetParent(null, false);
        }

        void AttachToSyncedPlate()
        {
            transform.SetParent(plateTransform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (Utilities.IsValid(plateTransform))
            {
                transform.localScale = new Vector3(1 / plateTransform.lossyScale.x, 1 / plateTransform.lossyScale.y, 1 / plateTransform.lossyScale.z);
            }
            else
            {
                transform.localScale = Vector3.up;
            }
            startTransform.SetParent(transform, true);
            endTransform.SetParent(transform, true);
            startVelTransform.SetParent(transform, true);
            startVelTransform.position += transform.position;
            endVelTransform.SetParent(transform, true);
            endVelTransform.position += transform.position;
        }

        public void SetParent(string newPlateName)
        {
            if (!sync.local)
            {
                return;
            }
            plateName = newPlateName;
            Sync();
            updateCounter = (short)((updateCounter + 1) % 1001);
            RequestSerialization();
        }
    }
}

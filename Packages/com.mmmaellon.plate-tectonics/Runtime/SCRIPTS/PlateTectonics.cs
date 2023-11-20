
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;
using UnityEngine.UIElements;
using VRC.Udon.Common;

namespace MMMaellon.PlateTectonics
{
    public class PlateTectonics : CyanPlayerObjectPoolEventListener
    {
        public PlayerCollisionDetection collision;
        public TectonicPlate startingPlate;
        public Transform worldParent;
        [System.NonSerialized]
        public string worldParentName;
        [System.NonSerialized]
        public VRCPlayerApi localPlayerAPI;
        Vector3 worldStartPos;
        public void Start()
        {
            worldParentName = GetFullPath(worldParent);
            localPlayerAPI = Networking.LocalPlayer;
            worldStartPos = worldParent.position;
        }

        public void AttachToStartingPlate()
        {
            if (Utilities.IsValid(startingPlate))
            {
                localPlayer.attachment.SetParent(startingPlate.transformName);
            }
        }

        bool respawn = true;
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal && Utilities.IsValid(localPlayer))
            {
                respawn = true;
                SendCustomEventDelayedFrames(nameof(SitInChairDelayed), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            }
        }
        public void SitInChairDelayed()
        {
            AttachToStartingPlate();
            localPlayer.transform.position = localPlayerAPI.GetPosition();
            localPlayer.transform.rotation = localPlayerAPI.GetRotation();
            attachment.startTransform.position = localPlayerAPI.GetPosition();
            attachment.startTransform.rotation = localPlayerAPI.GetRotation();
            worldParent.position = worldStartPos;
            localPlayerAPI.TeleportTo(localPlayer.transform.position, localPlayer.transform.rotation);
            SendCustomEventDelayedFrames(nameof(SitInChair), 1, VRC.Udon.Common.Enums.EventTiming.Update);
        }
        public void SitInChair()
        {
            AttachToStartingPlate();
            localPlayer.transform.position = localPlayerAPI.GetPosition();
            localPlayer.transform.rotation = localPlayerAPI.GetRotation();
            attachment.startTransform.position = localPlayerAPI.GetPosition();
            attachment.startTransform.rotation = localPlayerAPI.GetRotation();
            localPlayer.chair.PlayerMobility = VRCStation.Mobility.Mobile;
            localPlayer.chair.UseStation(localPlayerAPI);
            respawn = false;
            localPlayer.RequestSerialization();
        }

        public override void _OnLocalPlayerAssigned()
        {

        }

        [System.NonSerialized]
        public PlayerSync localPlayer;

        [System.NonSerialized]
        public PlayerAttachmentSync attachment;
        public override void _OnPlayerAssigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                localPlayer = poolObject.GetComponent<PlayerSync>();
                localPlayer.transform.position = localPlayerAPI.GetPosition();
                localPlayer.transform.rotation = localPlayerAPI.GetRotation();

                collision.transform.SetParent(localPlayer.transform);
                collision.transform.localPosition = Vector3.zero;
                collision.transform.localRotation = Quaternion.identity;

                SendCustomEventDelayedSeconds(nameof(SitInChair), 1, VRC.Udon.Common.Enums.EventTiming.Update);//1 second delay is arbitrarily picked but it helps fix bugs

                attachment = localPlayer.attachment;
                AttachToStartingPlate();
            }
        }

        public override void _OnPlayerUnassigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {

        }

        Vector3 plateInfluence;
        public void FixedUpdate()
        {
            if (!Utilities.IsValid(localPlayer) || respawn)
            {
                return;
            }
            
            Physics.SyncTransforms();
            if (localPlayerAPI.IsPlayerGrounded())
            {
                calcPlateInfluence();
            } else if (plateInfluence.magnitude > 0) {
                localPlayerAPI.SetVelocity(localPlayerAPI.GetVelocity() + plateInfluence);
                plateInfluence = Vector3.zero;
            }

            worldParent.Translate(-1 * plateInfluence * Time.fixedDeltaTime);
            localPlayer.transform.position = localPlayerAPI.GetPosition();
            localPlayer.transform.rotation = localPlayerAPI.GetRotation();

            if (localPlayerAPI.GetVelocity().magnitude > 0.001f || !localPlayerAPI.IsPlayerGrounded() || Vector3.Distance(attachment.startTransform.position, localPlayerAPI.GetPosition()) > 0.005f)
            {
                attachment.startTransform.position = localPlayerAPI.GetPosition();
                attachment.startTransform.rotation = localPlayerAPI.GetRotation();
            }
            
            CheckSync();
        }

        public void calcPlateInfluence()
        {
            plateInfluence = (localPlayer.attachment.startTransform.position - localPlayer.transform.position) / Time.fixedDeltaTime;
        }

        public void CheckSync()
        {
            localPlayer.needSync = localPlayer.needSync || Vector3.Angle(attachment.endTransform.rotation * Vector3.forward, localPlayer.transform.rotation * Vector3.forward) > 10f;                                             //check rotation
            localPlayer.needSync = localPlayer.needSync || (attachment.endVelTransform.localPosition.y >= 0 && attachment.startVelTransform.localPosition.y < -0.01f) || Vector3.Distance(attachment.endVelTransform.localPosition, attachment.startVelTransform.localPosition) > 0.1f;                                         //check velocity
            localPlayer.needSync = localPlayer.needSync || Vector3.Distance(localPlayer.transform.position, attachment.endTransform.position + (attachment.endVelTransform.position - attachment.transform.position) * (Time.timeSinceLevelLoad - localPlayer.syncTime)) > 0.1f; //check position
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
        TectonicPlate hitPlate;

        public void OnPlateStay(Collider hit)
        {
            if (!Utilities.IsValid(hit) || !Utilities.IsValid(localPlayer) || !localPlayerAPI.IsPlayerGrounded())
            {
                return;
            }
            hitPlate = hit.GetComponent<TectonicPlate>();
            if (!Utilities.IsValid(hitPlate) || attachment.plateName == hitPlate.transformName)
            {
                return;
            }
            attachment.SetParent(hitPlate.transformName);
        }
        public void OnPlateExit(Collider hit)
        {
            if (!Utilities.IsValid(hit) || !Utilities.IsValid(attachment))
            {
                return;
            }
            hitPlate = hit.GetComponent<TectonicPlate>();
            if (!Utilities.IsValid(hitPlate) || hitPlate.transformName != attachment.plateName)
            {
                return;
            }
            attachment.SetParent(worldParentName);
        }

    }
}

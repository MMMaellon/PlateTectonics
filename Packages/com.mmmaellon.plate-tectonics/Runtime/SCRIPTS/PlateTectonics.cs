
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;
using UnityEngine.UIElements;

namespace MMMaellon
{
    public class PlateTectonics : CyanPlayerObjectPoolEventListener
    {
        public TectonicPlate startingPlate;
        public bool keepWorldStatic = false;
        public bool resetOnRespawn = true;
        Vector3 plateLockPosition;
        Quaternion plateLockRotation;

        [FieldChangeCallback(nameof(plateActive))]
        bool _plateActive = false;
        public bool plateActive
        {
            get => _plateActive;
            set
            {
                if (Utilities.IsValid(localPlayer))
                {
                    _plateActive = value && Utilities.IsValid(localPlayer.parentTransform);
                    if (_plateActive)
                    {
                        plateLockPosition = localPlayer.parentTransform.position;
                        plateLockRotation = localPlayer.parentTransform.rotation;
                    }
                }
            }
        }

        Vector3 startPos;
        Quaternion startRot;
        VRCPlayerApi localPlayerAPI;
        public void Start()
        {
            startPos = transform.localPosition;
            startRot = transform.localRotation;
            localPlayerAPI = Networking.LocalPlayer;
        }

        public void AttachToStartingPlate()
        {
            if (Utilities.IsValid(startingPlate))
            {
                startingPlate.EnableTectonicPlate();
            }
            else
            {
                plateActive = false;
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal && Utilities.IsValid(localPlayer))
            {
                if (resetOnRespawn)
                {
                    ResetTransforms();
                }
                localPlayer.SitInChairDelayed();
            }
        }

        Vector3 respawnPos;
        Quaternion respawnRot;
        public void ResetTransforms()
        {
            respawnPos = transform.InverseTransformPoint(localPlayerAPI.GetPosition());
            respawnRot = Quaternion.Inverse(transform.rotation) * localPlayerAPI.GetRotation();
            transform.localPosition = startPos;
            transform.localRotation = startRot;
            localPlayerAPI.TeleportTo(transform.TransformPoint(respawnPos), transform.rotation * respawnRot, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, false);
            localPlayerAPI.SetVelocity(Vector3.zero);
            AttachToStartingPlate();
        }

        Quaternion rotationalDiff;
        public void Update()
        {
            if (!plateActive || !Utilities.IsValid(localPlayer) || !Utilities.IsValid(localPlayer.parentTransform))
            {
                return;
            }
            if (localPlayerAPI.IsPlayerGrounded())
            {
                rotationalDiff = Quaternion.Inverse(localPlayer.parentTransform.rotation) * plateLockRotation;
                transform.rotation = transform.rotation * localPlayer.parentTransform.localRotation * rotationalDiff * Quaternion.Inverse(localPlayer.parentTransform.localRotation);
            }
            else
            {
                plateLockRotation = localPlayer.parentTransform.rotation;
            }
            transform.position += plateLockPosition - localPlayer.parentTransform.position;
        }

        public override void _OnLocalPlayerAssigned()
        {

        }

        [System.NonSerialized]
        public PlateTectonicsPlayerSync localPlayer;
        PlateTectonicsPlayerSync tempPlayer;
        public override void _OnPlayerAssigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {
            tempPlayer = poolObject.GetComponent<PlateTectonicsPlayerSync>();
            tempPlayer.plateTectonics = this;
            if (Utilities.IsValid(player) && player.isLocal)
            {
                localPlayer = tempPlayer;
                AttachToStartingPlate();
            }
        }

        public override void _OnPlayerUnassigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {

        }
    }
}
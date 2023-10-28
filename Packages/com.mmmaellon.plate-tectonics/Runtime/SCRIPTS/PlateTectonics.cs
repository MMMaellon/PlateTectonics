
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Cyan.PlayerObjectPool;
using UnityEngine.UIElements;
using VRC.Udon.Common;

namespace MMMaellon
{
    public class PlateTectonics : CyanPlayerObjectPoolEventListener
    {
        public CharacterController character;
        public TectonicPlate startingPlate;
        VRCPlayerApi localPlayerAPI;
        Vector3 gravity;
        public void Start()
        {
            localPlayerAPI = Networking.LocalPlayer;
            gravity = Physics.gravity;
        }

        public void AttachToStartingPlate()
        {
            if (Utilities.IsValid(startingPlate))
            {
                localPlayer.attachment.parentTransformName = startingPlate.transformName;
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal && Utilities.IsValid(localPlayer))
            {
                AttachToStartingPlate();
                transform.position = localPlayerAPI.GetPosition();
                transform.rotation = localPlayerAPI.GetRotation();
                localPlayer.SitInChairDelayed();
            }
        }

        Quaternion rotationalDiff;

        public override void _OnLocalPlayerAssigned()
        {

        }

        [System.NonSerialized]
        public PlateTectonicsPlayerSync localPlayer;

        [System.NonSerialized]
        public PlateTectonicsPlayerAttachment attachment;
        PlateTectonicsPlayerSync tempPlayer;
        float runSpeed;
        float walkSpeed;
        float strafeSpeed;
        float jumpSpeed;
        public override void _OnPlayerAssigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {
            tempPlayer = poolObject.GetComponent<PlateTectonicsPlayerSync>();
            tempPlayer.attachment.plateTectonics = this;
            if (Utilities.IsValid(player) && player.isLocal)
            {
                transform.position = localPlayerAPI.GetPosition();
                transform.rotation = localPlayerAPI.GetRotation();

                localPlayer = tempPlayer;
                // localPlayer.transform.position = transform.position;

                localPlayer.transform.SetParent(transform, false);
                localPlayer.transform.localPosition = Vector3.zero;
                localPlayer.transform.localRotation = Quaternion.identity;
                localPlayer.SitInChairDelayed();
                localPlayer.SendCustomEventDelayedSeconds("SitInChairDelayed", 30);//sometimes you don't end up in the chair, so let's auto retry in 30 seconds

                attachment = localPlayer.attachment;
                runSpeed = localPlayerAPI.GetRunSpeed();
                walkSpeed = localPlayerAPI.GetWalkSpeed();
                strafeSpeed = localPlayerAPI.GetStrafeSpeed();
                jumpSpeed = localPlayerAPI.GetJumpImpulse();
                localPlayerAPI.SetRunSpeed(0);
                localPlayerAPI.SetWalkSpeed(0);
                localPlayerAPI.SetStrafeSpeed(0);
                localPlayerAPI.SetJumpImpulse(0);
                AttachToStartingPlate();
            }
        }

        public override void _OnPlayerUnassigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {

        }

        bool lastJump;
        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            lastJump = value;
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            input.y = value;
        }
        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            input.x = value;
        }
        [System.NonSerialized]
        public bool snapTurn = false;
        [System.NonSerialized]
        bool turned = false;
        float lookH;
        public override void InputLookHorizontal(float value, UdonInputEventArgs args)
        {
            // base.InputMoveHorizontal(value, args);
            if (snapTurn)
            {
                if (!turned && value < 0.5f)
                {
                    turned = true;
                }
                else
                {
                    if (value < -0.5f)
                    {
                        lookH = -1f;
                        turned = true;
                    }
                    else if (value > 0.5f)
                    {
                        lookH = 1f;
                        turned = true;
                    }
                    else
                    {
                        lookH = 0f;
                    }
                }
            }
            else
            {
                lookH = value * 360f;
            }
        }

        Vector3 plateInfluence;
        Vector2 input;
        Vector3 moveInfluence;
        Vector3 playerForward;
        Quaternion moveRotation;
        float lastY;

        Vector3 _playerRetainedVelocity;
        Vector3 STICK_TO_GROUND_FORCE = new Vector3(0, -2f, 0);
        private const float RATE_OF_AIR_ACCELERATION = 5f;
        Vector2 speed;
        Vector3 localVelocity;
        Vector3 maxAc;
        Vector3 minAc;
        Vector3 inputAcceleration;
        [System.NonSerialized]
        public bool moved = false;
        bool coyoteGrounded = false;
        float coyoteTime = 0.1f;
        float lastGrounded = 0;
        Vector3 lastMovePos;
        public void FixedUpdate()
        {
            //Stolen from client sim
            if (!Utilities.IsValid(localPlayer))
            {
                return;
            }
            Physics.SyncTransforms();
            speed = GetSpeed();

            if (localPlayerAPI.IsUserInVR())
            {
                playerForward = transform.rotation * Vector3.forward;
                playerForward.y = 0;
            }
            else
            {
                playerForward = localPlayerAPI.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
                playerForward.y = 0;
                if (playerForward.magnitude <= 0)
                {
                    playerForward = transform.rotation * Vector3.forward;
                }
            }
            moveRotation = Quaternion.FromToRotation(Vector3.forward, playerForward);

            // Always move along the camera forward as it is the direction that it being aimed at
            moveInfluence = input.y * speed.x * (moveRotation * Vector3.forward) + input.x * speed.y * (moveRotation * Vector3.right);
            moved = moveInfluence.magnitude > 0;

            //calc coyote time
            if (character.isGrounded)
            {
                lastGrounded = Time.timeSinceLevelLoad;
                coyoteGrounded = true;
            }
            else
            {
                coyoteGrounded = coyoteGrounded && (lastGrounded + coyoteTime > Time.timeSinceLevelLoad);
            }

            if (coyoteGrounded)
            {
                if (lastJump)
                {
                    _playerRetainedVelocity = moveInfluence;
                    _playerRetainedVelocity.y = jumpSpeed;
                    coyoteGrounded = false;
                    lastJump = false;
                }
                else
                {
                    _playerRetainedVelocity = moveInfluence;
                }
            }
            else
            {
                moved = true;
                // Slowly add velocity from movement inputs
                localVelocity = Quaternion.Inverse(moveRotation) * character.velocity;
                localVelocity.x = Mathf.Clamp(localVelocity.x, -speed.y, speed.y);
                localVelocity.z = Mathf.Clamp(localVelocity.z, -speed.x, speed.x);

                maxAc = new Vector3(speed.y - localVelocity.x, 0, speed.x - localVelocity.z);
                minAc = new Vector3(-speed.y - localVelocity.x, 0, -speed.x - localVelocity.z);

                inputAcceleration = Time.fixedDeltaTime * RATE_OF_AIR_ACCELERATION * new Vector3(input.x * speed.y, 0, input.y * speed.x);
                inputAcceleration.x = Mathf.Clamp(inputAcceleration.x, minAc.x, maxAc.x);
                inputAcceleration.z = Mathf.Clamp(inputAcceleration.z, minAc.z, maxAc.z);

                inputAcceleration = moveRotation * inputAcceleration;
                _playerRetainedVelocity += inputAcceleration;
                _playerRetainedVelocity.y += Time.fixedDeltaTime * Physics.gravity.y;
            }
            calcPlateInfluence();
            _playerRetainedVelocity += plateInfluence;
            if (character.isGrounded)//only move downward if we're actually grounded and no in coyote time
            {
                _playerRetainedVelocity += STICK_TO_GROUND_FORCE;
            }

            character.Move(_playerRetainedVelocity * Time.fixedDeltaTime);
            HandleRotation();
            RecordTransforms();
            if (Utilities.IsValid(attachment.parentTransform))
            {
                lastGlobalPos = attachment.parentTransform.position + attachment.parentTransform.rotation * localPlayer._localPosition;
                lastGlobalRot = attachment.parentTransform.rotation * localPlayer._localRotation;
            }
            else
            {
                lastGlobalPos = transform.position;
                lastGlobalRot = transform.rotation;
            }
        }

        // public void LateUpdate()
        // {
        //     if (!moved && Utilities.IsValid(attachment) && Utilities.IsValid(attachment.parentTransform))// && !localPlayer.needSync)
        //     {
        //         transform.position = attachment.transform.position;
        //     }
        // }

        Vector3 lastGlobalPos;
        Quaternion lastGlobalRot;
        bool justLanded;
        public void calcPlateInfluence()
        {
            if (!Utilities.IsValid(attachment.parentTransform) || !character.isGrounded || lastJump)
            {
                plateInfluence = Vector3.zero;
                justLanded = true;
            }
            else if (!justLanded)
            {
                transform.rotation *= Quaternion.Inverse(lastGlobalRot) * (attachment.parentTransform.rotation * localPlayer._localRotation);
                plateInfluence = (attachment.parentTransform.position + (attachment.parentTransform.rotation * localPlayer._localPosition) - lastGlobalPos) / Time.fixedDeltaTime;
                // if (moved)
                // {
                // } else
                // {
                //     plateInfluence = (attachment.transform.position - lastGlobalPos);
                // }
                // plateInfluence = lastGlobalPos;
            }
            else
            {
                landedChanged = true;
                justLanded = false;
                plateInfluence = Vector3.zero;
            }
        }

        public void HandleRotation()
        {
            if (localPlayerAPI.IsUserInVR())
            {
                if (snapTurn)
                {
                    if (lookH < -0.5f)
                    {
                        transform.Rotate(Vector3.up * 30f);
                    }
                    else if (lookH > 0.5f)
                    {
                        transform.Rotate(Vector3.up * -30f);
                    }
                    lookH = 0;
                }
                else
                {
                    transform.Rotate(Vector3.up * lookH * Time.fixedDeltaTime);
                }
            }
            else
            {
                transform.Rotate(Vector3.up * Mathf.Lerp(0, lookH * Time.fixedDeltaTime, (Vector3.Angle(playerForward, transform.rotation * Vector3.forward) - 75) / 15));
            }
            transform.rotation = Quaternion.FromToRotation(transform.rotation * Vector3.up, Vector3.up) * transform.rotation;
        }

        Vector3 oldPos;
        Vector3 oldVel;
        bool rotChanged = false;
        bool velChanged = false;
        bool posChanged = false;
        [System.NonSerialized]
        public bool landedChanged = false;
        public void RecordTransforms()
        {
            // Physics.SyncTransforms();
            oldPos = localPlayer._localPosition;
            oldVel = localPlayer._velocity;
            if (Utilities.IsValid(attachment.parentTransform))
            {
                localPlayer.localPosition = Quaternion.Inverse(attachment.parentTransform.rotation) * (transform.position - attachment.parentTransform.position);
                localPlayer.localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
                localPlayer.velocity = Quaternion.Inverse(attachment.parentTransform.rotation) * ((oldPos - localPlayer._localPosition) / Time.deltaTime);
            } else
            {
                localPlayer.localPosition = transform.position;
                localPlayer.localRotation = transform.rotation;
                localPlayer.velocity = character.velocity;
            }
            rotChanged = Vector3.Angle(localPlayer._syncedRot * Vector3.forward, localPlayer._localRotation * Vector3.forward) > 10f;//arbitrarily picked limit
            velChanged = (localPlayer._syncedVel.y >= 0 && oldVel.y < -0.01f) || Vector3.Distance(localPlayer._syncedVel, oldVel) > 0.1f;//arbitrarily picked limit
            posChanged = Vector3.Distance(localPlayer._syncedPos, localPlayer._syncedPos + localPlayer._syncedVel * (Time.timeSinceLevelLoad - localPlayer.syncTime)) > 0.1f;//arbitrarily picked limit
            localPlayer.needSync = localPlayer.needSync || landedChanged || rotChanged || velChanged || posChanged;
        }

        TectonicPlate hitPlate;

        public void OnTriggerEnter(Collider hit)
        {
            if (!Utilities.IsValid(hit) || !Utilities.IsValid(localPlayer))
            {
                return;
            }
            hitPlate = hit.GetComponent<TectonicPlate>();
            if (!Utilities.IsValid(hitPlate))
            {
                return;
            }
            localPlayer.attachment.parentTransformName = hitPlate.transformName;
        }


        private Vector2 GetSpeed()
        {
            // TODO check current bindings to see if non keyboard and only use runspeed.
            Vector2 speed = new Vector2(
                localPlayerAPI.IsUserInVR() || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runSpeed : walkSpeed,
                strafeSpeed);

            return speed;
        }
    }
}
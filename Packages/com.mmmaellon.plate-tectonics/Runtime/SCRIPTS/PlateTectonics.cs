
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

        public bool forceUpright = true;
        VRCPlayerApi localPlayerAPI;
        public void Start()
        {
            localPlayerAPI = Networking.LocalPlayer;
        }

        public void AttachToStartingPlate()
        {
            if (Utilities.IsValid(startingPlate))
            {
                localPlayer.attachment.parentTransformName = startingPlate.transformName;
            }
        }

        bool respawn = true;
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal && Utilities.IsValid(localPlayer))
            {
                respawn = true;
                // _playerRetainedVelocity = Vector3.zero;
                // transform.position = localPlayerAPI.GetPosition();
                // transform.rotation = localPlayerAPI.GetRotation();
                // AttachToStartingPlate();
                SendCustomEventDelayedFrames(nameof(SitInChairDelayed), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            }
        }
        public void SitInChairDelayed()
        {
            AttachToStartingPlate();
            transform.position = localPlayerAPI.GetPosition();
            transform.rotation = localPlayerAPI.GetRotation();
            _playerRetainedVelocity = Vector3.zero;
            SendCustomEventDelayedFrames(nameof(SitInChair), 1, VRC.Udon.Common.Enums.EventTiming.Update);
        }
        public void SitInChair()
        {
            AttachToStartingPlate();
            transform.position = localPlayerAPI.GetPosition();
            transform.rotation = localPlayerAPI.GetRotation();
            _playerRetainedVelocity = Vector3.zero;
            localPlayer.chair.UseStation(localPlayerAPI);
            respawn = false;
        }

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


                runSpeed = localPlayerAPI.GetRunSpeed();
                walkSpeed = localPlayerAPI.GetWalkSpeed();
                strafeSpeed = localPlayerAPI.GetStrafeSpeed();
                jumpSpeed = localPlayerAPI.GetJumpImpulse();
                localPlayerAPI.SetRunSpeed(0);
                localPlayerAPI.SetWalkSpeed(0);
                localPlayerAPI.SetStrafeSpeed(0);
                localPlayerAPI.SetJumpImpulse(0);

                localPlayer.transform.SetParent(transform, false);
                localPlayer.transform.localPosition = Vector3.zero;
                localPlayer.transform.localRotation = Quaternion.identity;
                // localPlayer.SitInChairDelayed();
                // localPlayer.SendCustomEventDelayedSeconds("SitInChairDelayed", 2);//delay to hopefully fix the bug that happens when you get into the chair before your avatar loads
                SendCustomEventDelayedSeconds(nameof(SitInChair), 1, VRC.Udon.Common.Enums.EventTiming.Update);//1 second delay is arbitrarily picked but it helps fix bugs

                attachment = localPlayer.attachment;
                AttachToStartingPlate();
            }
        }

        public override void _OnPlayerUnassigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject)
        {

        }

        bool jump;
        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            jump = value;
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
        }

        Vector3 plateInfluence;
        Vector2 input;
        Vector3 moveInfluence;
        Vector3 playerForward;
        Quaternion moveRotation;

        Vector3 _playerRetainedVelocity;
        Vector3 STICK_TO_GROUND_FORCE = new Vector3(0, -2f, 0);
        private const float RATE_OF_AIR_ACCELERATION = 5f;
        Vector2 speed;
        Vector3 localVelocity;
        Vector3 maxAc;
        Vector3 minAc;
        Vector3 inputAcceleration;
        bool coyoteGrounded = false;
        float coyoteTime = 0.1f;
        float lastGrounded = 0;
        Transform lastTransform;
        bool parentTransformChanged;
        bool freeMovement;
        public void FixedUpdate()
        {
            //Stolen from client sim
            if (!Utilities.IsValid(localPlayer) || respawn)
            {
                return;
            }
            Physics.SyncTransforms();
            speed = GetSpeed();
            parentTransformChanged = lastTransform != attachment.parentTransform;

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

            //calc coyote time
            if (character.isGrounded)
            {
                lastGrounded = Time.timeSinceLevelLoad;
            }
            coyoteGrounded = (character.isGrounded || (coyoteGrounded && (lastGrounded + coyoteTime > Time.timeSinceLevelLoad) && !jump)) && !parentTransformChanged;

            if (coyoteGrounded && !parentTransformChanged)
            {
                calcPlateInfluence();
                _playerRetainedVelocity = moveInfluence + plateInfluence;
                if (jump)
                {
                    _playerRetainedVelocity.y += jumpSpeed;
                }
                else if(character.isGrounded)//ignore coyote time and only stick if actually grounded
                {
                    _playerRetainedVelocity += STICK_TO_GROUND_FORCE;
                }
            }
            else
            {
                plateInfluence = Vector3.zero;
                // Slowly add velocity from movement inputs
                localVelocity = Quaternion.Inverse(moveRotation) * character.velocity;
                localVelocity.x = Mathf.Clamp(localVelocity.x, -speed.y, speed.y);
                localVelocity.z = Mathf.Clamp(localVelocity.z, -speed.x, speed.x);

                maxAc = new Vector3(speed.y - localVelocity.x, 0, speed.x - localVelocity.z);
                minAc = new Vector3(-speed.y - localVelocity.x, 0, -speed.x - localVelocity.z);

                inputAcceleration = Time.fixedDeltaTime * RATE_OF_AIR_ACCELERATION * new Vector3(input.x * speed.y, 0, input.y * speed.x);
                inputAcceleration.x = Mathf.Clamp(inputAcceleration.x, minAc.x, maxAc.x);
                inputAcceleration.z = Mathf.Clamp(inputAcceleration.z, minAc.z, maxAc.z);

                _playerRetainedVelocity += moveRotation * inputAcceleration + Time.fixedDeltaTime * Physics.gravity;
            }
            freeMovement = !coyoteGrounded || moveInfluence.magnitude > 0 || jump || parentTransformChanged || Vector3.Distance(localPlayer._syncedPos, localPlayer.localPosition) > 0.1f;
            if (freeMovement)
            {
                character.Move(_playerRetainedVelocity * Time.fixedDeltaTime);
                localPlayer.endTransform.position = transform.position;
            }
            else
            {
                // transform.position = localPlayer.endTransform.position;
                // character.Move(STICK_TO_GROUND_FORCE * Time.fixedDeltaTime);
                character.Move(_playerRetainedVelocity * Time.fixedDeltaTime);
                if (Vector3.Distance(localPlayer.endTransform.position, transform.position) > 0.001f)//stops random slipping
                {
                    localPlayer.endTransform.position = transform.position;
                }
            }
            HandleRotation();
            RecordTransforms();
            lastGlobalPos = localPlayer.endTransform.position;
            lastGlobalRot = localPlayer.endTransform.rotation;
            // jump = false;
            lastTransform = attachment.parentTransform;
        }

        Vector3 lastGlobalPos;
        Quaternion lastGlobalRot;
        bool justLanded;
        public void calcPlateInfluence()
        {
            if (!Utilities.IsValid(attachment.parentTransform) || !coyoteGrounded || jump)
            {
                plateInfluence = Vector3.zero;
                justLanded = true;
            }
            else if (!justLanded)
            {
                // transform.rotation *= Quaternion.Inverse(lastGlobalRot) * (attachment.parentTransform.rotation * localPlayer._localRotation);
                // plateInfluence = (attachment.parentTransform.position + (attachment.parentTransform.rotation * localPlayer._localPosition) - lastGlobalPos) / Time.fixedDeltaTime;
                transform.rotation *= Quaternion.Inverse(lastGlobalRot) * localPlayer.endTransform.rotation;
                plateInfluence = (localPlayer.endTransform.position - lastGlobalPos) / Time.fixedDeltaTime;
            }
            else
            {
                justLanded = false;
                plateInfluence = Vector3.zero;
            }
        }
        float dif;
        float inputLookX;

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
                // transform.Rotate(Vector3.up * Mathf.Lerp(0, lookH * Time.fixedDeltaTime, (Vector3.Angle(playerForward, transform.rotation * Vector3.forward) - 75) / 15));

                //Code shared by Centauri
                dif = Vector3.Dot(playerForward.normalized, transform.right);
                inputLookX = Input.GetAxisRaw("Mouse X");

                if (Mathf.Abs(dif) >= 0.89f && Mathf.Sign(dif) == Mathf.Sign(inputLookX))
                {
                    transform.localEulerAngles += 1.35f * inputLookX * Vector3.up;
                }
            }
            if (forceUpright)
            {
                transform.rotation = Quaternion.FromToRotation(transform.rotation * Vector3.up, Vector3.up) * transform.rotation;
            }
            localPlayer.endTransform.rotation = transform.rotation;
        }

        Vector3 oldPos;
        Vector3 oldVel;
        bool rotChanged = false;
        bool velChanged = false;
        bool posChanged = false;
        public void RecordTransforms()
        {
            // Physics.SyncTransforms();
            oldPos = localPlayer._localPosition;
            oldVel = localPlayer._velocity;
            localPlayer.localPosition = localPlayer.endTransform.localPosition;
            localPlayer.localRotation = localPlayer.endTransform.localRotation;
            if (Utilities.IsValid(attachment.parentTransform))
            {
                if (!parentTransformChanged)
                {
                    localPlayer.velocity = (localPlayer._localPosition - oldPos) / Time.fixedDeltaTime;
                }
                else
                {
                    localPlayer.velocity = (localPlayer._localPosition - ((Quaternion.Inverse(attachment.parentTransform.rotation) * lastGlobalPos) - attachment.parentTransform.position)) / Time.fixedDeltaTime;
                }
            } else
            {
                localPlayer.velocity = character.velocity;
            }
            localPlayer.needSync = localPlayer.needSync || Vector3.Angle(localPlayer._syncedRot * Vector3.forward, localPlayer._localRotation * Vector3.forward) > 10f;                                             //check rotation
            localPlayer.needSync = localPlayer.needSync || (localPlayer._syncedVel.y >= 0 && oldVel.y < -0.01f) || Vector3.Distance(localPlayer._syncedVel, oldVel) > 0.1f;                                         //check velocity
            localPlayer.needSync = localPlayer.needSync || Vector3.Distance(localPlayer._localPosition, localPlayer._syncedPos + localPlayer._syncedVel * (Time.timeSinceLevelLoad - localPlayer.syncTime)) > 0.1f; //check position

            // Debug.LogWarning("pos changed " + Vector3.Distance(localPlayer._localPosition, localPlayer._syncedPos + localPlayer._syncedVel * (Time.timeSinceLevelLoad - localPlayer.syncTime)));
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
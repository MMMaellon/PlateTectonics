
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
        // public void Update()
        // {
        //     if (!plateActive || !Utilities.IsValid(localPlayer) || !Utilities.IsValid(localPlayer.parentTransform))
        //     {
        //         return;
        //     }
        //     if (localPlayerAPI.IsPlayerGrounded())
        //     {
        //         rotationalDiff = Quaternion.Inverse(localPlayer.parentTransform.rotation) * plateLockRotation;
        //         transform.rotation = transform.rotation * localPlayer.parentTransform.localRotation * rotationalDiff * Quaternion.Inverse(localPlayer.parentTransform.localRotation);
        //     }
        //     else
        //     {
        //         plateLockRotation = localPlayer.parentTransform.rotation;
        //     }
        //     transform.position += plateLockPosition - localPlayer.parentTransform.position;
        // }
        // public void FixedUpdate()
        // {
        //     if (!Utilities.IsValid(localPlayer))
        //     {
        //         playerVelocity = Vector3.zero;
        //         return;
        //     }
        //     // if (!plateActive || !Utilities.IsValid(localPlayer) || !Utilities.IsValid(attachment.parentTransform))
        //     // {
        //     //     // plateVel = Vector3.zero;
        //     //     return;
        //     // }
        //     // if (character.isGrounded)
        //     // {
        //     //     // transform.rotation = attachment.parentTransform.rotation * localPlayer.localRotation;
        //     //     playerVelocity = attachment.parentTransform.TransformPoint(localPlayer.localPosition) - localPlayer.transform.position;
        //     //     // transform.rotation = transform.rotation * localPlayer.parentTransform.localRotation * rotationalDiff * Quaternion.Inverse(localPlayer.parentTransform.localRotation);
        //     //     // localPlayerAPI.setrot
        //     //     // localPlayerAPI.SetVelocity(localPlayerAPI.GetVelocity() + plateVel);
        //     // }
        //     if (Utilities.IsValid(attachment.parentTransform))
        //     {
        //         transform.rotation = attachment.parentTransform.rotation * localPlayer._localRotation;
        //         // transform.position = attachment.parentTransform.TransformPoint(localPlayer._localPosition);
        //         // character.Move(attachment.parentTransform.TransformPoint(localPlayer._localPosition) - transform.position);
        //         // playerVelocity.x = playerDiff.x;
        //         // playerVelocity.z = playerDiff.z;
        //         // playerVelocity.y = Mathf.Max(playerDiff.y, playerVelocity.y);
        //     }
        // }

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
        private const float STICK_TO_GROUND_FORCE = 2f;
        private const float RATE_OF_AIR_ACCELERATION = 5f;
        Vector2 speed;
        Vector3 localVelocity;
        Vector3 maxAc;
        Vector3 minAc;
        Vector3 inputAcceleration;
        [System.NonSerialized]
        public bool moved = false;
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
            moveInfluence.y = 0;
            moved = moveInfluence.magnitude > 0;

            float gravityContribution = Time.fixedDeltaTime * Physics.gravity.y;

            if (character.isGrounded)
            {
                _playerRetainedVelocity = Vector3.zero;
                _playerRetainedVelocity.y = -STICK_TO_GROUND_FORCE;
                if (lastJump)
                {
                    _playerRetainedVelocity = moveInfluence;
                    _playerRetainedVelocity.y = jumpSpeed;
                    moveInfluence = Vector3.zero;
                    lastJump = false;
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
                moveInfluence = Vector3.zero;
                _playerRetainedVelocity.y += gravityContribution;
            }
            calcPlateInfluence();
            _playerRetainedVelocity += plateInfluence;
            moveInfluence += _playerRetainedVelocity;

            character.Move(moveInfluence * Time.fixedDeltaTime);
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

        // public void Update()
        // {
        //     if (!Utilities.IsValid(localPlayer))
        //     {
        //         return;
        //     }
        //     RecordTransforms();
        // }
        // public void FixedUpdate()
        // {
        //     if (!Utilities.IsValid(localPlayer))
        //     {
        //         return;
        //     }
        //     Physics.SyncTransforms();
        //     playerForward = localPlayerAPI.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
        //     playerForward.y = 0;
        //     if (playerForward.magnitude <= 0)
        //     {
        //         playerForward = transform.rotation * Vector3.forward;
        //     }
        //     moveRotation = Quaternion.FromToRotation(Vector3.forward, playerForward);
        //     // Physics.SyncTransforms();
        //     calcPlateInfluence();
        //     calcInputInfluence();
        //     character.Move((plateInfluence + moveInfluence) * Time.deltaTime);

        //     transform.rotation = Quaternion.FromToRotation(transform.rotation * Vector3.up, Vector3.up) * transform.rotation;
        //     localPlayer.transform.position = transform.position;
        //     localPlayer.transform.rotation = transform.rotation;
        //     RecordTransforms();
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
            }
            else
            {
                landedChanged = true;
                justLanded = false;
                plateInfluence = Vector3.zero;
            }
        }
        // float lateralVel;
        // public void calcInputInfluence()
        // {
        //     lastY = moveInfluence.y;
        //     if (character.isGrounded && !lastJump)
        //     {
        //         moveInfluence = moveRotation * Vector3.right * input.x * strafeSpeed;
        //         if (localPlayerAPI.IsUserInVR() || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        //         {
        //             moveInfluence += moveRotation * Vector3.forward * input.y * runSpeed;
        //         }
        //         else
        //         {
        //             moveInfluence += moveRotation * Vector3.forward * input.y * walkSpeed;
        //         }
        //     }
        //     else
        //     {
        //         moveInfluence += moveRotation * Vector3.right * input.x * strafeSpeed * Time.deltaTime * 4;
        //         if (localPlayerAPI.IsUserInVR() || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        //         {
        //             moveInfluence += moveRotation * Vector3.forward * input.y * runSpeed * Time.deltaTime * 4;
        //         }
        //         else
        //         {
        //             moveInfluence += moveRotation * Vector3.forward * input.y * walkSpeed * Time.deltaTime * 4;
        //         }
        //         lateralVel = new Vector2(moveInfluence.x, moveInfluence.z).magnitude;
        //         if (lateralVel > runSpeed)
        //         {
        //             moveInfluence = Vector3.Lerp(moveInfluence, moveInfluence * runSpeed / lateralVel, 0.1f);
        //             moveInfluence.y = lastY;
        //         }
        //     }
        //     if (lastJump)
        //     {
        //         if (character.isGrounded)
        //         {
        //             moveInfluence.y = Mathf.Sqrt(jumpSpeed * -gravity.y);
        //         }
        //         lastJump = false;
        //     }
        //     else
        //     {
        //         moveInfluence += gravity * Time.deltaTime;
        //     }
        // }

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
        bool syncTime;
        bool rotChanged = false;
        bool velChanged = false;
        [System.NonSerialized]
        public bool landedChanged = false;
        public void RecordTransforms()
        {
            // Physics.SyncTransforms();
            oldPos = localPlayer._localPosition;
            oldVel = localPlayer._velocity;
            if (Utilities.IsValid(attachment.parentTransform))
            {
                if (landedChanged)
                {
                    localPlayer.velocity = Vector3.zero;
                    localPlayer.localPosition = Quaternion.Inverse(attachment.parentTransform.rotation) * (transform.position - attachment.parentTransform.position);
                } else if (moved)
                {
                    localPlayer.localPosition = Quaternion.Inverse(attachment.parentTransform.rotation) * (transform.position - attachment.parentTransform.position);
                    localPlayer.velocity = oldPos - localPlayer._localPosition / Time.deltaTime;
                } else
                {
                    localPlayer.velocity = Vector3.zero;
                }
                localPlayer.localRotation = Quaternion.Inverse(attachment.parentTransform.rotation) * transform.rotation;
            } else
            {
                localPlayer.localPosition = transform.position;
                localPlayer.localRotation = transform.rotation;
                localPlayer.velocity = character.velocity;
            }
            syncTime = localPlayer.syncTime + localPlayer.networkUpdateInterval < Time.timeSinceLevelLoad;
            rotChanged = Vector3.Angle(localPlayer._lastSyncedRot * Vector3.forward, localPlayer._localRotation * Vector3.forward) > 10f;//arbitrarily picked limit
            velChanged = (localPlayer._velocity == Vector3.zero && oldVel != Vector3.zero) || Vector3.Distance(localPlayer._velocity, oldVel) > 0.1f;//arbitrarily picked limit
            if (landedChanged || (syncTime && (rotChanged || velChanged)))
            {
                localPlayer.RequestSerialization();
            }
        }

        TectonicPlate hitPlate;
        // void OnControllerColliderHit(ControllerColliderHit hit)
        // {
        //     Debug.LogWarning("CCCCCCCCCCCCCCC");
        //     if (!Utilities.IsValid(hit) || !Utilities.IsValid(hit.gameObject))
        //     {
        //         return;
        //     }
        //     hitPlate = hit.gameObject.GetComponent<TectonicPlate>();
        //     if (!Utilities.IsValid(hitPlate))
        //     {
        //         return;
        //     }

        //     hitPlate.Enter();
        // }

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
        // public void OnTriggerExit(Collider hit)
        // {
        //     if (!Utilities.IsValid(hit))
        //     {
        //         return;
        //     }
        //     triggerGrounded = false;
        //     hitPlate = hit.GetComponent<TectonicPlate>();
        //     if (!Utilities.IsValid(hitPlate))
        //     {
        //         return;
        //     }

        //     Debug.LogWarning("Exit");
        //     hitPlate.Exit();
        // }


        private Vector2 GetSpeed()
        {
            // TODO check current bindings to see if non keyboard and only use runspeed.
            Vector2 speed = new Vector2(
                localPlayerAPI.IsUserInVR() || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runSpeed : walkSpeed,
                strafeSpeed);

            // switch (localPlayerAPI.cro)
            // {
            //     case ClientSimPlayerStanceEnum.CROUCHING:
            //         speed *= CROUCH_SPEED_MULTIPLIER;
            //         break;
            //     case ClientSimPlayerStanceEnum.PRONE:
            //         speed *= PRONE_SPEED_MULTIPLIER;
            //         break;
            // }

            return speed;
        }
    }
}
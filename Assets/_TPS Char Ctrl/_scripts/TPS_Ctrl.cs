using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sinn.TPS_CTRL
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    public class TPS_Ctrl : MonoBehaviour
    {
        Rigidbody RB;
        Animator anim;
        CapsuleCollider capCol;

        float original_capsule_height;
        Vector3 original_capsule_center;
        bool isCrouching;

        public float groundCheckDist = 0.3f;
        public float strt_groundCheckDist;
        bool isGrounded;
        Vector3 groundNormal;
        

        float turn_amount;
        float forward_amount;
        public float jumpForce = 12f;
        public float stationaryTurnSpeed = 180;
        public float movingTurnSpeed = 360;


        public float animSpeedMultiplier = 1f;
        public float moveSpeedMultiplier = 1f;
        [Range(1f, 4f)] public float gravityMultiplier = 2f;

        const float k_Half = 0.5f;
        public float runCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others

        void Start()
        {
            anim = GetComponent<Animator>();
            RB = GetComponent<Rigidbody>();
            capCol = GetComponent<CapsuleCollider>();

            original_capsule_height = capCol.height;
            original_capsule_center = capCol.center;

            RB.constraints = RigidbodyConstraints.FreezeRotation;

            strt_groundCheckDist = groundCheckDist;
        }

        public void Move(Vector3 _move, bool crouch, bool jump, bool isAimmed)
        {
            /* ...........convert the world relative moveInput vector into a local-relative
             * ...........turn amount and forward amount required to head in the desired direction...*/
            if (_move.magnitude > 1) _move.Normalize();
            _move = transform.InverseTransformDirection(_move); // [JEIDIKE FIRE THAKBE OIDIKE MOVE KORBE]
            CheckGroundStatus();

            _move = Vector3.ProjectOnPlane(_move, groundNormal); // # [WHAT THE HELL IS THIS LINE]<<<<<<<<<<<<>>>>>>>>>>>>

            if (!isAimmed)
                turn_amount = Mathf.Atan2(_move.x, _move.z);
            else
                turn_amount = _move.x;
    
            forward_amount = _move.z;



            if (isGrounded) HandleGroundedMovement(crouch, jump); // [RESPOSIBLE FOR JUMP]
            else            HandleAirborneMovement();             // [RESPOSIBLE FOR EXTRA GRAVITY]

            ApplyExtraTurnRotation(isAimmed); // [- WITHOUT THIS PLAYER WILL TURN SLOWLY BECAUSE OF ANIM TRANSITION]
            ScaleCapsuleForCrouching(crouch);
            PreventStandingInLowHeadroom();

            UpdateAnimation(_move);
        }
        void UpdateAnimation(Vector3 _move)
        {
            anim.SetFloat("forward", forward_amount, 0.1f, Time.deltaTime);
            anim.SetFloat("turn", turn_amount, 0.1f, Time.deltaTime);
            anim.SetBool("crouch", isCrouching);
            anim.SetBool("onGround", isGrounded);
            if(!isGrounded) 
                anim.SetFloat("velocityY", RB.velocity.y);


            /* calculate which leg is behind, so as to leave that leg trailing in the jump animation
             * (This code is reliant on the specific run cycle offset in our animations,
             * and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5) */
            float runCycle = Mathf.Repeat(anim.GetCurrentAnimatorStateInfo(0).normalizedTime + runCycleLegOffset, 1);
            float jumpLeg = (runCycle < 0.5f ? 1 : -1) * forward_amount; 
            if (isGrounded)
                anim.SetFloat("jumpLeg", jumpLeg);


            /* ........allows the overall speed of walking/running to be tweaked in inspector,
             * ........which affects the movement speed because of the root motion. */
            if (isGrounded && _move.magnitude > 0)
                anim.speed = animSpeedMultiplier;
            else
                anim.speed = 1;// don't use that while airborne
        }
        void ScaleCapsuleForCrouching(bool crouch)
        {
            if(isGrounded && crouch)
            {
                if (isCrouching) return;
                capCol.height /= 2f;
                capCol.center /= 2f;
                isCrouching = true;
            }
            else // [PREVENTS PLAYER TO STAND UP WHEN IN THE CROUCH ZONE]
            {
                Ray crouchRay = new Ray(RB.position + Vector3.up * capCol.radius * k_Half, Vector3.up);
                float crouchRayLength = original_capsule_height - capCol.radius * k_Half;
                if(Physics.SphereCast(crouchRay, capCol.radius * 0.5f, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    isCrouching = true; // NOT PRESSING "c" & in crouch zone. this will prevent from standing up
                    return;
                }

                capCol.height = original_capsule_height;
                capCol.center = original_capsule_center;
                isCrouching = false;
            }
        }
        void PreventStandingInLowHeadroom() // [-KI KORLAM BUJHSI, KEN KORLAM BUJHI NAI-]<<<<<<<<<<<<>>>>>>>>>>>>
        {
            // prevent standing up in crouch-only zones
            if (!isCrouching)
            {
                Ray crouchRay = new Ray(RB.position + Vector3.up * capCol.radius * k_Half, Vector3.up);
                float crouchRayLength = original_capsule_height - capCol.radius * k_Half;
                if (Physics.SphereCast(crouchRay, capCol.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    isCrouching = true;
                }
            }
        }
        void HandleAirborneMovement()
        {
            // apply extra gravity from multiplier:
            Vector3 extraGravity = (Physics.gravity * gravityMultiplier) - Physics.gravity; 
            RB.AddForce(extraGravity);      // {0, (-9.81f * 2), 0} - 9.81f

            groundCheckDist = RB.velocity.y < 0 ? strt_groundCheckDist : 0.01f; 
                // if velocity < 0( meaning not going up, going down) set groundCheckDistance to original groundCheckDistance
        }
        void HandleGroundedMovement(bool crouch, bool jump)
        {
            // check whether conditions are right to allow a jump:
            if (jump && !crouch && anim.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                RB.velocity = new Vector3(RB.velocity.x, jumpForce, RB.velocity.z);
                isGrounded = false;
                anim.applyRootMotion = false;
                groundCheckDist = 0.01f;
            }
        }
        void ApplyExtraTurnRotation(bool aimmed) // [-KEN KORLAM BUJHSI, KI KORLAM BUJHI NAI-]????????????????????????????
        {
            if (aimmed) return;
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(stationaryTurnSpeed, movingTurnSpeed, forward_amount);
            transform.Rotate(0, turn_amount * turnSpeed * Time.deltaTime, 0);
        }
        void OnAnimatorMove()   // [-BUJHI NAI-]???????????????????????????????????????????????????????????????
        {
            /* we implement this function to override the default root motion.
             * this allows us to modify the positional speed before it's applied. */
            if (isGrounded && Time.deltaTime > 0)
            {
                Vector3 v = (anim.deltaPosition * moveSpeedMultiplier) / Time.deltaTime;
                // .......we preserve the existing y part of the current velocity.
                v.y = RB.velocity.y;
                RB.velocity = v;
            }
        }
        void CheckGroundStatus()
        {
            Ray ray = new Ray(transform.position + (Vector3.up * 0.1f), Vector3.down); // 0.1f is just an offset to start ray from a bit above the player
            RaycastHit hitInfo;

            Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * groundCheckDist), Color.red);

            if (Physics.Raycast(ray, out hitInfo, groundCheckDist))
            {
                groundNormal = hitInfo.normal;  // its a dir that makes 90" with the surface
                isGrounded = true;
                anim.applyRootMotion = true;
            }
            else
            {
                groundNormal = Vector3.up;
                isGrounded = false;
                anim.applyRootMotion = false;
            }
        }
    }
}

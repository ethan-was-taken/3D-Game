using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonCharacter : MonoBehaviour {

        [SerializeField] float m_MovingTurnSpeed = 360;
        [SerializeField] float m_StationaryTurnSpeed = 360;
        [SerializeField] float m_JumpPower = 9f;
        private float m_DoubleJumpPower;
        [Range(1f, 4f)] [SerializeField] float m_GravityMultiplier = 2f;
        [SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
        [SerializeField] float m_MoveSpeedMultiplier = 1.1f;
        [SerializeField] float m_AnimSpeedMultiplier = 1.1f;
        [SerializeField] float m_GroundCheckDistance = 0.1f;

        Rigidbody m_Rigidbody;
        Animator m_Animator;
        bool m_IsGrounded;
        float m_OrigGroundCheckDistance;
        const float k_Half = 0.5f;
        float m_TurnAmount;
        float m_ForwardAmount;
        Vector3 m_GroundNormal;
        float m_CapsuleHeight;
        Vector3 m_CapsuleCenter;
        CapsuleCollider m_Capsule;
        bool m_Crouching;

        private bool canDoubleJump;
        // Temporary... hopefully
        private float currSpeed = 0;                
        private bool isSliding;

        void Start() {
            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            m_CapsuleHeight = m_Capsule.height;
            m_CapsuleCenter = m_Capsule.center;

            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            m_OrigGroundCheckDistance = m_GroundCheckDistance;

            m_DoubleJumpPower = m_JumpPower / 1.5f;
            canDoubleJump = true;
            isSliding = false;

        }


        public void Move(Vector3 move, bool crouch, bool jump) {

            Vector3 move2 = move;

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            if (move.magnitude > 1f) move.Normalize();
            move = transform.InverseTransformDirection(move);
            CheckGroundStatus(move);
            move = Vector3.ProjectOnPlane(move, m_GroundNormal);
            m_TurnAmount = Mathf.Atan2(move.x, move.z);
            m_ForwardAmount = move.z;

            ApplyExtraTurnRotation();

            currSpeed = m_Rigidbody.velocity.magnitude;

            if (m_IsGrounded) {
                HandleGroundedMovement(crouch, jump);
            }
            else {
                HandleAirborneMovement(jump, move2);
            }

            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }


        void UpdateAnimator(Vector3 move) {

            // update the animator parameters
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            m_Animator.SetBool("Crouch", m_Crouching);
            m_Animator.SetBool("OnGround", m_IsGrounded);
            if (!m_IsGrounded) {
                m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
            }

            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            float runCycle =
                Mathf.Repeat(
                    m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
            float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
            if (m_IsGrounded) {
                m_Animator.SetFloat("JumpLeg", jumpLeg);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (m_IsGrounded && move.magnitude > 0) {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else {
                // don't use that while airborne
                m_Animator.speed = 1;
            }

        }

        void HandleAirborneMovement(bool jump, Vector3 move) {

            // apply extra gravity from multiplier:
            Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
            m_Rigidbody.AddForce(extraGravityForce);

            m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.1f;

            // Allows for in-air movement
            Vector3 temp = new Vector3( move.x * currSpeed, m_Rigidbody.velocity.y, move.z * currSpeed);

            // max speed at the moment is ~5.5
            float modifier = 5.5f;
            // We want the character to slide a little bit faster then the current max speed          
            float ySpeed = isSliding ? (-1.5f * modifier) : m_Rigidbody.velocity.y;

            if (m_Rigidbody.velocity.magnitude > modifier)
                m_Rigidbody.velocity = new Vector3(move.x * modifier, ySpeed, move.z * modifier);
            else
                m_Rigidbody.velocity = temp;

            // For double Jumping
            if (jump && canDoubleJump) {
                m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_DoubleJumpPower, m_Rigidbody.velocity.z);
                m_Animator.applyRootMotion = false;
                m_GroundCheckDistance = 0.1f;
                canDoubleJump = false;
            }

        }

        void HandleGroundedMovement(bool crouch, bool jump) {

            // check whether conditions are right to allow a jump:
            if (jump /*&& !m_Crouching*/ && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded")) {
                // jump!
                m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
                m_IsGrounded = false;
                m_Animator.applyRootMotion = false;
                m_GroundCheckDistance = 0.1f;
            }

        }

        void ApplyExtraTurnRotation() {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
        }

        public void OnAnimatorMove() {
            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (m_IsGrounded && Time.deltaTime > 0) 
            {
                Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

                // we preserve the existing y part of the current velocity.
                v.y = m_Rigidbody.velocity.y;
                m_Rigidbody.velocity = v;
            }
        }

        void CheckGroundStatus(Vector3 move) {

            RaycastHit hitInfo;

            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance) ) {
                
                float surfaceAngle = convertTo180Degrees(hitInfo.transform.eulerAngles.x);

                if (Mathf.Abs(surfaceAngle) > 40) 
                {
                    slide(hitInfo, move);
                }
                else 
                {
                    m_Animator.applyRootMotion = true;
                    m_GroundNormal = hitInfo.normal;
                    m_IsGrounded = true;
                    canDoubleJump = true;
                    isSliding = false;
                }

            }
            else 
            {
                m_IsGrounded = false;
                m_GroundNormal = Vector3.up;
                m_Animator.applyRootMotion = false;
            }
        }

        // Because I'm too dumb to use 360 deg angles
        float convertTo180Degrees(float angle) {
            if(angle > 180)
                return -(360 - angle);
            return angle;
        }

        void slide(RaycastHit hitInfo, Vector3 move) {

            float modifier = 6;

            // If the player slides at a rate faster than the designated speed, stop accelerating
            // What this boils down to is: if the character is sliding, pretend we're in the air
            if (m_Rigidbody.velocity.magnitude > modifier)
                HandleAirborneMovement(false, move);
            else 
                m_Rigidbody.velocity = new Vector3(move.x * modifier, -modifier, move.z * modifier);

            m_IsGrounded = false;
            canDoubleJump = false;
            m_Animator.applyRootMotion = false;
            isSliding = true;

        }

        void OnCollisionEnter(Collision coll) {
            if (coll.transform.tag == "MovingPlatform")
                this.transform.parent = coll.transform;            
        }

        void OnCollisionExit(Collision coll) {
            if (coll.transform.tag == "MovingPlatform")
                this.transform.parent = null;
        }

    }
}

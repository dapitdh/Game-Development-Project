using UnityEngine;
using UnityEngine.InputSystem;

namespace FPP
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Puan_control : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 6f;
        public float crouchSpeed = 1.5f;
        [Tooltip("Seberapa cepat mengubah velocity horizontal ke target")]
        public float accel = 40f;
        public float airControl = 0.4f;

        [Header("Jump & Gravity")]
        public float jumpForce = 5f;
        public float extraGravity = 20f;
        public float groundStick = 10f;
        public float postJumpGroundIgnore = 0.12f;

        [Header("Crouch (Collider)")]
        public float standingHeight = 1.8f;
        public float crouchingHeight = 1.2f;

        [Header("Ground (GroundCheck GO ONLY)")]
        public Transform groundCheck; // drag GameObject kaki ke sini

        // ===== Animator params =====
        [Header("Animation (Animator Bools)")]
        public Animator anim;
        public string walkParam = "walk";
        public string runParam = "Run";
        public string jumpParam = "jump";
        public string strafeLParam = "strafeL";
        public string strafeLWalkParam = "strafeLWalk";
        public string strafeRParam = "strafeR";
        public string strafeRWalkParam = "strafeRWalk";
        public string turnLParam = "turnL";
        public string turnRParam = "turnR";

        [Header("Step Climb")]
        [SerializeField] public GameObject stepRayLower;
        [SerializeField] public GameObject stepRayUpper;
        [SerializeField] float stepRayHeight = 0.3f;
        [SerializeField] float stepSmooth = 0.1f;

        int walkHash, runHash, jumpHash, strafeLHash, strafeLWalkHash, strafeRHash, strafeRWalkHash, turnLHash, turnRHash;

        // State publik
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector2 MoveInput { get; private set; }

        Rigidbody rb;
        CapsuleCollider capsule;

        // lompat/ground
        bool jumpQueued;
        bool hasLandedSinceLastJump = true;
        bool wasGrounded;
        float postJumpIgnoreTimer;

        // untuk turn anim
        float mouseXRaw;
        public float turnMouseThreshold = 0.2f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            capsule = GetComponent<CapsuleCollider>();
            capsule.height = standingHeight;
            capsule.center = new Vector3(0f, standingHeight * 0.5f, 0f);

            if (!anim) anim = GetComponent<Animator>();
            if (anim)
            {
                walkHash = Animator.StringToHash(walkParam);
                runHash = Animator.StringToHash(runParam);
                jumpHash = Animator.StringToHash(jumpParam);
                strafeLHash = Animator.StringToHash(strafeLParam);
                strafeLWalkHash = Animator.StringToHash(strafeLWalkParam);
                strafeRHash = Animator.StringToHash(strafeRParam);
                strafeRWalkHash = Animator.StringToHash(strafeRWalkParam);
                turnLHash = Animator.StringToHash(turnLParam);
                turnRHash = Animator.StringToHash(turnRParam);

                anim.applyRootMotion = false;
                anim.updateMode = AnimatorUpdateMode.Normal;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.speed = 1f;
            }

            rb.useGravity = false; // gravity manual

            // (biarkan sesuai permintaanmu, tidak diubah)
            stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepRayHeight, stepRayUpper.transform.position.z);

            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            ReadInputs();

            // crouch (hold)
            IsCrouching = IsCrouchHeld();
            float targetH = IsCrouching ? crouchingHeight : standingHeight;
            capsule.height = Mathf.Lerp(capsule.height, targetH, Time.deltaTime * 12f);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            if (JumpPressed())
                jumpQueued = true;

            // simpan mouse X untuk anim turn
            mouseXRaw = Input.GetAxis("Mouse X");
        }

        void FixedUpdate()
        {
            if (postJumpIgnoreTimer > 0f)
                postJumpIgnoreTimer -= Time.fixedDeltaTime;

            bool prevGrounded = IsGrounded;

            // Grounding: hanya dari GroundCheckGO (tanpa kontak)
            bool groundByCast = GroundCheckGO();
            IsGrounded = (postJumpIgnoreTimer <= 0f) && groundByCast;

            if (IsGrounded && !prevGrounded)
                hasLandedSinceLastJump = true;
            wasGrounded = IsGrounded;

            MoveHorizontal();
            HandleJumpAndGravity();
            UpdateAnimatorBools();

            stepClimb(); // tetap panggil
        }

        void MoveHorizontal()
        {
            IsSprinting = MoveInput.y > 0.1f && !IsCrouching && SprintHeld();
            float targetSpeed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

            Vector3 wishDir = (transform.right * MoveInput.x + transform.forward * MoveInput.y).normalized;

            Vector3 v = rb.linearVelocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            Vector3 targetHoriz = wishDir * targetSpeed;

            float usedAccel = IsGrounded ? accel : accel * airControl;
            Vector3 newHoriz = Vector3.MoveTowards(horiz, targetHoriz, usedAccel * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(newHoriz.x, v.y, newHoriz.z);
        }

        void HandleJumpAndGravity()
        {
            if (jumpQueued && IsGrounded && hasLandedSinceLastJump)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                hasLandedSinceLastJump = false;
                postJumpIgnoreTimer = postJumpGroundIgnore;
            }
            jumpQueued = false;

            if (!IsGrounded)
                rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            else
                rb.AddForce(Vector3.down * groundStick, ForceMode.Acceleration);
        }

        // ---------- INPUT ----------
        void ReadInputs()
        {
            var kb = Keyboard.current;
            float x = 0f, y = 0f;
            if (kb != null)
            {
                if (kb.aKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed) x += 1f;
                if (kb.wKey.isPressed) y += 1f;
                if (kb.sKey.isPressed) y -= 1f;
            }
            MoveInput = new Vector2(x, y).normalized;
        }

        bool SprintHeld() => Keyboard.current?.leftShiftKey.isPressed ?? false;
        bool IsCrouchHeld() => Keyboard.current?.leftCtrlKey.isPressed ?? false;
        bool JumpPressed() => Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;

        // ---------- GROUND VIA GroundCheck GO ----------
        bool GroundCheckGO()
        {
            if (!groundCheck) return false;

            // Radius & jarak internal, dihitung dari kapsul (tanpa property publik)
            float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float r = (capsule ? capsule.radius : 0.2f) * scaleXZ;
            Vector3 up = transform.up;

            // mulai sedikit di atas kaki
            Vector3 origin = groundCheck.position + up * 0.02f;
            float dist = 0.08f; // kecil & stabil

            // SphereCast turun (tanpa groundMask)
            if (Physics.SphereCast(origin, r, -up, out RaycastHit hit, dist))
            {
                // ambang kemiringan tetap sebagai konstanta (bukan field publik)
                return hit.normal.y >= 0.6f;
            }

            // fallback: CheckSphere di titik kaki (tanpa groundMask)
            return Physics.CheckSphere(groundCheck.position, r * 0.98f);
        }

        // ---------- ANIM ----------
        void UpdateAnimatorBools()
        {
            if (!anim) return;

            bool left = MoveInput.x < -0.1f;
            bool right = MoveInput.x > 0.1f;
            bool fwd = MoveInput.y > 0.1f;
            bool back = MoveInput.y < -0.1f;

            Vector2 hv = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
            float speed = hv.magnitude;
            bool moving = speed > 0.1f;

            bool isJump = !IsGrounded;
            bool isRun = IsGrounded && (left || right || fwd || back) && IsSprinting;

            bool diagL = left && (fwd || back);
            bool diagR = right && (fwd || back);
            bool pureL = left && !fwd && !back;
            bool pureR = right && !fwd && !back;

            bool isStrafeL = IsGrounded && diagL && !IsSprinting;
            bool isStrafeR = IsGrounded && diagR && !IsSprinting;
            bool isStrafeLWalk = IsGrounded && pureL && !IsSprinting;
            bool isStrafeRWalk = IsGrounded && pureR && !IsSprinting;

            bool isWalk = IsGrounded && !isRun && !isStrafeL && !isStrafeR && !isStrafeLWalk && !isStrafeRWalk
                          && (fwd || back) && moving;

            bool noKeys = !(left || right || fwd || back);
            bool isTurnL = IsGrounded && noKeys && mouseXRaw < -turnMouseThreshold;
            bool isTurnR = IsGrounded && noKeys && mouseXRaw > turnMouseThreshold;

            anim.SetBool(jumpHash, isJump);
            anim.SetBool(runHash, !isJump && isRun);
            anim.SetBool(walkHash, !isJump && isWalk);
            anim.SetBool(strafeLHash, !isJump && isStrafeL);
            anim.SetBool(strafeRHash, !isJump && isStrafeR);
            anim.SetBool(strafeLWalkHash, !isJump && isStrafeLWalk);
            anim.SetBool(strafeRWalkHash, !isJump && isStrafeRWalk);
            anim.SetBool(turnLHash, !isJump && isTurnL);
            anim.SetBool(turnRHash, !isJump && isTurnR);
        }

        // ---------- UTIL KAPSUL DUNIA ----------
        void GetCapsuleWorldAt(Vector3 centerPos, out Vector3 top, out Vector3 bottom, out float radius)
        {
            float scaleY = transform.lossyScale.y;
            float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

            radius = capsule.radius * scaleXZ;
            float height = Mathf.Max(capsule.height * scaleY, 2f * radius + 0.01f);

            Vector3 up = transform.up;
            Vector3 worldCenter = transform.TransformPoint(capsule.center) + (centerPos - rb.position);
            float half = (height * 0.5f) - radius;

            top = worldCenter + up * half;
            bottom = worldCenter - up * half;
        }

        // ---------- DEBUG GIZMOS ----------
        void OnDrawGizmosSelected()
        {
            if (!groundCheck) return;
            Gizmos.color = Color.green;

            float scaleXZ = Application.isPlaying ? Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : 1f;
            float r = (capsule ? capsule.radius : 0.2f) * scaleXZ;

            // sphere di titik kaki
            Gizmos.DrawWireSphere(groundCheck.position, r);

            // garis cast turun (pakai dist internal 0.08f)
            Vector3 origin = groundCheck.position + transform.up * 0.02f;
            Gizmos.DrawLine(origin, origin - transform.up * 0.08f);
        }

        // ---------- STEP CLIMB (biarkan sesuai punyamu) ----------
        void stepClimb()
        {
            RaycastHit hitLower;
            if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
            {
                RaycastHit hitUpper;
                if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.2f))
                {
                    rb.position -= new Vector3(0f, -stepSmooth, 0f);
                }
            }
        }
    }
}

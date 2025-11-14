using UnityEngine;
using UnityEngine.InputSystem;

namespace FPP
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Puan_control : MonoBehaviour
    {
        // =======================
        //     CONFIGURATIONS
        // =======================

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 6f;
        public float crouchSpeed = 1.5f;
        [Tooltip("Seberapa cepat mengubah velocity horizontal ke target")]
        public float accel = 40f;
        [Range(0f, 1f)] public float airControl = 0.4f;

        [Header("Jump & Gravity")]
        public float jumpForce = 5f;
        public float extraGravity = 20f;
        public float groundStick = 10f;
        [Tooltip("Setelah lompat, jeda sebelum diizinkan detect grounded lagi")]
        public float postJumpGroundIgnore = 0.12f;

        [SerializeField, Tooltip("Cooldown antar lompatan untuk anti-bunnyhop")]
        float jumpCooldown = 0.10f;
        [SerializeField, Tooltip("Minimal durasi grounded sebelum boleh lompat")]
        float minGroundedTime = 0.05f;

        [Header("Crouch (Collider)")]
        public float standingHeight = 1.8f;
        public float crouchingHeight = 1.2f;

        [Header("Ground (GroundCheck GO ONLY)")]
        public Transform groundCheck; // drag GameObject "kaki"
        [Tooltip("Layer yang dihitung sebagai tanah (excluded Player)")]
        public LayerMask groundMask = ~0;
        [SerializeField, Range(0.0f, 1.0f)]
        float minGroundNormalY = 0.6f; // ambang kemiringan

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

        [Header("Turning")]
        public float turnMouseThreshold = 0.2f;

        // =======================
        //       RUNTIME STATE
        // =======================

        int walkHash, runHash, jumpHash, strafeLHash, strafeLWalkHash, strafeRHash, strafeRWalkHash, turnLHash, turnRHash;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector2 MoveInput { get; private set; }

        Rigidbody rb;
        CapsuleCollider capsule;
        Collider selfCol;

        bool jumpQueued;
        bool hasLandedSinceLastJump = true;
        float postJumpIgnoreTimer;
        float groundedTimer;
        float nextJumpTime;
        float mouseXRaw;

        // temp buffer untuk overlap (hindari alloc)
        readonly Collider[] overlapBuf = new Collider[8];

        // =======================
        //        LIFECYCLE
        // =======================

        [SerializeField] private gagak_sfx gagakAudio;


        GameObject mainCamera;

        void Start()
        {
            mainCamera = GameObject.Find("MainCamera");
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false; // gravity manual

            capsule = GetComponent<CapsuleCollider>();
            capsule.height = standingHeight;
            capsule.center = new Vector3(0f, standingHeight * 0.5f, 0f);

            selfCol = GetComponent<Collider>();

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
                anim.updateMode = AnimatorUpdateMode.Normal; // disinkronkan via Update()
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.speed = 1f;
            }

            // Set tinggi ray step di local space (bukan world)
            if (stepRayUpper)
            {
                var lp = stepRayUpper.transform.localPosition;
                stepRayUpper.transform.localPosition = new Vector3(lp.x, stepRayHeight, lp.z);
            }

            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            ReadInputs();

            // crouch (hold) + safe-stand check
            bool crouchHeld = IsCrouchHeld();
            if (!crouchHeld && IsCrouching)
            {
                // mau berdiri → cek headroom
                if (!CanStandUp())
                    crouchHeld = true; // tetap crouch kalau mentok
            }
            IsCrouching = crouchHeld;

            float targetH = IsCrouching ? crouchingHeight : standingHeight;
            capsule.height = Mathf.Lerp(capsule.height, targetH, Time.deltaTime * 12f);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            if (JumpPressed())
                jumpQueued = true;

            // Mouse delta (Input System baru)
            mouseXRaw = Mouse.current?.delta.ReadValue().x ?? 0f;

            // Animator sinkron di Update
            UpdateAnimatorBools();
        }

        void FixedUpdate()
        {
            if (postJumpIgnoreTimer > 0f)
                postJumpIgnoreTimer -= Time.fixedDeltaTime;

            bool prevGrounded = IsGrounded;

            // Grounding: via groundCheck spherecast/overlap
            bool groundByCast = GroundCheckGO();
            IsGrounded = (postJumpIgnoreTimer <= 0f) && groundByCast;

            groundedTimer = IsGrounded ? groundedTimer + Time.fixedDeltaTime : 0f;

            if (IsGrounded && !prevGrounded)
                hasLandedSinceLastJump = true;

            MoveHorizontal();
            HandleJumpAndGravity();
            StepClimb(); // hanya jalan saat grounded
        }

        // =======================
        //        MOVEMENT
        // =======================

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
            bool canJumpNow =
                jumpQueued &&
                IsGrounded &&
                hasLandedSinceLastJump &&
                groundedTimer >= minGroundedTime &&
                Time.time >= nextJumpTime &&
                rb.linearVelocity.y <= 0.1f; // cegah chain jump saat masih naik

            if (canJumpNow)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                hasLandedSinceLastJump = false;
                postJumpIgnoreTimer = postJumpGroundIgnore;
                nextJumpTime = Time.time + jumpCooldown;
            }
            jumpQueued = false;

            // Gravity manual
            if (!IsGrounded)
                rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            else
                rb.AddForce(Vector3.down * groundStick, ForceMode.Acceleration);
        }

        // =======================
        //          INPUT
        // =======================

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

        // =======================
        //       GROUND CHECK
        // =======================

        bool GroundCheckGO()
        {
            if (!groundCheck || !capsule) return false;

            // Skala radius sesuai transform
            float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float r = (capsule.radius > 0f ? capsule.radius : 0.2f) * scaleXZ;
            Vector3 up = transform.up;

            // Mulai sedikit di atas kaki, cast turun
            Vector3 origin = groundCheck.position + up * 0.02f;
            float dist = 0.08f;

            if (Physics.SphereCast(origin, r, -up, out RaycastHit hit, dist, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (!IsSelf(hit.collider) && hit.normal.y >= minGroundNormalY)
                    return true;
            }

            // Fallback: overlap sphere
            int n = Physics.OverlapSphereNonAlloc(groundCheck.position, r * 0.98f, overlapBuf, groundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = overlapBuf[i];
                if (c && !IsSelf(c))
                    return true;
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            // cek berdasarkan tag, bukan nama
            if (other.CompareTag("GagakArea"))
            {
                if (gagakAudio != null)
                {
                    gagakAudio.rubah_musik2();
                    Debug.Log("Masuk area gagak → rubah_musik2");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("GagakArea"))
            {
                if (gagakAudio != null)
                {
                    gagakAudio.rubah_musik1();
                    Debug.Log("Keluar area gagak → rubah_musik1");
                }
            }
        }


        bool IsSelf(Collider c) => c && c.transform.root == transform.root;

        // =======================
        //         ANIM
        // =======================

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

        // =======================
        //      CROUCH/HEADROOM
        // =======================

        bool CanStandUp()
        {
            if (!capsule) return true;

            float scaleY = transform.lossyScale.y;
            float scaleXZ = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

            float radius = capsule.radius * scaleXZ;
            float height = Mathf.Max(standingHeight * scaleY, 2f * radius + 0.01f);

            Vector3 up = transform.up;
            Vector3 worldCenter = transform.TransformPoint(capsule.center);
            float half = (height * 0.5f) - radius;

            Vector3 top = worldCenter + up * half;
            Vector3 bottom = worldCenter - up * half;

            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = overlapBuf[i];
                if (c && !IsSelf(c))
                    return false; // ada halangan
            }
            return true;
        }

        // =======================
        //       STEP CLIMB
        // =======================

        void StepClimb()
        {
            if (!IsGrounded) return;
            if (!stepRayLower || !stepRayUpper) return;

            if (Physics.Raycast(stepRayLower.transform.position, transform.forward, out var hitLower, 0.1f))
            {
                if (!Physics.Raycast(stepRayUpper.transform.position, transform.forward, out var hitUpper, 0.2f))
                {
                    rb.MovePosition(rb.position + Vector3.up * stepSmooth);
                }
            }
        }

        // =======================
        //        DEBUG DRAW
        // =======================

        void OnDrawGizmosSelected()
        {
            if (!groundCheck) return;
            Gizmos.color = Color.green;

            float scaleXZ = Application.isPlaying ? Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : 1f;
            float r = (capsule ? capsule.radius : 0.2f) * scaleXZ;

            Gizmos.DrawWireSphere(groundCheck.position, r);

            Vector3 origin = groundCheck.position + transform.up * 0.02f;
            Gizmos.DrawLine(origin, origin - transform.up * 0.08f);
        }
    }
}

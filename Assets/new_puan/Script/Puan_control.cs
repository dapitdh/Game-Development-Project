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

        [Header("Ground (via Rigidbody Contacts)")]
        [Range(0f, 1f)] public float minGroundNormalY = 0.6f;

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

        int walkHash, runHash, jumpHash, strafeLHash, strafeLWalkHash, strafeRHash, strafeRWalkHash, turnLHash, turnRHash;

        // ===== Stairs / Step Assist =====
        [Header("Stairs / Step Assist (robust)")]
        [Tooltip("Tinggi maksimum anak tangga yang bisa dinaikkan otomatis")]
        public float stepHeight = 0.45f;
        [Tooltip("Seberapa jauh memeriksa rintangan di depan kaki")]
        public float stepProbeForward = 0.35f;
        [Tooltip("Radius probe (mendekati radius capsule)")]
        public float stepProbeRadius = 0.12f;
        [Tooltip("Kecepatan naik per detik saat menapak step")]
        public float stepClimbRate = 3.5f;
        [Tooltip("Berapa kali mencoba 'naik kecil' per FixedUpdate")]
        public int stepIterations = 3;
        [Tooltip("Layer environment/tangga (pakai sama dgn groundMask kamu)")]
        public LayerMask groundMask;

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
        bool groundedFromContacts;

        // untuk turn anim
        float mouseXRaw;
        public float turnMouseThreshold = 0.2f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;

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
            IsGrounded = (postJumpIgnoreTimer <= 0f) && groundedFromContacts;
            groundedFromContacts = false;

            if (IsGrounded && !prevGrounded)
                hasLandedSinceLastJump = true;
            wasGrounded = IsGrounded;

            MoveHorizontal();

            // === STEP ASSIST aktif: jalan/larian/crouch semua bisa ===
            if (IsGrounded && MoveInput.sqrMagnitude > 0.01f)
            {
                Vector3 wishDir = (transform.right * MoveInput.x + transform.forward * MoveInput.y).normalized;

                for (int i = 0; i < stepIterations; i++)
                {
                    if (!TryStepUpIterative(wishDir)) break;
                    // nolkan Vy agar tak “mental” saat nabrak bibir
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }
            }

            HandleJumpAndGravity();
            UpdateAnimatorBools();
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

        void OnCollisionStay(Collision col)
        {
            if (postJumpIgnoreTimer > 0f) return;
            foreach (var cp in col.contacts)
            {
                if (cp.normal.y >= minGroundNormalY)
                {
                    groundedFromContacts = true;
                    break;
                }
            }
        }

        // ====== Animator sync lengkap ======
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

        // ============================
        // STEP ASSIST (CapsuleCast + CheckCapsule + iterative climb)
        // ============================
        bool TryStepUpIterative(Vector3 dir)
        {
            // 3 arah (depan & diagonal) supaya gak nyangkut di sudut
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3[] dirs = new Vector3[] { dir, (dir + right).normalized, (dir - right).normalized };

            foreach (var d in dirs)
            {
                if (TryStepOnce(d)) return true;
            }
            return false;
        }

        bool TryStepOnce(Vector3 dir)
        {
            float skin = 0.02f;
            float climbPerFrame = Mathf.Max(0.03f, stepClimbRate * Time.fixedDeltaTime);

            // hitung world capsule
            GetCapsuleWorldAt(rb.position, out Vector3 top, out Vector3 bottom, out float capRadius);

            // Origin rendah sedikit di atas dasar kapsul
            float bottomY = bottom.y;
            Vector3 foot = new Vector3(rb.position.x, bottomY, rb.position.z);
            Vector3 lowOrigin = foot + Vector3.up * (skin + capRadius * 0.2f);

            // LOW CAPSULECAST: ada bibir di depan?
            Vector3 p1 = lowOrigin + Vector3.up * (capsule.height * 0.5f - capRadius);
            Vector3 p2 = lowOrigin - Vector3.up * (capsule.height * 0.5f - capRadius);

            RaycastHit lowHit;
            bool hit = Physics.CapsuleCast(
                p1, p2, stepProbeRadius, dir, out lowHit,
                maxDistance: stepProbeForward, layerMask: groundMask
            );
            if (!hit) return false;

            // Pastikan ini bibir (permukaan bukan lantai)
            if (lowHit.normal.y > 0.25f) return false;

            // Cari ketinggian yang clear: 0 → stepHeight
            float targetUp = 0f;
            bool found = false;

            for (float h = climbPerFrame; h <= stepHeight + 0.001f; h += climbPerFrame)
            {
                // kandidat posisi: naik h + sedikit maju sejauh lowHit.distance
                Vector3 candidate = rb.position + Vector3.up * h + dir * Mathf.Min(lowHit.distance + 0.03f, stepProbeForward);

                if (CapsuleClearAt(candidate))
                {
                    targetUp = h;
                    found = true;
                    break;
                }
            }

            if (!found) return false;

            // Snap naik sebagian (halus)
            float climb = Mathf.Min(targetUp, climbPerFrame);
            rb.MovePosition(rb.position + Vector3.up * climb);

            return true;
        }

        bool CapsuleClearAt(Vector3 centerPos)
        {
            GetCapsuleWorldAt(centerPos, out Vector3 top, out Vector3 bottom, out float radius);
            return !Physics.CheckCapsule(top, bottom, radius, groundMask, QueryTriggerInteraction.Ignore);
        }

        void GetCapsuleWorldAt(Vector3 centerPos, out Vector3 top, out Vector3 bottom, out float radius)
        {
            // skala kira-kira: ambil skala terbesar untuk radius
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
    }
}

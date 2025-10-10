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
        public float extraGravity = 20f;   // gaya turun tambahan saat di udara
        public float groundStick = 10f;    // nempel ke tanah saat grounded
        [Tooltip("Abaikan ground sesaat setelah lompatan agar tidak langsung terdeteksi grounded")]
        public float postJumpGroundIgnore = 0.12f;

        [Header("Crouch (Collider)")]
        public float standingHeight = 1.8f;
        public float crouchingHeight = 1.2f;

        [Header("Ground (via Rigidbody Contacts)")]
        [Range(0f, 1f)] public float minGroundNormalY = 0.6f; // > ~53°

        [Header("Stairs / Step Assist (NEW)")]
        [Tooltip("Tinggi maksimum anak tangga yang bisa dinaikkan otomatis")]
        public float stepHeight = 0.45f;
        [Tooltip("Jarak cek halangan di depan kaki")]
        public float stepCheckDistance = 0.35f;
        [Tooltip("Radius sphere untuk deteksi bibir tangga")]
        public float stepProbeRadius = 0.1f;
        [Tooltip("Kecepatan naik per detik (dibatasi per FixedUpdate)")]
        public float stepClimbRate = 2.0f;
        [Tooltip("Berapa kali mencoba 'naik' kecil per FixedUpdate")]
        public int stepIterations = 2;
        [Tooltip("Layer environment/tangga")]
        public LayerMask groundMask;

        // State publik (untuk kamera)
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector2 MoveInput { get; private set; }

        Rigidbody rb;
        CapsuleCollider capsule;

        // --- Lompat & grounded via kontak ---
        bool jumpQueued;
        bool hasLandedSinceLastJump = true;
        bool wasGrounded;
        float postJumpIgnoreTimer;
        bool groundedFromContacts;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true; // cegah terguling

            capsule = GetComponent<CapsuleCollider>();
            capsule.height = standingHeight;
            capsule.center = new Vector3(0f, standingHeight * 0.5f, 0f);
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

            // ==== STEP ASSIST: beberapa iterasi naik kecil agar halus ====
            if (IsGrounded && MoveInput.sqrMagnitude > 0.01f && !IsCrouching)
            {
                Vector3 wishDir = (transform.right * MoveInput.x + transform.forward * MoveInput.y).normalized;
                for (int i = 0; i < stepIterations; i++)
                {
                    if (!TryStepUpMulti(wishDir)) break;
                    // nolkan VY agar tidak "mental" saat nyentuh bibir
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }
            }

            HandleJumpAndGravity();
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

        // Ground via kontak physics
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

        // ============================================================
        // STEP ASSIST: spherecast low/high + downcast, 3 arah (depan/diagonal)
        // ============================================================
        bool TryStepUpMulti(Vector3 wishDir)
        {
            // 3 arah: depan & 45° kiri/kanan agar ga nyangkut di sudut pijakan
            Vector3 right = Vector3.Cross(Vector3.up, wishDir).normalized;
            Vector3[] dirs = new Vector3[]
            {
                wishDir,
                (wishDir + right).normalized,
                (wishDir - right).normalized,
            };

            foreach (var d in dirs)
            {
                if (TryStepUpOnce(d)) return true;
            }
            return false;
        }

        bool TryStepUpOnce(Vector3 dir)
        {
            float radius = capsule.radius;
            float skin = 0.02f;
            float castDist = radius + stepCheckDistance;

            float bottomY = rb.position.y + capsule.center.y - (capsule.height * 0.5f) + radius;
            Vector3 foot = new Vector3(rb.position.x, bottomY, rb.position.z);

            Vector3 lowOrigin = foot + Vector3.up * skin;

            // LOW SPHERECAST (pakai out + named args)
            RaycastHit lowHit;
            if (Physics.SphereCast(lowOrigin, stepProbeRadius, dir, out lowHit,
                                   castDist, layerMask: groundMask,
                                   queryTriggerInteraction: QueryTriggerInteraction.Ignore))
            {
                if (lowHit.normal.y > 0.2f) return false;

                // HIGH SPHERECAST: hanya untuk cek blocked
                Vector3 highOrigin = lowOrigin + Vector3.up * stepHeight;
                RaycastHit highHit;
                bool blockedHigh = Physics.SphereCast(highOrigin, stepProbeRadius, dir, out highHit,
                                                      castDist, layerMask: groundMask,
                                                      queryTriggerInteraction: QueryTriggerInteraction.Ignore);
                if (blockedHigh) return false;

                // DOWNCAST: cari permukaan mendarat
                Vector3 downStart = highOrigin + dir * (lowHit.distance + skin);
                float downRange = stepHeight + 0.6f;

                RaycastHit downHit;
                bool foundLanding =
                    Physics.SphereCast(downStart, stepProbeRadius, Vector3.down, out downHit,
                                       downRange, layerMask: groundMask,
                                       queryTriggerInteraction: QueryTriggerInteraction.Ignore)
                    || Physics.Raycast(downStart, Vector3.down, out downHit,
                                       downRange, groundMask, QueryTriggerInteraction.Ignore);

                if (foundLanding && downHit.normal.y >= minGroundNormalY)
                {
                    float neededBottomY = downHit.point.y + skin;
                    float neededUp = neededBottomY - bottomY;

                    if (neededUp > 0f && neededUp <= stepHeight + 0.05f)
                    {
                        float climb = Mathf.Min(neededUp, stepClimbRate * Time.fixedDeltaTime);
                        rb.MovePosition(rb.position + Vector3.up * climb);
                        return true;
                    }
                }
            }
            return false;
        }

    }
}

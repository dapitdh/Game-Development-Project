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
        [Tooltip("Waktu minimal benar2 menyentuh tanah agar dianggap grounded (stabil)")]
        public float minStableGroundTime = 0.05f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.28f;
        public LayerMask groundMask;

        [Header("Crouch (Collider)")]
        public float standingHeight = 1.8f;
        public float crouchingHeight = 1.2f;

        // State publik (untuk kamera)
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector2 MoveInput { get; private set; }

        Rigidbody rb;
        CapsuleCollider capsule;

        // --- Anti "terbang" ---
        bool jumpQueued;
        bool hasLandedSinceLastJump = true; // WAJIB true untuk bisa lompat
        bool wasGrounded;
        float postJumpIgnoreTimer;
        float groundedStableTimer;          // akumulasi waktu grounded stabil

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;

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
            UpdateGrounded();
            MoveHorizontal();
            HandleJumpAndGravity();
        }

        void UpdateGrounded()
        {
            if (postJumpIgnoreTimer > 0f)
                postJumpIgnoreTimer -= Time.fixedDeltaTime;

            // Raw ground (tanpa stabilisasi), tapi diabaikan saat post-jump ignore aktif
            bool rawGround =
                postJumpIgnoreTimer <= 0f &&
                Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

            // Stabilisasi grounded (harus bertahan minStableGroundTime)
            if (rawGround)
                groundedStableTimer += Time.fixedDeltaTime;
            else
                groundedStableTimer = 0f;

            IsGrounded = groundedStableTimer >= minStableGroundTime;

            // Event landing (udara -> grounded)
            if (IsGrounded && !wasGrounded)
                hasLandedSinceLastJump = true;

            wasGrounded = IsGrounded;
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
            // HANYA boleh lompat jika grounded stabil & sudah mendarat sejak lompatan terakhir
            if (jumpQueued && IsGrounded && hasLandedSinceLastJump)
            {
                // reset vertikal lalu lompat
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                // kunci sampai benar2 mendarat lagi
                hasLandedSinceLastJump = false;
                postJumpIgnoreTimer = postJumpGroundIgnore;
                groundedStableTimer = 0f; // paksa harus stabil lagi setelah lompat
            }
            jumpQueued = false;

            // gravity / stick
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

        void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}

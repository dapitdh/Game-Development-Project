namespace EasyPeasyFirstPersonController
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public partial class FirstPersonController : MonoBehaviour
    {
        [Range(0, 100)] public float mouseSensitivity = 25f;
        [Range(0f, 200f)] private float snappiness = 100f;
        [Range(0f, 20f)] public float walkSpeed = 10f;
        [Range(0f, 30f)] public float sprintSpeed = 15f;
        [Range(0f, 10f)] public float crouchSpeed = 6f;
        public float crouchHeight = 1f;
        public float crouchCameraHeight = 0.5f;
        public float slideSpeed = 9f;
        public float slideDuration = 0.7f;
        public float slideFovBoost = 5f;
        public float slideTiltAngle = 5f;
        [Range(0f, 15f)] public float jumpSpeed = 3f;
        [Range(0f, 50f)] public float gravity = 9.81f;
        public bool coyoteTimeEnabled = true;
        public float coyoteTimeDuration = 0.25f;
        public float normalFov = 60f;
        public float sprintFov = 70f;
        public float fovChangeSpeed = 5f;
        public float walkingBobbingSpeed = 14f;
        public float bobbingAmount = 0.05f;
        private float sprintBobMultiplier = 1.2f;
        private float recoilReturnSpeed = 8f;
        public bool canSlide = true;
        public bool canJump = true;
        public bool canSprint = true;
        public bool canCrouch = true;
        public Transform groundCheck;
        public float groundDistance = 0.3f;
        public LayerMask groundMask;
        public Transform playerCamera;
        public Transform cameraParent;

        private float rotX, rotY;
        private float xVelocity, yVelocity;
        private CharacterController characterController;
        private Vector3 moveDirection = Vector3.zero;
        private bool isGrounded;
        private Vector2 moveInput;
        public bool isSprinting;
        public bool isCrouching;
        public bool isSliding;
        private float slideTimer;
        private float postSlideCrouchTimer;
        private Vector3 slideDirection;
        private float originalHeight;
        private float originalCameraParentHeight;
        private float coyoteTimer;
        private Camera cam;
        private AudioSource slideAudioSource;
        private float bobTimer;
        private float defaultPosY;
        private Vector3 recoil = Vector3.zero;
        private bool isLook = true, isMove = true;
        private float currentCameraHeight;
        private float currentBobOffset;
        private float currentFov;
        private float fovVelocity;
        private float currentSlideSpeed;
        private float slideSpeedVelocity;
        private float currentTiltAngle;
        private float tiltVelocity;

        // === Animator ===
        private Animator animator;

        public float CurrentCameraHeight => isCrouching || isSliding ? crouchCameraHeight : originalCameraParentHeight;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            cam = playerCamera.GetComponent<Camera>();
            animator = GetComponentInChildren<Animator>(); // ambil Animator dari child juga

            originalHeight = characterController.height;
            originalCameraParentHeight = cameraParent.localPosition.y;
            defaultPosY = cameraParent.localPosition.y;
            slideAudioSource = gameObject.AddComponent<AudioSource>();
            slideAudioSource.playOnAwake = false;
            slideAudioSource.loop = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            currentCameraHeight = originalCameraParentHeight;
            currentBobOffset = 0f;
            currentFov = normalFov;
            currentSlideSpeed = 0f;
            currentTiltAngle = 0f;
        }

        private void Update()
        {
            // --- Ground check
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            if (isGrounded && moveDirection.y < 0)
            {
                moveDirection.y = -2f;
                coyoteTimer = coyoteTimeEnabled ? coyoteTimeDuration : 0f;
            }
            else if (coyoteTimeEnabled)
            {
                coyoteTimer -= Time.deltaTime;
            }

            // --- Mouse Look
            if (isLook && Mouse.current != null)
            {
                float mouseX = Mouse.current.delta.ReadValue().x * mouseSensitivity * Time.deltaTime;
                float mouseY = Mouse.current.delta.ReadValue().y * mouseSensitivity * Time.deltaTime;

                rotX += mouseX;
                rotY -= mouseY;
                rotY = Mathf.Clamp(rotY, -90f, 90f);

                xVelocity = Mathf.Lerp(xVelocity, rotX, snappiness * Time.deltaTime);
                yVelocity = Mathf.Lerp(yVelocity, rotY, snappiness * Time.deltaTime);

                float targetTiltAngle = isSliding ? slideTiltAngle : 0f;
                currentTiltAngle = Mathf.SmoothDamp(currentTiltAngle, targetTiltAngle, ref tiltVelocity, 0.2f);

                playerCamera.localRotation = Quaternion.Euler(yVelocity - currentTiltAngle, 0f, 0f);
                transform.rotation = Quaternion.Euler(0f, xVelocity, 0f);
            }

            HandleHeadBob();
            HandleMovement();
        }

        private void HandleHeadBob()
        {
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            bool isMovingEnough = horizontalVelocity.magnitude > 0.1f;

            float targetBobOffset = isMovingEnough ? Mathf.Sin(bobTimer) * bobbingAmount : 0f;
            currentBobOffset = Mathf.Lerp(currentBobOffset, targetBobOffset, Time.deltaTime * walkingBobbingSpeed);

            if (!isGrounded || isSliding || isCrouching)
            {
                bobTimer = 0f;
                float targetCameraHeight = isCrouching || isSliding ? crouchCameraHeight : originalCameraParentHeight;
                currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * 10f);
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, currentCameraHeight + currentBobOffset, cameraParent.localPosition.z);
                recoil = Vector3.zero;
                cameraParent.localRotation = Quaternion.RotateTowards(cameraParent.localRotation, Quaternion.Euler(recoil), recoilReturnSpeed * Time.deltaTime);
                return;
            }

            if (isMovingEnough)
            {
                float bobSpeed = walkingBobbingSpeed * (isSprinting ? sprintBobMultiplier : 1f);
                bobTimer += Time.deltaTime * bobSpeed;
                float targetCameraHeight = isCrouching || isSliding ? crouchCameraHeight : originalCameraParentHeight;
                currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * 10f);
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, currentCameraHeight + currentBobOffset, cameraParent.localPosition.z);
                recoil.z = moveInput.x * -2f;
            }
            else
            {
                bobTimer = 0f;
                float targetCameraHeight = isCrouching || isSliding ? crouchCameraHeight : originalCameraParentHeight;
                currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * 10f);
                cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, currentCameraHeight + currentBobOffset, cameraParent.localPosition.z);
                recoil = Vector3.zero;
            }

            cameraParent.localRotation = Quaternion.RotateTowards(cameraParent.localRotation, Quaternion.Euler(recoil), recoilReturnSpeed * Time.deltaTime);
        }

        private void HandleMovement()
        {
            // --- Keyboard movement (WASD)
            if (Keyboard.current != null)
            {
                moveInput.x = (Keyboard.current.aKey.isPressed ? -1 : 0) + (Keyboard.current.dKey.isPressed ? 1 : 0);
                moveInput.y = (Keyboard.current.sKey.isPressed ? -1 : 0) + (Keyboard.current.wKey.isPressed ? 1 : 0);
            }
            else
            {
                moveInput = Vector2.zero;
            }

            // Sprinting
            isSprinting = canSprint && Keyboard.current.leftShiftKey.isPressed && moveInput.y > 0.1f && isGrounded && !isCrouching && !isSliding;

            float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            if (!isMove) currentSpeed = 0f;

            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 moveVector = transform.TransformDirection(direction) * currentSpeed;
            moveVector = Vector3.ClampMagnitude(moveVector, currentSpeed);

            // Jump
            if ((isGrounded || coyoteTimer > 0f))
            {
                if (canJump && Keyboard.current.spaceKey.wasPressedThisFrame && !isSliding)
                {
                    moveDirection.y = jumpSpeed;
                }
                else if (moveDirection.y < 0)
                {
                    moveDirection.y = -2f;
                }
            }
            else
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            if (!isSliding)
            {
                moveDirection = new Vector3(moveVector.x, moveDirection.y, moveVector.z);
                characterController.Move(moveDirection * Time.deltaTime);
            }

            // === Update Animator berdasarkan input KeyCode ===
            if (animator != null)
            {
                bool pressWASD = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                                 Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

                if (!pressWASD) // Idle
                {
                    animator.SetBool("idle", true);
                    animator.SetBool("walking", false);
                    animator.SetBool("running", false);
                }
                else if (pressWASD && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) // Jalan
                {
                    animator.SetBool("idle", false);
                    animator.SetBool("walking", true);
                    animator.SetBool("running", false);
                }
                else if (pressWASD && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) // Lari
                {
                    animator.SetBool("idle", false);
                    animator.SetBool("walking", false);
                    animator.SetBool("running", true);
                }

                // Debug cek di console
                Debug.Log($"Idle:{animator.GetBool("idle")}, Walk:{animator.GetBool("walking")}, Run:{animator.GetBool("running")}");
            }
        }

        public void SetControl(bool newState)
        {
            SetLookControl(newState);
            SetMoveControl(newState);
        }

        public void SetLookControl(bool newState) => isLook = newState;
        public void SetMoveControl(bool newState) => isMove = newState;

        public void SetCursorVisibility(bool newVisibility)
        {
            Cursor.lockState = newVisibility ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = newVisibility;
        }
    }
}

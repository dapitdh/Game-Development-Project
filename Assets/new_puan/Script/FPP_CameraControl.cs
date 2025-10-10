using UnityEngine;

namespace FPP
{
    public class FPP_CameraControl : MonoBehaviour
    {
        [Header("References")]
        public Transform playerBody;       // Puan
        public Transform cameraTransform;  // MainCamera
        public Puan_control movement;      // referensi ke script player (untuk baca state)

        [Header("Mouse Look")]
        public float sensitivity = 1.6f;
        public float lookSmooth = 12f;

        [Header("Head Bob & Tilt")]
        public float bobFrequency = 1.8f;
        public float bobAmplitude = 0.045f;    // naik turun saat jalan
        public float tiltAngle = 5f;           // miring saat A/D ditekan
        public float camSmooth = 10f;

        [Header("Crouch Camera Height")]
        public float standingCamY = 1.6f;
        public float crouchCamY = 1.0f;

        [Header("FOV (Zoom saat sprint)")]
        public float normalFov = 60f;
        public float sprintFov = 68f;
        public float fovLerpSpeed = 8f;

        float xRot;                 // pitch (atas-bawah)
        float bobTimer;
        Vector3 camLocalStart;
        Camera cam;
        Rigidbody rb;               // ambil dari player

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;

            cam = cameraTransform.GetComponent<Camera>();
            cam.fieldOfView = normalFov;
            camLocalStart = cameraTransform.localPosition;

            if (playerBody != null)
                rb = playerBody.GetComponent<Rigidbody>();
        }

        void Update()
        {
            HandleLook();
            HandleHeadBobAndTilt();
            HandleFovAndCamHeight();
        }

        void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            xRot -= mouseY;
            xRot = Mathf.Clamp(xRot, -90f, 90f);

            // pitch di kamera
            Quaternion targetPitch = Quaternion.Euler(xRot, 0f, 0f);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetPitch, Time.deltaTime * lookSmooth);

            // yaw di badan player
            playerBody.Rotate(Vector3.up * mouseX);
        }

        void HandleHeadBobAndTilt()
        {
            if (rb == null) return;

            // Kecepatan horizontal (lokal)
            Vector3 localVel = playerBody.InverseTransformDirection(rb.linearVelocity);
            Vector2 horiz = new Vector2(localVel.x, localVel.z);
            float speed = horiz.magnitude;

            bool grounded = movement ? movement.IsGrounded : true;
            bool moving = grounded && speed > 0.1f;

            float targetTilt = 0f;
            float targetBob = 0f;

            // ==== HEADBOB hanya ketika maju (W) ====
            if (moving && movement.MoveInput.y > 0.1f)
            {
                bobTimer += Time.deltaTime * bobFrequency;
                targetBob = Mathf.Sin(bobTimer) * bobAmplitude;
            }
            else
            {
                bobTimer = 0f;
            }

            // ==== TILT hanya ketika strafe (A/D) ====
            if (movement.MoveInput.x < -0.1f)     // A
                targetTilt = tiltAngle;
            else if (movement.MoveInput.x > 0.1f) // D
                targetTilt = -tiltAngle;

            // ==== Terapkan posisi headbob ====
            Vector3 targetPos = new Vector3(camLocalStart.x, camLocalStart.y + targetBob, camLocalStart.z);
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPos, Time.deltaTime * camSmooth);

            // ==== Gabungkan tilt + pitch ====
            Quaternion targetRot = Quaternion.Euler(xRot, 0f, targetTilt);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetRot, Time.deltaTime * camSmooth);
        }

        void HandleFovAndCamHeight()
        {
            if (movement == null || cam == null) return;

            // === Zoom saat sprint ===
            float targetFov = movement.IsSprinting ? sprintFov : normalFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovLerpSpeed);

            // === Kamera turun saat crouch ===
            float targetY = movement.IsCrouching ? crouchCamY : standingCamY;
            Vector3 p = transform.localPosition;
            p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * camSmooth);
            transform.localPosition = p;
        }
    }
}

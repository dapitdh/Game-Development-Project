using UnityEngine;

namespace FPP
{
    public class FPP_CameraControl : MonoBehaviour
    {
        [Header("References")]
        public Transform playerBody;       // Puan
        public Transform cameraTransform;  // MainCamera
        public Puan_control movement;      // baca state

        [Header("Mouse Look")]
        public float sensitivity = 1.6f;
        public float lookSmooth = 12f;

        [Header("Tilt (tanpa headbob)")]
        public float tiltAngle = 5f;       // miring saat A/D
        public float camSmooth = 10f;

        [Header("Crouch Camera Height")]
        public float standingCamY = 1.6f;
        public float crouchCamY = 1.0f;

        [Header("FOV (Zoom saat sprint)")]
        public float normalFov = 60f;
        public float sprintFov = 68f;
        public float fovLerpSpeed = 8f;

        float xRot;                 // pitch (atas-bawah)
        Vector3 camLocalStart;
        Camera cam;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;

            cam = cameraTransform.GetComponent<Camera>();
            cam.fieldOfView = normalFov;
            camLocalStart = cameraTransform.localPosition;
        }

        void Update()
        {
            HandleLookAndTilt();
            HandleFovAndCamHeight();

            // pastikan posisi kamera stabil (tanpa headbob)
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                camLocalStart,
                Time.deltaTime * camSmooth
            );
        }

        void HandleLookAndTilt()
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            xRot -= mouseY;
            xRot = Mathf.Clamp(xRot, -90f, 90f);

            // yaw di badan player (mouse hanya memutar arah pandang, tidak menggerakkan posisi)
            playerBody.Rotate(Vector3.up * mouseX);

            // tilt saat strafe A/D
            float targetTilt = 0f;
            if (movement != null)
            {
                if (movement.MoveInput.x < -0.1f) targetTilt = tiltAngle;    // A
                else if (movement.MoveInput.x > 0.1f) targetTilt = -tiltAngle; // D
            }

            // gabungkan pitch + tilt (tanpa headbob)
            Quaternion targetRot = Quaternion.Euler(xRot, 0f, targetTilt);
            cameraTransform.localRotation = Quaternion.Slerp(
                cameraTransform.localRotation,
                targetRot,
                Time.deltaTime * Mathf.Max(lookSmooth, camSmooth)
            );
        }

        void HandleFovAndCamHeight()
        {
            if (movement == null || cam == null) return;

            // Zoom saat sprint
            float targetFov = movement.IsSprinting ? sprintFov : normalFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovLerpSpeed);

            // Kamera turun saat crouch (atur parent lokal Y)
            float targetY = movement.IsCrouching ? crouchCamY : standingCamY;
            Vector3 p = transform.localPosition;
            p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * camSmooth);
            transform.localPosition = p;
        }
    }
}

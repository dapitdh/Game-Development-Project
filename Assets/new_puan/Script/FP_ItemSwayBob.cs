using UnityEngine;
using UnityEngine.InputSystem;

namespace FPP
{
    public class FP_ItemSwayBob : MonoBehaviour
    {
        [Header("Refs")]
        public Transform item;       // drag: transform yang mau disway (biasanya parent dari mesh)
        public Transform cameraParent; // drag: CameraParent
        public FPP.Puan_control movement; // kalau namespace-mu masih EasyPeasy..., sesuaikan

        [Header("Sway (mouse)")]
        public float swayAmount = 0.025f;
        public float swayMax = 0.06f;
        public float swaySmooth = 8f;

        [Header("Bob (langkah)")]
        public float bobFreq = 6f;
        public float bobAmp = 0.02f;
        public float bobRunMult = 1.4f;
        public float bobSmooth = 10f;

        Vector3 baseLocalPos;
        float bobTimer;

        void Start()
        {
            if (item == null) item = transform;
            baseLocalPos = item.localPosition;
        }

        void LateUpdate()
        {
            if (item == null) return;

            // --- SWAY dari mouse ---
            Vector2 md = Vector2.zero;
            if (Mouse.current != null) md = Mouse.current.delta.ReadValue() * 0.01f;
            else md = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            Vector3 swayOffset = new Vector3(-md.x * swayAmount, -md.y * swayAmount, 0f);
            swayOffset = Vector3.ClampMagnitude(swayOffset, swayMax);

            // --- BOB dari langkah (pakai movement) ---
            float bob = 0f;
            if (movement != null && movement.IsGrounded)
            {
                Vector2 input = movement.MoveInput;
                bool moving = input.sqrMagnitude > 0.01f;
                if (moving)
                {
                    float freq = movement.IsSprinting ? bobFreq * bobRunMult : bobFreq;
                    bobTimer += Time.deltaTime * freq;
                    bob = Mathf.Sin(bobTimer) * bobAmp;
                }
                else bobTimer = 0f;
            }

            Vector3 targetPos = baseLocalPos + swayOffset + new Vector3(0f, bob, 0f);
            item.localPosition = Vector3.Lerp(item.localPosition, targetPos, Time.deltaTime * Mathf.Max(swaySmooth, bobSmooth));
        }
    }
}

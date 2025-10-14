using UnityEngine;
using UnityEngine.InputSystem;

namespace FPP
{
    public class FP_Flashlight : MonoBehaviour
    {
        public Light lightSource;        // drag komponen Light (Spot/Point)
        public Key toggleKey = Key.F;    // tekan F
        public AudioSource clickSfx;     // opsional

        bool isOn = true;

        void Reset()
        {
            lightSource = GetComponentInChildren<Light>();
            clickSfx = GetComponent<AudioSource>();
        }

        void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            isOn = !isOn;
            if (lightSource) lightSource.enabled = isOn;
            if (clickSfx) clickSfx.Play();
        }
    }
}

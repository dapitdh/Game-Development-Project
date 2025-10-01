using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FlashlightIKController : MonoBehaviour
{
    public TwoBoneIKConstraint rightHandIK;

    void Update()
    {
        // contoh: aktifkan IK hanya saat klik kanan (aim)
        if (Input.GetMouseButton(1))
            rightHandIK.weight = 1f;
        else
            rightHandIK.weight = 0f;
    }
}

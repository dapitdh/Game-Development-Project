using UnityEngine;
using UnityEngine.InputSystem;

namespace FPP
{
    public class FP_PlayerInteract : MonoBehaviour
    {
        public Camera cam;            
        public FP_Inventory inventory;   
        public float useDistance = 3f;
        public LayerMask interactMask = ~0;

        void Awake()
        {
            if (!cam) cam = Camera.main;
            if (!inventory) inventory = GetComponentInParent<FP_Inventory>();

            int fp = LayerMask.NameToLayer("FP_Arms");
            if (fp >= 0) interactMask &= ~(1 << fp); 
        }

        void Update()
        {
            var kb = Keyboard.current; if (kb == null || cam == null || inventory == null) return;

            if (kb.eKey.wasPressedThisFrame)
            {
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, useDistance, interactMask))
                {
                    var w = hit.collider ? hit.collider.GetComponentInParent<ItemWorld>() : null;
                    if (w) inventory.Pickup(w);
                }
            }
            if (kb.gKey.wasPressedThisFrame) inventory.DropOneFromEquipped();
            if (kb.digit1Key.wasPressedThisFrame) inventory.EquipByIndex(0);
        }
    }
}

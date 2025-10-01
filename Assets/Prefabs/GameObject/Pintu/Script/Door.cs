using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : OnRaycast
{
    [SerializeField] private float openAngle;
    [SerializeField] private float closeAngle;

    [SerializeField] private float speed;
    [SerializeField] Collider col;

    public bool isOpen;
    Quaternion newRotation;

    private void Update()
    {
        if (isOpen)
        {
            newRotation = Quaternion.Euler(transform.localEulerAngles.x, openAngle, transform.localEulerAngles.z);
        }
        else
        {
            newRotation = Quaternion.Euler(transform.localEulerAngles.x, closeAngle, transform.localEulerAngles.z);
        }


        col.enabled = transform.localRotation == newRotation;

        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, newRotation, speed);
    }

    public override void OnUseItem()
    {
        Debug.Log("Pintu kuncinya terbuka");
        removeRequirementItem(); // pakai nama yang benar
    }


    public override void OnInteract()
    {
        isOpen = !isOpen;
    }
}

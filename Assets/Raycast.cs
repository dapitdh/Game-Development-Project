using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raycast : MonoBehaviour
{
    public float range;
    public LayerMask interactedMask;

    public Inventory inventory;

    private void Update()
    {
        RaycastHit hit; 
        if (Physics.Raycast(transform.position, transform.forward, out hit, range, interactedMask))
        {
            OnRaycast or = hit.transform.gameObject.GetComponent<OnRaycast>();
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (or.requirementItem != null)
                {
                    if (inventory.searchItem(or.requirementItem))
                    {
                        DataItem searchedItem = inventory.searchItem(or.requirementItem);
                        or.OnUseItem();
                        inventory.RemoveItem(searchedItem);
                    }
                    else
                    {
                        Debug.Log("Pintu terkunci. Butuh item: " + or.requirementItem.item.m_name);
                        // jangan panggil OnInteract() kalau tidak punya item
                    }
                }
                else
                {
                    // kalau memang tidak butuh item, baru bisa dibuka biasa
                    or.OnInteract();
                }

            }
        }
    }
}

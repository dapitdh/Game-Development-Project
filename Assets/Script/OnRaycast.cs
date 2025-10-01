using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class OnRaycast : MonoBehaviour
{
    public DataItem requirementItem;
    public virtual void OnInteract(){}

    public virtual void OnUseItem(){}

    public void removeRequirementItem()
    {
        Debug.Log("Removed item: " + gameObject.name);
        requirementItem = null;
    }
}

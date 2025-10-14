using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<DataItem> items = new List<DataItem>();

    public void AddItem(DataItem newItem)
    {
        items.Add(newItem);
        Debug.Log("Added item: " + newItem.item.m_name);
    }

    public void RemoveItem(DataItem Remove_item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item.m_name == Remove_item.item.m_name)
            {
                Debug.Log("Removed item: " + items[i].item.m_name);
                items.RemoveAt(i);
            }
        }
    }

    public DataItem searchItem(DataItem searchItem)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item.m_name == searchItem.item.m_name)
            {
                return items[i];
            }
        }
        return null;
    }
}
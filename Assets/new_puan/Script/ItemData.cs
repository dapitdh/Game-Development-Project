using UnityEngine;

namespace FPP
{
    [CreateAssetMenu(menuName = "FPP/Item Data", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        public string itemId = "flashlight";
        public string displayName = "Flashlight";
        public Sprite icon;
        public GameObject worldPrefab; // <- GameObject!
        public GameObject fpPrefab;    // <- GameObject!
    }
}
using UnityEngine;
using Unity.AI.Navigation;

[DisallowMultipleComponent]
public class DoorLinkNav : MonoBehaviour
{
    public NavMeshLink link;               // auto di Reset
    [Header("Door(s) on this doorway")]
    public Sc_pintu[] doors;               // drag 1 atau 2 daun (kiri/kanan)
    public Transform handlePoint;          // opsional

    [Header("Trigger & Close")]
    [Tooltip("Jarak agent ke garis link untuk memicu buka pintu")]
    public float activationDistance = 0.6f;
    public bool autoClose = true;
    public float closeDelay = 0.8f;        // jeda setelah lewat sebelum menutup
    public float clearDistance = 0.7f;     // jarak minimal agent dari garis link agar dianggap sudah lewat

    void Reset()
    {
        link = GetComponent<NavMeshLink>();
    }

    public void GetWorldPoints(out Vector3 a, out Vector3 b)
    {
        var t = link ? link.transform : transform;
        a = t.TransformPoint(link.startPoint);
        b = t.TransformPoint(link.endPoint);
    }
}

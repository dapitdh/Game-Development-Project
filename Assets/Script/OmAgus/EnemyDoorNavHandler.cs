using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyDoorNavHandler : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Anim (opsional)")]
    public string openDoorState = "";
    public int openDoorLayer = 0;
    public float openBlend = 0.2f;

    [Header("Behaviour")]
    public float faceTurnSpeed = 540f;
    public float minOpenWait = 0.12f;
    public float minOpenPercent = 0.85f;
    public float openTimeout = 2.5f;
    public float checkAheadDistance = 1.6f;
    public float rearmDelay = 0.75f;

    [Header("Debug")] public bool log;

    DoorLinkNav[] allLinks;
    bool busy;
    float lastTriggerTime;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = true; // NavMesh Link di-traverse otomatis
        allLinks = FindObjectsOfType<DoorLinkNav>(true);
    }

    void Update()
    {
        if (busy || !agent.hasPath) return;

        // cari link di depan yang paling relevan
        DoorLinkNav best = null;
        float bestDist = float.MaxValue;

        Vector3 pos = transform.position;
        Vector3 fwd = agent.desiredVelocity.sqrMagnitude > 0.0001f ? agent.desiredVelocity.normalized : transform.forward;

        foreach (var dl in allLinks)
        {
            if (!dl || !dl.link || !dl.enabled) continue;

            dl.GetWorldPoints(out Vector3 a, out Vector3 b);
            float dist = DistancePointToSegment(pos, a, b);
            if (dist > dl.activationDistance) continue;

            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = (mid - pos); dir.y = 0;
            if (Vector3.Dot(fwd, dir.normalized) <= 0f) continue;            // harus di depan
            if (dir.magnitude > checkAheadDistance) continue;                 // terlalu jauh

            if (dist < bestDist) { bestDist = dist; best = dl; }
        }

        if (best && Time.time - lastTriggerTime > rearmDelay)
            StartCoroutine(HandleDoor(best));
    }

    IEnumerator HandleDoor(DoorLinkNav dl)
    {
        busy = true;
        lastTriggerTime = Time.time;

        // titik link
        dl.GetWorldPoints(out Vector3 a, out Vector3 b);
        Vector3 mid = (a + b) * 0.5f;
        Vector3 axis = (b - a); axis.y = 0f;

        // 1) rotate ke handle
        if (dl.handlePoint) yield return RotateTowards(dl.handlePoint.position, faceTurnSpeed);

        // 2) buka semua daun yang ada
        if (dl.doors != null)
        {
            foreach (var d in dl.doors)
            {
                if (!d) continue;

                // mainkan anim upper-body sekali saja
                if (animator && !string.IsNullOrEmpty(openDoorState))
                    animator.CrossFadeInFixedTime(openDoorState, openBlend, openDoorLayer, 0f);

                if (!d.IsBlocked && (!d.IsOpen || d.OpenPercent < minOpenPercent))
                    d.Open();
            }

            // tunggu minimal + sampai salah satu cukup terbuka (atau timeout)
            float t = 0f; yield return new WaitForSeconds(minOpenWait);
            while (!AnyDoorOpenEnough(dl.doors, minOpenPercent) && t < openTimeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // 3) biarkan agent melintas link (auto). Kita tunggu sampai "melewati garis" + jarak aman, lalu close
        if (dl.autoClose && dl.doors != null && dl.doors.Length > 0)
            StartCoroutine(CloseAfterPassed(dl, axis, mid));

        busy = false;
    }

    IEnumerator RotateTowards(Vector3 worldTarget, float degPerSec)
    {
        Vector3 flat = worldTarget - transform.position; flat.y = 0;
        if (flat.sqrMagnitude < 0.0001f) yield break;

        Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degPerSec * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator CloseAfterPassed(DoorLinkNav dl, Vector3 axis, Vector3 mid)
    {
        // tanda “sudah di sisi seberang” menggunakan tanda dot dengan sumbu link
        // sebelum melewati: sign ≈ negatif; sesudah melewati: sign ≈ positif (atau sebaliknya)
        int side0 = Side(transform.position, axis, mid);

        // tunggu sampai agent berganti sisi dan menjauh dari garis link
        while (true)
        {
            int side = Side(transform.position, axis, mid);
            float dist = DistancePointToSegment(transform.position, mid - 0.5f * axis, mid + 0.5f * axis);
            if (side != side0 && dist > dl.clearDistance) break; // sudah lewat + jarak aman
            yield return null;
        }

        // sedikit delay agar tidak nutup di badan
        if (dl.closeDelay > 0f) yield return new WaitForSeconds(dl.closeDelay);

        // tutup semua daun (jika tidak sedang terhalang)
        foreach (var d in dl.doors)
        {
            if (!d) continue;
            if (!d.IsBlocked) d.Close();
        }

        if (log) Debug.Log("[Door] closed after passed");
    }

    static int Side(Vector3 p, Vector3 axis, Vector3 mid)
    {
        // signed: positif/negatif terhadap sumbu link
        Vector3 toP = p - mid; toP.y = 0;
        Vector3 right = new Vector3(-axis.z, 0, axis.x); // 90° dari axis
        float s = Vector3.Dot(toP, right.normalized);
        return s >= 0 ? 1 : -1;
    }

    static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;
        float t = Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f);
        t = Mathf.Clamp01(t);
        Vector3 closest = a + t * ab;
        return Vector3.Distance(p, closest);
    }

    static bool AnyDoorOpenEnough(Sc_pintu[] doors, float minOpen)
    {
        foreach (var d in doors) if (d && (d.IsOpen || d.OpenPercent >= minOpen)) return true;
        return false;
    }
}

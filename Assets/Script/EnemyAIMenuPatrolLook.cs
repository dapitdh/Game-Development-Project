using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAIMenu : MonoBehaviour
{
    [System.Serializable]
    public class PatrolNode
    {
        public Transform point;
        [Tooltip("Diam di titik ini (detik). Diabaikan jika runLookAround = true dan lookAroundDuration > 0")]
        public float dwellTime = 0.5f;

        [Header("Look Around (opsional)")]
        public bool runLookAround = false;
        [Tooltip("Nama state Animator untuk animasi Look Around")]
        public string lookAroundState = "LookAround";
        [Tooltip("Biarkan -1 agar durasi mengikuti panjang clip state; set >0 untuk override manual")]
        public float lookAroundDuration = -1f;

        [Tooltip("Sebelum animasi, hadapkan karakter ke target ini (opsional)")]
        public Transform faceTarget;
        [Tooltip("Kecepatan rotasi saat menghadap target (derajat/detik)")]
        public float faceTurnSpeed = 360f;
    }

    [Header("Patrol Route")]
    public PatrolNode[] nodes;
    public bool pingPongRoute = false;
    public bool randomizeStart = true;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float stoppingDistance = 0.1f;

    [Header("Animation")]
    public string speedParam = "Speed";
    public float maxSpeedForAnim = 4f;
    [Tooltip("Speed animasi saat menyeberang OffMeshLink")]
    public float offMeshAnimSpeed = 0.5f;

    [Header("Gizmos")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private Animator animator;
    private int idx;
    private int dir = 1;
    private bool isBusy; // mencegah double coroutine

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (nodes == null || nodes.Length == 0)
        {
            Debug.LogWarning("[EnemyAIMenuPatrolLook] Nodes kosong.");
            enabled = false;
            return;
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = true;

        if (randomizeStart)
            idx = Random.Range(0, nodes.Length);

        SetDestTo(idx);
    }

    void Update()
    {
        // Update param animasi Speed (0..1)
        float speedPercent = agent.isOnOffMeshLink
            ? offMeshAnimSpeed
            : agent.velocity.magnitude / Mathf.Max(0.01f, maxSpeedForAnim);
        animator.SetFloat(speedParam, speedPercent);

        if (isBusy || agent.pathPending) return;

        // Sudah sampai node?
        if (agent.remainingDistance <= Mathf.Max(stoppingDistance, 0.1f))
        {
            StartCoroutine(HandleArrivalAtNode());
        }
    }

    private IEnumerator HandleArrivalAtNode()
    {
        isBusy = true;
        agent.isStopped = true;

        var node = nodes[idx];
        Debug.Log($"[MenuPatrol] Arrived at node {idx}. runLookAround={node.runLookAround}");

        // Optional: hadap target dulu
        if (node.runLookAround && node.faceTarget != null)
            yield return RotateTowardsTarget(node.faceTarget.position, node.faceTurnSpeed);

        if (node.runLookAround)
        {
            string state = string.IsNullOrEmpty(node.lookAroundState) ? "LookAround" : node.lookAroundState;
            float wait = node.lookAroundDuration > 0f ? node.lookAroundDuration : 0f;

            // Mainkan state secara tegas (hindari nabrak transition)
            Debug.Log($"[MenuPatrol] Play look state: {state}");
            animator.Play(state, 0, 0f); // langsung di-normalizedTime 0

            // Jika durasi belum ditentukan, coba cari clip length
            if (wait <= 0f)
            {
                wait = GetStateLength(animator, state);
                if (wait <= 0f) wait = 1.8f; // fallback aman
            }

            // (Opsional) pastikan balik ke locomotion setelahnya
            yield return new WaitForSeconds(wait);
            animator.CrossFadeInFixedTime("Locomotion", 0.1f);
            Debug.Log($"[MenuPatrol] Finished look, wait={wait}s → locomotion");
        }
        else
        {
            float t = Mathf.Max(0f, node.dwellTime);
            Debug.Log($"[MenuPatrol] Dwell only: {t}s");
            if (t > 0f) yield return new WaitForSeconds(t);
        }

        agent.isStopped = false;
        GoNext();
        isBusy = false;
    }

    private void GoNext()
    {
        if (nodes.Length == 0) return;

        if (pingPongRoute)
        {
            if ((idx == nodes.Length - 1 && dir == 1) || (idx == 0 && dir == -1))
                dir *= -1;
            idx += dir;
        }
        else
        {
            idx = (idx + 1) % nodes.Length;
        }

        SetDestTo(idx);
    }

    private void SetDestTo(int i)
    {
        var n = nodes[i];
        if (n == null || n.point == null)
        {
            Debug.LogWarning($"[EnemyAIMenuPatrolLook] Node {i} null.");
            return;
        }
        agent.speed = patrolSpeed;
        agent.SetDestination(n.point.position);
    }

    private IEnumerator RotateTowardsTarget(Vector3 targetPos, float degreesPerSec)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // putar sampai cukup dekat
        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, degreesPerSec * Time.deltaTime);
            yield return null;
        }
    }

    private float GetStateLength(Animator anim, string stateName, int layer = 0)
    {
        if (string.IsNullOrEmpty(stateName) || anim.runtimeAnimatorController == null)
            return 0f;

        // Cari clip dengan nama persis stateName
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == stateName)
                return clip.length;
        }
        // Jika tak ketemu clip, coba tunggu sampai state aktif lalu baca length
        // (berguna kalau state pakai blend tree atau clip rename)
        var info = anim.GetCurrentAnimatorStateInfo(layer);
        return info.length;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || nodes == null) return;

        for (int i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i];
            if (n == null || n.point == null) continue;

            Gizmos.color = n.runLookAround ? new Color(1f, 0.6f, 0.2f) : Color.yellow;
            Gizmos.DrawSphere(n.point.position, 0.16f);

            int next = (i + 1) % nodes.Length;
            if (next < nodes.Length && nodes[next] != null && nodes[next].point != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(n.point.position, nodes[next].point.position);
            }

            if (n.faceTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(n.point.position, n.faceTarget.position);
            }
        }
    }
}

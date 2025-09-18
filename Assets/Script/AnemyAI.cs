using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;

    [Header("Chase")]
    public float chaseSpeed = 4f;
    public float loseSightDelay = 2f;

    [Header("Vision")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public float eyeHeight = 1.2f;
    public Transform player;

    [Header("Debug")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool isChasing = false;
    private float timeSinceLastSeen = Mathf.Infinity;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null || animator == null)
        {
            Debug.LogError("NavMeshAgent/Animator not found on enemy.");
            enabled = false;
            return;
        }

        agent.speed = patrolSpeed;

        if (waypoints != null && waypoints.Length > 0)
        {
            GoToNextWaypoint();
        }
    }

    void Update()
    {
        if (player == null) return;

        bool canSee = CanSeePlayer();

        if (canSee)
        {
            // start chase
            isChasing = true;
            timeSinceLastSeen = 0f;
            agent.speed = chaseSpeed;

            Vector3 targetPos = player.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(targetPos);
            }
        }
        else
        {
            if (isChasing)
            {
                timeSinceLastSeen += Time.deltaTime;
                if (timeSinceLastSeen >= loseSightDelay)
                {
                    isChasing = false;
                    agent.speed = patrolSpeed;
                    GoToNextWaypoint();
                }
            }
            else
            {
                if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(0.1f, agent.stoppingDistance))
                {
                    GoToNextWaypoint();
                }
            }
        }

        // ✅ Update animasi sesuai kecepatan
        float speedPercent = agent.velocity.magnitude / chaseSpeed;
        animator.SetFloat("Speed", speedPercent);
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    bool CanSeePlayer()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerEye = player.position + Vector3.up * eyeHeight;
        Vector3 dirToPlayer = (playerEye - eyePos).normalized;
        float dist = Vector3.Distance(eyePos, playerEye);

        if (dist > viewRadius) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, viewRadius))
        {
            if (hit.collider != null && (hit.collider.transform == player || hit.collider.CompareTag("Player")))
            {
                return true;
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * eyeHeight, viewRadius);

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 left = DirFromAngle(-viewAngle / 2f);
        Vector3 right = DirFromAngle(viewAngle / 2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(eyePos, eyePos + left * viewRadius);
        Gizmos.DrawLine(eyePos, eyePos + right * viewRadius);
    }

    Vector3 DirFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}

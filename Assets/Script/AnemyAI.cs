using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;

    [Header("Chase")]
    public float chaseSpeed = 4f;
    public float loseSightDelay = 2f; // detik sebelum kembali patrol

    [Header("Vision")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public float eyeHeight = 1.2f; // offset mata dari center capsule
    public Transform player; // assign di inspector (root transform)
    
    [Header("Debug")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isChasing = false;
    private float timeSinceLastSeen = Mathf.Infinity;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent not found on enemy.");
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

            // sample player's nearest NavMesh position (fallback safe)
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
                // keep moving toward last known position OR give time to reacquire
                if (timeSinceLastSeen >= loseSightDelay)
                {
                    // give up, return to patrol
                    isChasing = false;
                    agent.speed = patrolSpeed;
                    GoToNextWaypoint();
                }
                else
                {
                    // optionally keep last destination (agent already moving)
                }
            }
            else
            {
                // still patrolling
                if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(0.1f, agent.stoppingDistance))
                {
                    GoToNextWaypoint();
                }
            }
        }
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

        // Raycast to check line of sight
        if (Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, viewRadius))
        {
            // Pastikan raycast mengenai player (player harus punya collider & tag "Player")
            if (hit.collider != null && hit.collider.transform == player || hit.collider.CompareTag("Player"))
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

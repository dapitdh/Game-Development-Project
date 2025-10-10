using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    //Backsound music & Sound Effect
    [Header("Audio Settings")]
    public AudioSource audioSourceMusic;     // drag AudioSource di inspector
    public AudioSource audioSourceSFX;
    public AudioClip chaseMusic;         // assign clip musik chase
    public AudioClip screamClip;
    public AudioClip[] footstepClips;
    public float musicFadeSpeed = 1.5f;        // lama fade in/out
    private bool isMusicFadingOut = false;
    private Coroutine fadeCoroutine;
    private bool isChasingPlayer = false;
    private bool hasShouted = false;

    //Enemy Ai Settings
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
    // Tambahkan setelah variabel timeSinceLastSeen
    private bool isOnOffMeshLink = false;

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

        if (audioSourceMusic != null)
        {
            audioSourceMusic.loop = true;
            audioSourceMusic.playOnAwake = false;
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

            if (!isChasingPlayer)
            {
                isChasingPlayer = true;
                StartChasing();
            }

            // Teriakan pertama kali lihat player
            if (!hasShouted && screamClip != null && audioSourceSFX != null)
            {
                audioSourceSFX.PlayOneShot(screamClip, 1f);
                hasShouted = true;
            }

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
            if (isChasingPlayer)
            {
                isChasingPlayer = false;
                StopChasing();
            }
            if (isChasing)
            {
                timeSinceLastSeen += Time.deltaTime;
                if (timeSinceLastSeen >= loseSightDelay)
                {
                    isChasing = false;
                    agent.speed = patrolSpeed;
                    GoToNextWaypoint();
                    hasShouted = false;
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

        if (isMusicFadingOut && audioSourceMusic.volume > 0f)
        {
            audioSourceMusic.volume -= Time.deltaTime * musicFadeSpeed;
            if (audioSourceMusic.volume <= 0f)
            {
                audioSourceMusic.Stop();
                isMusicFadingOut = false;
                audioSourceMusic.volume = 1f;
            }
        }

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        float speedPercent;
        
        // Jika sedang melewati NavMesh Link (pintu), paksa jalan pelan
        if (agent.isOnOffMeshLink)
        {
            // Gunakan nilai untuk animasi berjalan normal
            // Sesuaikan nilai ini agar sesuai dengan animator controller Anda
            speedPercent = 0.5f; // Nilai 0.5 = jalan santai
            
            if (!isOnOffMeshLink)
            {
                isOnOffMeshLink = true;
                // Optional: bisa tambahkan logic khusus saat mulai melewati link
            }
        }
        else
        {
            if (isOnOffMeshLink)
            {
                isOnOffMeshLink = false;
                // Optional: logic saat selesai melewati link
            }
            
            // Animasi normal berdasarkan kecepatan aktual
            speedPercent = agent.velocity.magnitude / chaseSpeed;
        }
        
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

    // === AUDIO HANDLER ===

    void StartChasing()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn(audioSourceMusic, chaseMusic));
    }

    void StopChasing()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut(audioSourceMusic));
    }

    IEnumerator FadeIn(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.volume = 0f;
        source.Play();

        float t = 0f;
        while (t < musicFadeSpeed)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, t / musicFadeSpeed);
            yield return null;
        }
        source.volume = 1f;
    }

    IEnumerator FadeOut(AudioSource source)
    {
        float startVolume = source.volume;
        float t = 0f;

        while (t < musicFadeSpeed)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeSpeed);
            yield return null;
        }

        source.Stop();
        source.volume = 1f; // reset volume
    }

}

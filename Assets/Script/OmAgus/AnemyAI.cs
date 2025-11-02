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
    public AudioClip mainClips;
    public AudioClip[] footstepClips;
    private bool isMusicFadingOut = false;
    private bool isChasingPlayer = false;
    private bool hasShouted = false;

    [Range(0.05f, 1.5f)] public float musicFade = 0.15f;

    enum MusicTarget { None, Main, Chase }
    MusicTarget _musicTarget = MusicTarget.None;
    Coroutine _musicCo;
    int _musicTicket = 0;

    //Enemy Ai Settings
    public enum PatrolMode { Sequential, Random }
    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 1.8f;
    public PatrolMode patrolMode = PatrolMode.Random;   // <— default random
    [Range(0f, 1f)] public float backtrackBlock = 1f;   // 1 = never go back to the previous point, 0 = allowed
    public float stuckTimeout = 3f;                     // seconds of low movement = re-pick target
    public float stuckSpeedEps = 0.05f; 

    // internals
    private int currentWaypointIndex = -1;
    private int lastWaypointIndex = -1;
    private float stuckTimer = 0f;

    [Header("Chase")]
    public float chaseSpeed = 4f;
    public float loseSightDelay = 2f;

    [Header("Vision")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public float eyeHeight = 0.8f;
    public Transform player;

    [Header("Debug")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private Animator animator;
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

        // Safety untuk source
        if (audioSourceMusic != null)
        {
            audioSourceMusic.playOnAwake = false;
            audioSourceMusic.loop = true;
            audioSourceMusic.spatialBlend = 0f;
            audioSourceMusic.volume = 1f;
        }

        // Mulai BGM utama
        PlayMainImmediate();
    }

    void Update()
    {
        NavMeshHit sampleHit;
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

            if (AgentReady())
            {
                if (NavMesh.SamplePosition(targetPos, out sampleHit, 1f, NavMesh.AllAreas))
                    agent.SetDestination(sampleHit.position);
                else
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
                if (AgentReady() && !agent.pathPending && agent.remainingDistance <= Mathf.Max(0.1f, agent.stoppingDistance))
                {
                    GoToNextWaypoint();
                }
            }
            // --- STUCK GUARD (only when patrolling) ---
            if (!isChasing)  // only during patrol
            {
                // low speed?
                if (agent.velocity.sqrMagnitude < stuckSpeedEps * stuckSpeedEps)
                    stuckTimer += Time.deltaTime;
                else
                    stuckTimer = 0f;

                if (stuckTimer >= stuckTimeout)
                {
                    // repick a new waypoint to unstick
                    stuckTimer = 0f;
                    GoToNextWaypoint();
                }
            }

        }

        // if (isMusicFadingOut && audioSourceMusic.volume > 0f)
        // {
        //     audioSourceMusic.volume -= Time.deltaTime * musicFadeSpeed;
        //     if (audioSourceMusic.volume <= 0f)
        //     {
        //         audioSourceMusic.Stop();
        //         isMusicFadingOut = false;
        //         audioSourceMusic.volume = 1f;
        //     }
        // }

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        float speedPercent = 0f;

        if (AgentReady())
        {
            if (agent.isOnOffMeshLink)
            {
                speedPercent = 0.5f;   // paksa jalan santai saat lewat link
                if (!isOnOffMeshLink) isOnOffMeshLink = true;
            }
            else
            {
                if (isOnOffMeshLink) isOnOffMeshLink = false;
                speedPercent = agent.velocity.magnitude / chaseSpeed;
            }
        }

        // clamp + damping biar smooth
        animator.SetFloat("Speed", Mathf.Clamp01(speedPercent), 0.1f, Time.deltaTime);
    }

    void GoToNextWaypoint()
    {
        if (!AgentReady() || waypoints == null || waypoints.Length == 0) return;

        // Tentukan index berikutnya (tanpa mengubah current dulu)
        int next = (patrolMode == PatrolMode.Sequential)
            ? NextSequential()
            : NextRandomNoImmediateRepeat();

        // Update state index
        lastWaypointIndex = currentWaypointIndex;
        currentWaypointIndex = next;

        // Set tujuan sekali saja (sample navmesh dulu)
        Vector3 dst = waypoints[currentWaypointIndex].position;
        if (NavMesh.SamplePosition(dst, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(dst);

        stuckTimer = 0f;
    }

    int NextSequential()
    {
        if (currentWaypointIndex < 0) return 0;
        return (currentWaypointIndex + 1) % waypoints.Length;
    }

    int NextRandomNoImmediateRepeat()
    {
        if (waypoints.Length == 1) return 0;

        int pick = currentWaypointIndex;
        int guard = 0;

        // avoid immediate repeat and (optionally) avoid backtracking to last point
        while (pick == currentWaypointIndex || (backtrackBlock >= 0.99f && pick == lastWaypointIndex))
        {
            pick = Random.Range(0, waypoints.Length);
            if (++guard > 20) break; // safety
        }
        return pick;
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

    bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    // === AUDIO HANDLER ===
    void PlayMainImmediate()
    {
        if (!audioSourceMusic || !mainClips) return;
        _musicTarget = MusicTarget.Main;
        audioSourceMusic.clip = mainClips;
        audioSourceMusic.volume = 1f;
        if (!audioSourceMusic.isPlaying) audioSourceMusic.Play();
    }

    public void StartChasing()
    {
        SetMusic(MusicTarget.Chase);
    }

    public void StopChasing()
    {
        SetMusic(MusicTarget.Main);
    }

    void SetMusic(MusicTarget target)
    {
        if (target == _musicTarget) return;
        _musicTarget = target;

        if (_musicCo != null) StopCoroutine(_musicCo);
        _musicCo = StartCoroutine(CoSwitchMusic(target));
    }

    public void StopAllMusicImmediate()
    {
        if (audioSourceMusic && audioSourceMusic.isPlaying)
        {
            audioSourceMusic.Stop();
            audioSourceMusic.volume = 1f; // reset volume agar siap dipakai lagi nanti
        }
    }

    System.Collections.IEnumerator CoSwitchMusic(MusicTarget target)
    {
        int my = ++_musicTicket;

        AudioClip next = (target == MusicTarget.Main) ? mainClips : chaseMusic;
        if (!audioSourceMusic || !next) yield break;

        float t = 0f;
        float dur = Mathf.Max(0.05f, musicFade);
        float v0 = audioSourceMusic.volume;

        // Fade out
        while (t < dur)
        {
            if (my != _musicTicket) yield break; // superseded
            t += Time.deltaTime;
            audioSourceMusic.volume = Mathf.Lerp(v0, 0f, t / dur);
            yield return null;
        }
        audioSourceMusic.volume = 0f;

        // Ganti clip & play
        audioSourceMusic.clip = next;
        if (!audioSourceMusic.isPlaying) audioSourceMusic.Play();

        // Fade in
        t = 0f;
        while (t < dur)
        {
            if (my != _musicTicket) yield break; // superseded
            t += Time.deltaTime;
            audioSourceMusic.volume = Mathf.Lerp(0f, 1f, t / dur);
            yield return null;
        }
        audioSourceMusic.volume = 1f;
    }
}

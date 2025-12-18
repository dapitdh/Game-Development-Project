using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class StunTarget : MonoBehaviour, IStunnable
{
    [Header("Stun")]
    public float defaultDuration = 2f;
    public bool stackDuration = true;              
    public bool freezeMovement = true;            
    public bool disableNavMeshAgent = true;
    public bool disableScripts = true;
    public Behaviour[] extraBehavioursToDisable;   
    public Animator animator;
    public string stunBoolParam = "stun";
    public ParticleSystem stunVfx;

    [Header("Knockback")]
    public Rigidbody rb;
    public float dragWhileStunned = 6f;           

    public bool IsStunned { get; private set; }

    float remaining;
    NavMeshAgent agent;
    float agentSpeed, agentAccel;
    float rbDragOriginal;
    Coroutine routine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (rb) rbDragOriginal = rb.linearDamping;

        if (agent)
        {
            agentSpeed = agent.speed;
            agentAccel = agent.acceleration;
        }
    }

    public void Stun(float duration, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator, float force)
    {
        float d = (duration > 0f) ? duration : defaultDuration;

        if (IsStunned)
        {
            if (stackDuration)
                remaining += d;    
            else
                return;            
        }
        else
        {
            remaining = d;
            if (routine == null) routine = StartCoroutine(StunRoutine());
        }

        if (rb && force > 0f)
        {
            Vector3 pushDir = -hitNormal;
            rb.AddForce(pushDir * force, ForceMode.Impulse);
        }
    }


    IEnumerator StunRoutine()
    {
        IsStunned = true;

        if (stunVfx) stunVfx.Play();
        if (animator && !string.IsNullOrEmpty(stunBoolParam))
            animator.SetBool(stunBoolParam, true);

        if (disableNavMeshAgent && agent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        if (disableScripts && extraBehavioursToDisable != null)
            foreach (var b in extraBehavioursToDisable) if (b) b.enabled = false;

        if (freezeMovement && rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = dragWhileStunned;
        }

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (stunVfx) stunVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (animator && !string.IsNullOrEmpty(stunBoolParam))
            animator.SetBool(stunBoolParam, false);

        if (disableNavMeshAgent && agent)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.speed = agentSpeed;
            agent.acceleration = agentAccel;
        }

        if (disableScripts && extraBehavioursToDisable != null)
            foreach (var b in extraBehavioursToDisable) if (b) b.enabled = true;

        if (freezeMovement && rb)
            rb.linearDamping = rbDragOriginal;

        IsStunned = false;
        routine = null;
    }
}

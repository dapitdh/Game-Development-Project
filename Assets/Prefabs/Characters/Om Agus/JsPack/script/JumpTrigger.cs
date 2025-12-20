using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class JumpTrigger : MonoBehaviour
{
    public AudioSource JumpSfx;
    public GameObject JumpAgus; 
    public GameObject FlashImg;  
    public Transform respawnPoint;
    private Vector3 _respawnPos;
    private Quaternion _respawnRot;

    void Awake()
    {
        var pc = FindObjectOfType<FPP.Puan_control>(true);
        if (respawnPoint == null && pc)
            respawnPoint = pc.transform;

        if (respawnPoint != null)
        {
            _respawnPos = respawnPoint.position;
            _respawnRot = respawnPoint.rotation;
        }
        else
        {
            _respawnPos = Vector3.zero;
            _respawnRot = Quaternion.identity;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return; 

        SetOmAgusControl(false);
        if (JumpSfx) JumpSfx.Play();
        if (JumpAgus) JumpAgus.SetActive(true);
        if (FlashImg) FlashImg.SetActive(true);

        SetPlayerControl(false);
        SetCursor(false);

        StartCoroutine(EndJump());
    }

    IEnumerator EndJump()
    {
       yield return new WaitForSeconds(3.8f);

        if (JumpAgus) JumpAgus.SetActive(false);
        if (FlashImg) FlashImg.SetActive(false);

        var pc = FindObjectOfType<FPP.Puan_control>(true);
        if (pc)
        {
            var t = pc.transform;
            var cc = pc.GetComponent<CharacterController>();
            var rb = pc.GetComponent<Rigidbody>();
            
            if (cc) cc.enabled = false;

            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            t.position = _respawnPos;
            t.rotation = _respawnRot;

            if (cc) cc.enabled = true;
        }
        var omAgus = FindObjectOfType<EnemyAI>(true);
        if (omAgus)
        {
            omAgus.ResetAfterJumpscare();
            SetOmAgusControl(true);
        }
        SetPlayerControl(true);
        SetCursor(false);
    }

    void SetPlayerControl(bool enabled)
    {
        var pc = FindObjectOfType<FPP.Puan_control>(true);
        if (pc) pc.enabled = enabled;

        var ch = pc ? pc.GetComponent<CharacterController>() : null;
        if (ch) ch.enabled = enabled;

        var rb = pc ? pc.GetComponent<Rigidbody>() : null;
        if (rb)
        {
            if (!enabled)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void SetOmAgusControl(bool enabled)
    {
        var omAgus = FindObjectOfType<EnemyAI>(true);
        if (!omAgus) return;

        omAgus.StopChasing();
        if (omAgus) omAgus.StopAllMusicImmediate();

        var rb = omAgus.GetComponent<Rigidbody>();
        if (rb)
        {
            if (!enabled)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        var agent = omAgus.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent)
        {
            if (!enabled)
            {
                if (agent.enabled)
                    agent.ResetPath();

                agent.enabled = false;
            }
            else
            {
                if (!agent.enabled) agent.enabled = true;

                if (!agent.isOnNavMesh)
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(
                            omAgus.transform.position,
                            out var hit,
                            2f,
                            UnityEngine.AI.NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }
            }
        }

        var anim = omAgus.GetComponent<Animator>();
        if (anim) anim.enabled = enabled;

        foreach (var r in omAgus.GetComponentsInChildren<Renderer>(true))
            r.enabled = enabled;
    }

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class JumpTrigger : MonoBehaviour
{
    public AudioSource JumpSfx; // AudioSource di child "Audio"
    public GameObject JumpAgus; // ParticleSystem di child "JumpEffect"
    public GameObject FlashImg;  

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
        yield return new WaitForSeconds(4f);
        var scene = SceneManager.GetActiveScene().name;
        JumpAgus.SetActive(false);
        FlashImg.SetActive(false);
        SceneManager.LoadScene(scene);
        SetCursor(true);
        SetPlayerControl(true);
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
            if (!enabled) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = !enabled;
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
            if (!enabled) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = !enabled;
        }

        var agent = omAgus.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent)
        {
            if (!enabled)
            {
                if (agent.enabled)
                {
                    agent.ResetPath();
                }
                agent.enabled = false;  // <- agent off supaya AI tidak gerak
            }
            else
            {
                // aktifkan kembali dan pastikan ditempatkan di NavMesh
                if (!agent.enabled) agent.enabled = true;

                // pastikan posisinya valid di NavMesh (warp ke titik terdekat jika perlu)
                if (!agent.isOnNavMesh)
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(omAgus.transform.position, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                        agent.Warp(hit.position);
                }
            }
        }

        // matikan/nyalakan visual & animasi
        var anim = omAgus.GetComponent<Animator>(); if (anim) anim.enabled = enabled;
        foreach (var r in omAgus.GetComponentsInChildren<Renderer>(true)) r.enabled = enabled;
        }

        void SetCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
}

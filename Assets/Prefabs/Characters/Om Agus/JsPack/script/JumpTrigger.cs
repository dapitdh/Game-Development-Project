using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class JumpTrigger : MonoBehaviour
{
    public AudioSource JumpSfx; // AudioSource di child "Audio"
    public GameObject JumpAgus; // ParticleSystem di child "JumpEffect"
    public GameObject FlashImg;  

    void OnTriggerEnter()
    {   
        SetOmAgusControl(false);
        JumpSfx.Play();
        JumpAgus.SetActive(true);
        FlashImg.SetActive(true);
        SetPlayerControl(false);
        SetCursor(false);
        StartCoroutine(EndJump());
    }

    IEnumerator EndJump()
    {
        yield return new WaitForSeconds(3f);
        var scene = SceneManager.GetActiveScene().name;
        JumpAgus.SetActive(false);
        FlashImg.SetActive(false);
        SceneManager.LoadScene(scene);
        SetCursor(true);
        SetPlayerControl(true);
    }

    void SetPlayerControl(bool enabled)
    {
        // cari controller player-mu (dalam namespace FPP)
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
        omAgus.StopChasing();
        if (omAgus) omAgus.enabled = enabled;
    }

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

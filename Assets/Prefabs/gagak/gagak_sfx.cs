using UnityEngine;

public class gagak_sfx : MonoBehaviour
{
    [SerializeField] AudioSource sumberSuara;
    [SerializeField] AudioClip clipMusik1;
    [SerializeField] AudioClip clipMusik2;
    void Start()
    {
        sumberSuara.volume = 0.5f;
        sumberSuara.clip = clipMusik1;
        sumberSuara.loop = true;
        sumberSuara.Play();
    }
    public void rubah_musik1()
    {
        sumberSuara.Stop();
        sumberSuara.clip = clipMusik1;
        sumberSuara.Play();

    }
    public void rubah_musik2()
    {
        sumberSuara.Stop();
        sumberSuara.clip = clipMusik2;
        sumberSuara.Play();

    }
}
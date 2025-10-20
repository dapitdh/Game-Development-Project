using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuPause : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI Roots")]
    public GameObject pauseMenuUI;    // CanvasPauseMenu
    public GameObject pauseMainPanel; // PauseMenu
    public GameObject settingsPanel;  // SettingsPanel

    // --- BGM ---
    [Header("BGM")]
    public AudioSource bgmSource;      // drag: BGM_Menu (AudioSource di Canvas)
    public float bgmFadeIn = 0.8f;    // detik
    public float bgmFadeOut = 0.4f;    // detik

    // --- SFX UI ---
    [Header("SFX UI")]
    public AudioSource uiSfxSource;    // drag: AudioSource SFX UI
    public AudioClip sfxClick;
    public AudioClip sfxHover;         // opsional
    public float sfxVolume = 0.9f;

    // ===== private =====
    Coroutine _bgmFadeCo;
    float _bgmBaseVol = 1f;

    void Awake()
    {
        // State awal
        pauseMenuUI.SetActive(false);
        pauseMainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameIsPaused = false;

        if (bgmSource)
        {
            _bgmBaseVol = bgmSource.volume;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            if (bgmSource.isPlaying) bgmSource.Stop(); // BGM menu hanya saat paused
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!GameIsPaused)
            {
                Pause();
            }
            else
            {
                // Jika sedang di Settings -> kembali ke PauseMenu
                if (settingsPanel.activeSelf) BackFromSettings();
                else Resume();
            }
        }
    }

    // ---------------- UI SFX helpers ----------------
    void PlayClickSfx() { if (uiSfxSource && sfxClick) uiSfxSource.PlayOneShot(sfxClick, sfxVolume); }
    void PlayHoverSfx() { if (uiSfxSource && sfxHover) uiSfxSource.PlayOneShot(sfxHover, 0.8f * sfxVolume); }

    // Expose utk EventTrigger/Button OnClick
    public void OnUiHover() => PlayHoverSfx();
    public void OnUiClick() => PlayClickSfx();

    // ---------------- Pause flow ----------------
    public void Resume()
    {
        PlayClickSfx();

        Time.timeScale = 1f;
        GameIsPaused = false;

        settingsPanel.SetActive(false);
        pauseMainPanel.SetActive(false);
        pauseMenuUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FadeBgm(false); // fade out
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        pauseMainPanel.SetActive(true);
        settingsPanel.SetActive(false);

        Time.timeScale = 0f;
        GameIsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // (Opsional) fokuskan tombol pertama
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(pauseMainPanel.transform.GetChild(0).gameObject);

        FadeBgm(true); // fade in
    }

    // Dipanggil tombol "Settings"
    public void LoadMenu() => OpenSettings();

    public void OpenSettings()
    {
        PlayClickSfx();

        pauseMainPanel.SetActive(false);
        settingsPanel.SetActive(true);

        if (EventSystem.current)
        {
            var back = settingsPanel.transform.Find("Btn_Back")?.gameObject;
            if (back) EventSystem.current.SetSelectedGameObject(back);
        }
        // BGM tetap menyala saat di Settings
    }

    // Dipanggil tombol "Back" di Settings
    public void BackFromSettings()
    {
        PlayClickSfx();

        settingsPanel.SetActive(false);
        pauseMainPanel.SetActive(true);

        if (EventSystem.current)
        {
            var resume = pauseMainPanel.transform.Find("Resume")?.gameObject; // pastikan child bernama "Resume"
            if (resume) EventSystem.current.SetSelectedGameObject(resume);
        }
    }

    public void QuitGame()
    {
        PlayClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ---------------- BGM fade helpers ----------------
    void FadeBgm(bool fadeIn)
    {
        if (!bgmSource) return;

        if (_bgmFadeCo != null) StopCoroutine(_bgmFadeCo);

        if (fadeIn)
        {
            if (!bgmSource.isPlaying) bgmSource.Play();
            _bgmFadeCo = StartCoroutine(FadeAudio(bgmSource, bgmFadeIn, _bgmBaseVol, false));
        }
        else
        {
            _bgmFadeCo = StartCoroutine(FadeAudio(bgmSource, bgmFadeOut, 0f, true));
        }
    }

    IEnumerator FadeAudio(AudioSource src, float dur, float target, bool stopWhenDone)
    {
        float start = src.volume;
        float t = 0f;
        if (dur <= 0f) dur = 0.001f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur; // tetap jalan saat Time.timeScale=0
            src.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        src.volume = target;

        if (stopWhenDone && target <= 0.0001f) src.Stop();
        _bgmFadeCo = null;
    }
}

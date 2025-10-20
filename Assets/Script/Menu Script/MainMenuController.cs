using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons (Main)")]
    public Button btnPlay;
    public Button btnHowToPlay;
    public Button btnSettings;
    public Button btnCredits;
    public Button btnQuit;

    [Header("Panels")]
    public GameObject leftPanel;      // kolom kiri (main menu)
    public GameObject settingsPanel;  // default inactive
    public GameObject howToPlayPanel;  // default inactive
    public GameObject creditsPanel;   // default inactive

    [Header("Back Buttons")]
    public Button btnBackHowToPlay;
    public Button btnBackSettings;
    public Button btnBackCredits;

    [Header("Selection")]
    public GameObject firstSelected;  // set ke Btn_Play

    [Header("Gameplay")]
    public string gameplaySceneName = "HouseSceneVedian";


    [Header("Fade")]
    public float fadeTime = 0.2f;
    public CanvasGroup fadeOverlay;

    // --- BGM ---
    [Header("BGM")]
    public AudioSource bgmSource;      // drag: BGM_Menu
    public float bgmFadeIn = 0.8f;     // detik
    public float bgmFadeOut = 0.4f;

    [Header("SFX UI")]
    public AudioSource uiSfxSource;
    public AudioClip sfxClick;
    public AudioClip sfxHover;          // (opsional) hover.wav
    public float sfxVolume = 0.9f;

    void PlayClickSfx() { if (uiSfxSource && sfxClick) uiSfxSource.PlayOneShot(sfxClick, sfxVolume); }
    void PlayHoverSfx() { if (uiSfxSource && sfxHover) uiSfxSource.PlayOneShot(sfxHover, 0.8f * sfxVolume); }


    void Awake()
    {
        HookButtons();

        // Keadaan awal
        EnsureCanvasGroup(leftPanel).alpha = 1f;
        EnsureCanvasGroup(leftPanel).interactable = true;
        EnsureCanvasGroup(leftPanel).blocksRaycasts = true;

        if (howToPlayPanel) { howToPlayPanel.SetActive(false); var cg = EnsureCanvasGroup(howToPlayPanel); cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
        if (settingsPanel) { settingsPanel.SetActive(false); var cg = EnsureCanvasGroup(settingsPanel); cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
        if (creditsPanel) { creditsPanel.SetActive(false); var cg = EnsureCanvasGroup(creditsPanel); cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }

        if (fadeOverlay) { fadeOverlay.alpha = 0; fadeOverlay.gameObject.SetActive(true); }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;
    }


    void OnEnable()
    {
        if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (howToPlayPanel && howToPlayPanel.activeSelf) { BackToMain(); return; }
            if (settingsPanel && settingsPanel.activeSelf) { BackToMain(); return; }
            if (creditsPanel && creditsPanel.activeSelf) { BackToMain(); return; }
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var sel = EventSystem.current.currentSelectedGameObject;
            var btn = sel ? sel.GetComponent<Button>() : null;
            if (btn) btn.onClick.Invoke();
        }
    }

    void Start()
    {
        // mulai BGM dengan fade in
        if (bgmSource && bgmSource.clip && !bgmSource.isPlaying)
            StartCoroutine(FadeAudio(bgmSource, target: 1f, time: bgmFadeIn, startFromZero: true));
    }

    System.Collections.IEnumerator FadeAudio(AudioSource src, float target, float time, bool startFromZero = false)
    {
        if (!src) yield break;
        if (startFromZero) { src.volume = 0f; src.Play(); }
        float start = src.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = t / Mathf.Max(0.0001f, time);
            // ease
            k = 1f - (1f - k) * (1f - k);
            src.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }
        src.volume = target;
        if (Mathf.Approximately(target, 0f)) src.Stop();
    }

    // === UI EVENTS ===
    void HookButtons()
    {
        if (btnPlay) { btnPlay.onClick.AddListener(PlayClickSfx); btnPlay.onClick.AddListener(OnPlay); }
        if (btnHowToPlay) { btnHowToPlay.onClick.AddListener(PlayClickSfx); btnHowToPlay.onClick.AddListener(() => OpenPanel(howToPlayPanel)); }
        if (btnSettings) { btnSettings.onClick.AddListener(PlayClickSfx); btnSettings.onClick.AddListener(() => OpenPanel(settingsPanel)); }
        if (btnCredits) { btnCredits.onClick.AddListener(PlayClickSfx); btnCredits.onClick.AddListener(() => OpenPanel(creditsPanel)); }
        if (btnQuit) { btnQuit.onClick.AddListener(PlayClickSfx); btnQuit.onClick.AddListener(OnQuit); }

        if (btnBackHowToPlay)  { btnBackHowToPlay.onClick.AddListener(PlayClickSfx); btnBackHowToPlay.onClick.AddListener(BackToMain); }
        if (btnBackSettings) { btnBackSettings.onClick.AddListener(PlayClickSfx); btnBackSettings.onClick.AddListener(BackToMain); }
        if (btnBackCredits) { btnBackCredits.onClick.AddListener(PlayClickSfx); btnBackCredits.onClick.AddListener(BackToMain); }

        // (Opsional) bunyi saat hover/focus pakai EventTrigger
        AddHoverSfx(btnPlay); AddHoverSfx(btnHowToPlay); AddHoverSfx(btnSettings); AddHoverSfx(btnCredits); AddHoverSfx(btnQuit);
        if (btnBackHowToPlay) AddHoverSfx(btnBackHowToPlay);
        if (btnBackSettings) AddHoverSfx(btnBackSettings);
        if (btnBackCredits) AddHoverSfx(btnBackCredits);
    }

    void AddHoverSfx(Button b)
    {
        if (!b || !sfxHover) return;
        var et = b.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (!et) et = b.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entry.callback.AddListener(_ => PlayHoverSfx());
        et.triggers.Add(entry);

        // biar keyboard navigation juga bunyi:
        var sel = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.Select
        };
        sel.callback.AddListener(_ => PlayHoverSfx());
        et.triggers.Add(sel);
    }

    void OpenPanel(GameObject panel)
    {
        if (!panel) return;
        // Hide LeftPanel (fade out), then show target panel (fade in)
        var left = EnsureCanvasGroup(leftPanel);
        var tgt = EnsureCanvasGroup(panel);
        panel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeOut(left, fadeTime, () =>
        {
            leftPanel.SetActive(false);
            StartCoroutine(FadeIn(tgt, fadeTime, () =>
            {
                // fokus tombol pertama di panel
                var firstBtn = panel.GetComponentInChildren<Button>();
                if (firstBtn) EventSystem.current.SetSelectedGameObject(firstBtn.gameObject);
            }));
        }));
    }

    void BackToMain()
    {
        // Hide whichever panel is open, then show LeftPanel
        GameObject open = null;
        if (howToPlayPanel && howToPlayPanel.activeSelf) open = howToPlayPanel;
        else if (settingsPanel && settingsPanel.activeSelf) open = settingsPanel;
        else if (creditsPanel && creditsPanel.activeSelf) open = creditsPanel;
        if (open == null) return;

        var openCg = EnsureCanvasGroup(open);
        var leftCg = EnsureCanvasGroup(leftPanel);
        StopAllCoroutines();
        StartCoroutine(FadeOut(openCg, fadeTime, () =>
        {
            open.SetActive(false);
            leftPanel.SetActive(true);
            StartCoroutine(FadeIn(leftCg, fadeTime, () =>
            {
                if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
            }));
        }));
    }

    void OnPlay()
    {
        // Optional: fade to black sebelum load
        StopAllCoroutines();

        System.Action go = () => SceneManager.LoadScene(gameplaySceneName);

        if (bgmSource) StartCoroutine(FadeAudio(bgmSource, 0f, bgmFadeOut));

        if (fadeOverlay)
            StartCoroutine(FadeIn(fadeOverlay, 0.25f, () => go(), useRaycast: false));
        else
            go();
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // === FADING HELPERS ===
    CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go ? go.GetComponent<CanvasGroup>() : null;
        if (!cg && go) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    System.Collections.IEnumerator FadeIn(CanvasGroup cg, float time, System.Action onDone = null, bool useRaycast = true)
    {
        if (!cg) yield break;
        cg.gameObject.SetActive(true);
        cg.blocksRaycasts = false;
        cg.interactable = false;

        float t = 0f, start = cg.alpha, end = 1f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, EaseOutQuad(t / time));
            yield return null;
        }
        cg.alpha = 1f;
        cg.blocksRaycasts = useRaycast;
        cg.interactable = true;
        onDone?.Invoke();
    }

    System.Collections.IEnumerator FadeOut(CanvasGroup cg, float time, System.Action onDone = null)
    {
        if (!cg) yield break;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        float t = 0f, start = cg.alpha, end = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, EaseInQuad(t / time));
            yield return null;
        }
        cg.alpha = 0f;
        onDone?.Invoke();
    }

    // Easing ringan biar manis
    float EaseInQuad(float x) => x * x;
    float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}

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

    [Header("Panels (Roots)")]
    public GameObject leftPanel;       // main menu
    public GameObject settingsPanel;   // default inactive
    public GameObject howToPlayPanel;  // default inactive
    public GameObject creditsPanel;    // default inactive
    public GameObject modeSelectPanel;

    [Header("Back Buttons")]
    public Button btnBackHowToPlay;
    public Button btnBackSettings;
    public Button btnBackCredits;

    [Header("Selection")]
    public GameObject firstSelected;   // set ke Btn_Play

    [Header("Gameplay")]
    public string gameplaySceneName = "HouseScene";
    public string gameplaySceneName2 = "HouseScene_NoMercy";

    [Header("Game Mode Select")]
    public Button btnModeCalm;
    public Button btnModeNoMercy;
    public Button btnModeBack;

    // key utk simpan mode ke PlayerPrefs (bisa dipakai di HouseScene)
    public string difficultyPrefKey = "OmAgus_Difficulty";

    [Header("Fade Overlay")]
    public float fadeTime = 0.2f;
    public CanvasGroup fadeOverlay;

    // --- BGM ---
    [Header("BGM")]
    public AudioSource bgmSource;      // drag: BGM_Menu
    public float bgmFadeIn = 0.8f;
    public float bgmFadeOut = 0.4f;

    [Header("SFX UI")]
    public AudioSource uiSfxSource;
    public AudioClip sfxClick;
    public AudioClip sfxHover;
    public float sfxVolume = 0.9f;

    // ---------- iTween Anim Settings ----------
    [Header("Intro Tween (Title/Buttons/Window)")]
    public RectTransform titleRT;           // “OM AGUS”
    public RectTransform buttonGroupRT;     // parent tombol
    public RectTransform windowFrameRightRT;// opsional, bingkai/jendela
    public float introDelay = 0.2f;
    public float titleDropTime = 0.9f;
    public string easeTitle = "easeOutBounce";
    public float buttonStagger = 0.07f;
    public string easeButtons = "easeOutBack";

    [Header("Panel Switch Tween")]
    public RectTransform leftPanelRT;
    public RectTransform settingsPanelRT;
    public RectTransform howToPanelRT;
    public RectTransform creditsPanelRT;
    public RectTransform modeSelectPanelRT;
    public float slideDist = 620f;
    public float slideTime = 0.35f;
    public float panelFadeTime = 0.25f;
    public string easeOut = "easeInOutCubic";
    public string easeIn = "easeInOutCubic";

    // state
    bool busySwitch;
    bool playedIntro;
    System.Action _pendingFinalize;

    // ---------------------------------------------------

    void PlayClickSfx() { if (uiSfxSource && sfxClick) uiSfxSource.PlayOneShot(sfxClick, sfxVolume); }
    void PlayHoverSfx() { if (uiSfxSource && sfxHover) uiSfxSource.PlayOneShot(sfxHover, 0.8f * sfxVolume); }

    // --- iTween callback targets (temporary holders) ---
    CanvasGroup _twCgA, _twCgB;
    RectTransform _twRt;

    // iTween will call these by name (string)
    void ITween_SetAlphaA(float v) { if (_twCgA) _twCgA.alpha = v; }
    void ITween_SetAlphaB(float v) { if (_twCgB) _twCgB.alpha = v; }
    void ITween_SetRT_Y(float y)
    {
        if (_twRt)
        {
            var p = _twRt.anchoredPosition;
            p.y = y;
            _twRt.anchoredPosition = p;
        }
    }

    void Awake()
    {
        HookButtons();

        // Keadaan awal (CanvasGroup dipastikan ada)
        var leftCG = EnsureCanvasGroup(leftPanel); leftCG.alpha = 1f; leftCG.interactable = true; leftCG.blocksRaycasts = true;

        if (howToPlayPanel) { howToPlayPanel.SetActive(false); var cg = EnsureCanvasGroup(howToPlayPanel); cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
        if (settingsPanel) { settingsPanel.SetActive(false); var cg = EnsureCanvasGroup(settingsPanel); cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
        if (creditsPanel) { creditsPanel.SetActive(false); var cg = EnsureCanvasGroup(creditsPanel); cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }

        if (fadeOverlay) { fadeOverlay.alpha = 0; fadeOverlay.gameObject.SetActive(true); }

        if (modeSelectPanel)
        {
            modeSelectPanel.SetActive(false);
            var cg = EnsureCanvasGroup(modeSelectPanel);
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;

    }
    
    // --- cache posisi home tiap panel ---
    Vector2 _homeLeft, _homeSettings, _homeHowTo, _homeCredits, _homeModeSelect;

    RectTransform GetOrSelfRT(GameObject go, RectTransform prefer)
    {
        return prefer ? prefer : (go ? go.GetComponent<RectTransform>() : null);
    }

    void CacheHomePositions()
    {
        // Pastikan field RT menunjuk ke RT yang benar
        leftPanelRT     = GetOrSelfRT(leftPanel,     leftPanelRT);
        settingsPanelRT = GetOrSelfRT(settingsPanel, settingsPanelRT);
        howToPanelRT    = GetOrSelfRT(howToPlayPanel,howToPanelRT);
        creditsPanelRT  = GetOrSelfRT(creditsPanel,  creditsPanelRT);
        modeSelectPanelRT = GetOrSelfRT(modeSelectPanel, modeSelectPanelRT);

        _homeLeft     = leftPanelRT     ? leftPanelRT.anchoredPosition     : Vector2.zero;
        _homeSettings = settingsPanelRT ? settingsPanelRT.anchoredPosition : Vector2.zero;
        _homeHowTo    = howToPanelRT    ? howToPanelRT.anchoredPosition    : Vector2.zero;
        _homeCredits  = creditsPanelRT  ? creditsPanelRT.anchoredPosition  : Vector2.zero;
        _homeModeSelect   = modeSelectPanelRT ? modeSelectPanelRT.anchoredPosition : Vector2.zero;

        // Debug bantu verifikasi di Console:
    #if UNITY_EDITOR
        Debug.Log($"[Menu] Home Left:     {_homeLeft}  (RT: {leftPanelRT?.name})");
        Debug.Log($"[Menu] Home Settings: {_homeSettings} (RT: {settingsPanelRT?.name})");
        Debug.Log($"[Menu] Home HowTo:    {_homeHowTo}    (RT: {howToPanelRT?.name})");
        Debug.Log($"[Menu] Home Credits:  {_homeCredits}  (RT: {creditsPanelRT?.name})");
        Debug.Log($"[Menu] Home ModeSelect: {_homeModeSelect} (RT: {modeSelectPanelRT?.name})");
    #endif
    }

    Vector2 GetHomePos(GameObject go)
    {
        if (go == leftPanel)      return _homeLeft;
        if (go == settingsPanel)  return _homeSettings;
        if (go == howToPlayPanel) return _homeHowTo;
        if (go == creditsPanel)   return _homeCredits;
        if (go == modeSelectPanel) return _homeModeSelect;
        var rt = GetRT(go);
        return rt ? rt.anchoredPosition : Vector2.zero;
    }


    void OnEnable()
    {
        if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void Start()
    {
        // BGM masuk fade in
        if (bgmSource && bgmSource.clip && !bgmSource.isPlaying)
            StartCoroutine(FadeAudio(bgmSource, 1f, bgmFadeIn, startFromZero: true));

        CacheHomePositions();
        // Intro tween satu kali
        if (!playedIntro) { playedIntro = true; StartCoroutine(CoIntroTween()); }
    }

    void Update()
    {
        if (modeSelectPanel && modeSelectPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideModePanel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (howToPlayPanel && howToPlayPanel.activeSelf) { BackToMain(); return; }
            if (settingsPanel && settingsPanel.activeSelf)   { BackToMain(); return; }
            if (creditsPanel && creditsPanel.activeSelf)     { BackToMain(); return; }
            if (modeSelectPanel && modeSelectPanel.activeSelf) { BackToMain(); return; }
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var sel = EventSystem.current.currentSelectedGameObject;
            var btn = sel ? sel.GetComponent<Button>() : null;
            if (btn) btn.onClick.Invoke();
        }
    }

    // -------------------- INTRO TWEEN --------------------
    System.Collections.IEnumerator CoIntroTween()
    {
        // fade-in LeftPanel pelan biar manis
        var leftCG = EnsureCanvasGroup(leftPanel);
        leftCG.alpha = 0f;
        _twCgA = leftCG;
        iTween.ValueTo(gameObject, iTween.Hash(
            "from", 0f, "to", 1f, "time", 0.25f,
            "delay", introDelay,
            "easetype", iTween.EaseType.linear,
            "onupdate", "ITween_SetAlphaA"   // <- string name
        ));

        yield return new WaitForSeconds(introDelay);

        // Title turun dari atas
        if (titleRT)
        {
            var startY = titleRT.anchoredPosition.y + 220f;
            var endY   = titleRT.anchoredPosition.y;
            titleRT.anchoredPosition = new Vector2(titleRT.anchoredPosition.x, startY);

            _twRt = titleRT;
            iTween.ValueTo(gameObject, iTween.Hash(
                "from", startY, "to", endY, "time", titleDropTime,
                "easetype", easeTitle,
                "onupdate", "ITween_SetRT_Y"  // <- string name
            ));
        }


        // Tombol scale-in staggered
        if (buttonGroupRT)
        {
            for (int i = 0; i < buttonGroupRT.childCount; i++)
            {
                var b = buttonGroupRT.GetChild(i) as RectTransform;
                if (!b) continue;
                b.localScale = Vector3.one * 0.5f;
                iTween.ScaleTo(b.gameObject, iTween.Hash(
                    "scale", Vector3.one,
                    "time", 0.35f,
                    "delay", 0.2f + i * buttonStagger,
                    "easetype", easeButtons
                ));
            }
        }

        // Parallax halus window kanan (opsional)
        if (windowFrameRightRT)
        {
            var p0 = windowFrameRightRT.anchoredPosition;
            windowFrameRightRT.anchoredPosition = p0 + new Vector2(30f, 0f);
            iTween.MoveTo(windowFrameRightRT.gameObject, iTween.Hash(
                "islocal", true, "time", 1.2f, "easetype", "easeOutCubic", "position", (Vector3)p0
            ));
        }
    }

    // -------------------- UI EVENTS --------------------
    void HookButtons()
    {
        if (btnPlay)     { btnPlay.onClick.AddListener(PlayClickSfx);     btnPlay.onClick.AddListener(OnPlay); }
        if (btnHowToPlay){ btnHowToPlay.onClick.AddListener(PlayClickSfx);btnHowToPlay.onClick.AddListener(() => OpenPanel(howToPlayPanel)); }
        if (btnSettings) { btnSettings.onClick.AddListener(PlayClickSfx);  btnSettings.onClick.AddListener(() => OpenPanel(settingsPanel)); }
        if (btnCredits)  { btnCredits.onClick.AddListener(PlayClickSfx);   btnCredits.onClick.AddListener(() => OpenPanel(creditsPanel)); }
        if (btnQuit)     { btnQuit.onClick.AddListener(PlayClickSfx);      btnQuit.onClick.AddListener(OnQuit); }

        if (btnBackHowToPlay) { btnBackHowToPlay.onClick.AddListener(PlayClickSfx); btnBackHowToPlay.onClick.AddListener(BackToMain); }
        if (btnBackSettings)  { btnBackSettings.onClick.AddListener(PlayClickSfx);  btnBackSettings.onClick.AddListener(BackToMain); }
        if (btnBackCredits)   { btnBackCredits.onClick.AddListener(PlayClickSfx);   btnBackCredits.onClick.AddListener(BackToMain); }

        AddHoverSfx(btnPlay); AddHoverSfx(btnHowToPlay); AddHoverSfx(btnSettings); AddHoverSfx(btnCredits); AddHoverSfx(btnQuit);
        if (btnBackHowToPlay) AddHoverSfx(btnBackHowToPlay);
        if (btnBackSettings)  AddHoverSfx(btnBackSettings);
        if (btnBackCredits)   AddHoverSfx(btnBackCredits);

        if (btnModeCalm)
        {
            btnModeCalm.onClick.AddListener(PlayClickSfx);
            btnModeCalm.onClick.AddListener(() => StartGameWithDifficulty(0));   // 0 = Calm
        }
        if (btnModeNoMercy)
        {
            btnModeNoMercy.onClick.AddListener(PlayClickSfx);
            btnModeNoMercy.onClick.AddListener(() => StartGameWithDifficulty(1)); // 1 = No Mercy
        }
        if (btnModeBack)
        {
            btnModeBack.onClick.AddListener(PlayClickSfx);
            btnModeBack.onClick.AddListener(BackToMain); // bukan HideModePanel lagi
        }
        if (btnModeBack) AddHoverSfx(btnModeBack);

        AddHoverSfx(btnModeCalm);
        AddHoverSfx(btnModeNoMercy);
        if (btnModeBack) AddHoverSfx(btnModeBack);
    }

    void AddHoverSfx(Button b)
    {
        if (!b || !sfxHover) return;
        var et = b.gameObject.GetComponent<EventTrigger>();
        if (!et) et = b.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => PlayHoverSfx());
        et.triggers.Add(entry);

        var sel = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        sel.callback.AddListener(_ => PlayHoverSfx());
        et.triggers.Add(sel);
    }

    // -------------------- PANEL SWITCH (iTween) --------------------
    void OpenPanel(GameObject target)
    {
        if (!target || busySwitch) return;

        var from = new PanelPack(leftPanel, leftPanelRT);
        var to   = new PanelPack(target,   GetRT(target));

        StartSwitch(from, to, +1);
    }

    void BackToMain()
    {
        if (busySwitch) return;

        GameObject open = null;
        if (howToPlayPanel && howToPlayPanel.activeSelf)      open = howToPlayPanel;
        else if (settingsPanel && settingsPanel.activeSelf)   open = settingsPanel;
        else if (creditsPanel && creditsPanel.activeSelf)     open = creditsPanel;
        else if (modeSelectPanel && modeSelectPanel.activeSelf) open = modeSelectPanel; // NEW

        if (!open) return;

        var from = new PanelPack(open, GetRT(open));
        var to   = new PanelPack(leftPanel, leftPanelRT);

        StartSwitch(from, to, -1);
    }

    void StartSwitch(PanelPack from, PanelPack to, int dir)
    {
        busySwitch = true;

        // siapkan target
        to.root.SetActive(true);
        to.cg.alpha = 0f;
        to.cg.interactable = false;
        to.cg.blocksRaycasts = false;

        // posisi home keduanya
        Vector2 fromHome = GetHomePos(from.root);
        Vector2 toHome   = GetHomePos(to.root);

        // SET START POSISI:
        // panel tujuan mulai di luar layar (arah kebalikan 'dir')
        if (to.rt)
            to.rt.anchoredPosition = toHome + new Vector2(dir * slideDist, 0f);

        // panel asal pastikan di home sebelum keluar
        if (from.rt)
            from.rt.anchoredPosition = fromHome;

        // FROM: slide keluar + fade out
        if (from.rt)
        {
            iTween.MoveTo(from.rt.gameObject, iTween.Hash(
                "islocal", true,
                "time",    slideTime,
                "easetype", easeOut,
                "position", (Vector3)(fromHome - new Vector2(dir * slideDist, 0f))
            ));
        }

        _twCgA = from.cg;
        iTween.ValueTo(gameObject, iTween.Hash(
            "from", from.cg.alpha, "to", 0f, "time", panelFadeTime,
            "onupdate", "ITween_SetAlphaA"
        ));

        // TO: slide masuk + fade in (overlap dikit)
        if (to.rt)
        {
            iTween.MoveTo(to.rt.gameObject, iTween.Hash(
                "islocal", true,
                "time",    slideTime,
                "delay",   0.02f,
                "easetype", easeIn,
                "position", (Vector3)toHome
            ));
        }

        _twCgB = to.cg;
        // gunakan iTween callback string + oncompletetarget
        _pendingFinalize = () =>
        {
            // snap kembali ke homePos untuk jaga-jaga
            if (from.rt) from.rt.anchoredPosition = fromHome;
            if (to.rt)   to.rt.anchoredPosition   = toHome;

            from.root.SetActive(false);
            to.cg.interactable = true;
            to.cg.blocksRaycasts = true;
            busySwitch = false;

            var firstBtn = to.root.GetComponentInChildren<Button>();
            if (firstBtn) EventSystem.current.SetSelectedGameObject(firstBtn.gameObject);
            else if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
        };

        iTween.ValueTo(gameObject, iTween.Hash(
            "from", 0f, "to", 1f, "time", panelFadeTime, "delay", 0.02f,
            "onupdate", "ITween_SetAlphaB",
            "oncomplete", "ITween_OnPanelInDone",
            "oncompletetarget", gameObject
        ));
    }


    RectTransform GetRT(GameObject go)
    {
        if (!go) return null;
        if (go == settingsPanel)  return settingsPanelRT ? settingsPanelRT : go.GetComponent<RectTransform>();
        if (go == howToPlayPanel) return howToPanelRT    ? howToPanelRT    : go.GetComponent<RectTransform>();
        if (go == creditsPanel)   return creditsPanelRT   ? creditsPanelRT   : go.GetComponent<RectTransform>();
        if (go == modeSelectPanel)  return modeSelectPanelRT  ? modeSelectPanelRT  : go.GetComponent<RectTransform>();
        if (go == leftPanel)      return leftPanelRT      ? leftPanelRT      : go.GetComponent<RectTransform>();
        return go.GetComponent<RectTransform>();
    }

    struct PanelPack
    {
        public GameObject root;
        public CanvasGroup cg;
        public RectTransform rt;
        public PanelPack(GameObject r, RectTransform rtRef)
        {
            root = r;
            cg = r ? (r.GetComponent<CanvasGroup>() ?? r.AddComponent<CanvasGroup>()) : null;
            rt = rtRef ? rtRef : (r ? r.GetComponent<RectTransform>() : null);
        }
    }

    // -------------------- PLAY / QUIT --------------------
    void OnPlay()
    {
        if (modeSelectPanel)
        {
            modeSelectPanel.SetActive(true);

            // fokuskan ke tombol Calm
            if (btnModeCalm)
                EventSystem.current.SetSelectedGameObject(btnModeCalm.gameObject);

            OpenPanel(modeSelectPanel);
            return;
        } else {
             StartGameWithDifficulty(0);
        }
    }

    void OnQuit()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    void StartGameWithDifficulty(int difficulty)
    {
        string targetScene = gameplaySceneName;

        if (difficulty == 1)
            targetScene = string.IsNullOrEmpty(gameplaySceneName2) ? gameplaySceneName : gameplaySceneName2;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[MainMenu] Target scene name is empty!");
            return;
        }

        // (Opsional) tetap simpan info difficulty, kalau mau dipakai di UI dalam scene
        if (!string.IsNullOrEmpty(difficultyPrefKey))
        {
            PlayerPrefs.SetInt(difficultyPrefKey, difficulty);
            PlayerPrefs.Save();
        }

        // Tutup panel mode biar rapi
        HideModePanel();

        // Transisi: fade BGM + fade overlay, sama seperti sebelumnya
        System.Action go = () => SceneManager.LoadScene(targetScene);

        if (bgmSource)
            StartCoroutine(FadeAudio(bgmSource, 0f, bgmFadeOut));

        if (fadeOverlay)
            StartCoroutine(FadeIn(fadeOverlay, 0.25f, () => go(), useRaycast: false));
        else
            go();
    }

    void HideModePanel()
    {
        if (modeSelectPanel)
            modeSelectPanel.SetActive(false);

        // balikin selection ke tombol Play lagi
        if (firstSelected)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }



    // -------------------- FADING HELPERS --------------------
    CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go ? go.GetComponent<CanvasGroup>() : null;
        if (!cg && go) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    System.Collections.IEnumerator FadeAudio(AudioSource src, float target, float time, bool startFromZero = false)
    {
        if (!src) yield break;
        if (startFromZero) { src.volume = 0f; src.Play(); }
        float start = src.volume, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = t / Mathf.Max(0.0001f, time);
            k = 1f - (1f - k) * (1f - k); // ease out quad
            src.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }
        src.volume = target;
        if (Mathf.Approximately(target, 0f)) src.Stop();
    }

    System.Collections.IEnumerator FadeIn(CanvasGroup cg, float time, System.Action onDone = null, bool useRaycast = true)
    {
        if (!cg) yield break;
        cg.gameObject.SetActive(true);
        cg.blocksRaycasts = false; cg.interactable = false;

        float t = 0f, start = cg.alpha, end = 1f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, EaseOutQuad(t / time));
            yield return null;
        }
        cg.alpha = 1f;
        cg.blocksRaycasts = useRaycast; cg.interactable = true;
        onDone?.Invoke();
    }

    System.Collections.IEnumerator FadeOut(CanvasGroup cg, float time, System.Action onDone = null)
    {
        if (!cg) yield break;
        cg.blocksRaycasts = false; cg.interactable = false;

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

    void ITween_OnPanelInDone()
    {
        _pendingFinalize?.Invoke();
        _pendingFinalize = null; // avoid accidental re-run
    }

    float EaseInQuad(float x)  => x * x;
    float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}

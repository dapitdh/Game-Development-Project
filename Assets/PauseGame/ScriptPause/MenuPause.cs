using System.Collections.Generic;
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
    public AudioSource bgmSource;     // drag: BGM_Menu (AudioSource di Canvas)
    public float bgmFadeIn = 0.8f;    // detik
    public float bgmFadeOut = 0.4f;   // detik

    // --- SFX UI ---
    [Header("SFX UI")]
    public AudioSource uiSfxSource;   // drag: AudioSource SFX UI
    public AudioClip sfxClick;
    public AudioClip sfxHover;        // opsional
    public float sfxVolume = 0.9f;

    // --- OPSI A: matikan komponen kontrol saat pause ---
    [Header("Control to disable when paused (Option A)")]
    [Tooltip("Drag komponen penggerak kamera/karakter: FirstPersonController/MouseLook/PlayerMovement/CinemachineBrain/Animator/Constraints, dsb.")]
    [SerializeField] private Behaviour[] disableWhenPaused;

    // --- Bekukan transform saat pause (POSISI + ROTASI) ---
    [Header("Freeze transforms when paused")]
    [Tooltip("Drag Transform yang harus DIAM saat pause: Player root, CameraParent/Pivot, Main Camera atau CM vcam.")]
    [SerializeField] private Transform[] freezeWhenPaused;

    [Tooltip("Jika ON, semua child dari target di atas juga dibekukan (disarankan ON untuk mencegah mesh/armature tetap bergerak).")]
    [SerializeField] private bool freezeChildren = true;

    // ===== private =====
    Coroutine _bgmFadeCo;
    float _bgmBaseVol = 1f;

    // snapshot & cache
    readonly List<Transform> _frozenTargets = new();
    readonly List<Vector3> _savedPos = new();
    readonly List<Quaternion> _savedRot = new();

    readonly List<Rigidbody> _savedRBs = new();
    readonly List<RigidbodyConstraints> _savedRBConstraints = new();

    void Awake()
    {
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        if (pauseMainPanel) pauseMainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameIsPaused = false;

        ToggleControls(true); // aktif di awal

        if (bgmSource)
        {
            _bgmBaseVol = bgmSource.volume;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            if (bgmSource.isPlaying) bgmSource.Stop();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!GameIsPaused) Pause();
            else
            {
                if (settingsPanel && settingsPanel.activeSelf) BackFromSettings();
                else Resume();
            }
        }
    }

    void LateUpdate()
    {
        if (!GameIsPaused || _frozenTargets.Count == 0) return;

        // Kunci POSISI + ROTASI tiap frame (supaya driver orbit/constraint tidak bisa menggeser)
        for (int i = 0; i < _frozenTargets.Count; i++)
        {
            var t = _frozenTargets[i];
            if (!t) continue;
            t.SetPositionAndRotation(_savedPos[i], _savedRot[i]);
        }
    }

    // ---------------- UI SFX helpers ----------------
    void PlayClickSfx() { if (uiSfxSource && sfxClick) uiSfxSource.PlayOneShot(sfxClick, sfxVolume); }
    void PlayHoverSfx() { if (uiSfxSource && sfxHover) uiSfxSource.PlayOneShot(sfxHover, 0.8f * sfxVolume); }
    public void OnUiHover() => PlayHoverSfx();
    public void OnUiClick() => PlayClickSfx();

    // ---------------- Toggle kontrol (OPSI A) ----------------
    void ToggleControls(bool enabled)
    {
        if (disableWhenPaused == null) return;
        foreach (var comp in disableWhenPaused)
            if (comp) comp.enabled = enabled;
    }

    public void TestClick()
    {
        Debug.Log("Button kepencet");
    }


    // ---------------- Flush input axes ----------------
    void HardStopInputs() => Input.ResetInputAxes(); // buang delta Mouse X/Y legacy

    // ---------------- Pause flow ----------------
    public void Resume()
    {
        PlayClickSfx();

        Time.timeScale = 1f;
        GameIsPaused = false;

        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseMainPanel) pauseMainPanel.SetActive(false);
        if (pauseMenuUI) pauseMenuUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        HardStopInputs();
        ToggleControls(true);
        UnfreezeAll();
        FadeBgm(false);
    }

    void Pause()
    {
        if (pauseMenuUI) pauseMenuUI.SetActive(true);
        if (pauseMainPanel) pauseMainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);

        Time.timeScale = 0f;
        GameIsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (EventSystem.current && pauseMainPanel && pauseMainPanel.transform.childCount > 0)
            EventSystem.current.SetSelectedGameObject(pauseMainPanel.transform.GetChild(0).gameObject);

        ToggleControls(false);   // WAJIB: matikan driver kamera/karakter/animator/constraint
        FreezeNow();             // snapshot & kunci POS+ROT (termasuk anak-anak bila dipilih)
        HardStopInputs();
        FadeBgm(true);
    }

    // Dipanggil tombol "Settings"
    public void LoadMenu() => OpenSettings();

    public void OpenSettings()
    {
        PlayClickSfx();

        if (pauseMainPanel) pauseMainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);

        if (EventSystem.current && settingsPanel)
        {
            var back = settingsPanel.transform.Find("Btn_Back")?.gameObject;
            if (back) EventSystem.current.SetSelectedGameObject(back);
        }
    }

    public void BackFromSettings()
    {
        PlayClickSfx();

        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseMainPanel) pauseMainPanel.SetActive(true);

        if (EventSystem.current && pauseMainPanel)
        {
            var resume = pauseMainPanel.transform.Find("Resume")?.gameObject;
            if (resume) EventSystem.current.SetSelectedGameObject(resume);
        }
    }

    public void QuitGame()
    {
        PlayClickSfx();
        Time.timeScale = 1f;
        ToggleControls(true);
        UnfreezeAll();
        HardStopInputs();
        SceneManager.LoadScene("MainMenu");
    }

    // ---------------- Freeze helpers ----------------
    void FreezeNow()
    {
        _frozenTargets.Clear();
        _savedPos.Clear();
        _savedRot.Clear();
        _savedRBs.Clear();
        _savedRBConstraints.Clear();

        if (freezeWhenPaused == null || freezeWhenPaused.Length == 0) return;

        var unique = new HashSet<Transform>();

        foreach (var root in freezeWhenPaused)
        {
            if (!root) continue;

            if (freezeChildren)
            {
                var all = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in all)
                    if (t && unique.Add(t)) AddFreezeTarget(t);
            }
            else
            {
                if (unique.Add(root)) AddFreezeTarget(root);
            }
        }
    }

    void AddFreezeTarget(Transform t)
    {
        _frozenTargets.Add(t);
        _savedPos.Add(t.position);
        _savedRot.Add(t.rotation);

        var rb = t.GetComponent<Rigidbody>();
        if (rb)
        {
            // simpan constraint lama, lalu tahan total
            _savedRBs.Add(rb);
            _savedRBConstraints.Add(rb.constraints);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = rb.constraints |
                             RigidbodyConstraints.FreezePosition |
                             RigidbodyConstraints.FreezeRotation;
        }
    }

    void UnfreezeAll()
    {
        // kembalikan constraint rigidbody
        for (int i = 0; i < _savedRBs.Count; i++)
        {
            var rb = _savedRBs[i];
            if (rb) rb.constraints = _savedRBConstraints[i];
        }

        _frozenTargets.Clear();
        _savedPos.Clear();
        _savedRot.Clear();
        _savedRBs.Clear();
        _savedRBConstraints.Clear();
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

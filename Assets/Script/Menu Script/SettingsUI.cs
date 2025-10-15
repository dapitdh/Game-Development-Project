using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

public class SettingsUI : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mixer;           // drag OmAgusMixer
    public Slider masterSlider;        // 0..1
    public Slider musicSlider;         // 0..1
    public Slider sfxSlider;           // 0..1
    // Exposed param names (harus sama dgn yg di Mixer)
    public string masterParam = "MasterVolume";
    public string musicParam  = "MusicVolume";
    public string sfxParam    = "SfxVolume";

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Toggle vSyncToggle;
    public TMP_Dropdown qualityDropdown;

    [Header("Optional: Brightness")]
    public Slider brightnessSlider;    // -1..+1 (default 0)
    public float brightnessMin = -1f;
    public float brightnessMax =  1f;
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
    public Volume globalVolume;        // drag Volume (URP/HDRP) kalau ada
    ColorAdjustments _colorAdj;
#endif

    Resolution[] _resolutions;

    const string PP_MASTER = "SET_Master";
    const string PP_MUSIC  = "SET_Music";
    const string PP_SFX    = "SET_Sfx";
    const string PP_FS     = "SET_Fullscreen";
    const string PP_RES    = "SET_ResolutionIndex";
    const string PP_VSYNC  = "SET_VSync";
    const string PP_QUAL   = "SET_Quality";
    const string PP_BRIGHT = "SET_Brightness";

    void Start()
    {
        // --- Audio sliders ---
        if (masterSlider) { masterSlider.onValueChanged.AddListener(SetMasterVolume); }
        if (musicSlider)  { musicSlider.onValueChanged.AddListener(SetMusicVolume);  }
        if (sfxSlider)    { sfxSlider.onValueChanged.AddListener(SetSfxVolume);      }

        // --- Display widgets ---
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        if (vSyncToggle) vSyncToggle.onValueChanged.AddListener(SetVSync);

        if (qualityDropdown)
        {
            qualityDropdown.ClearOptions();
            var names = QualitySettings.names;
            var opts = new System.Collections.Generic.List<string>(names);
            qualityDropdown.AddOptions(opts);
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        if (resolutionDropdown)
        {
            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string>();
            int currentIndex = 0;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                var r = _resolutions[i];
                string label = $"{r.width}×{r.height} @ {r.refreshRateRatio.value:0}Hz";
                opts.Add(label);
                if (r.width == Screen.currentResolution.width &&
                    r.height == Screen.currentResolution.height)
                    currentIndex = i;
            }
            resolutionDropdown.AddOptions(opts);
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
        if (globalVolume && globalVolume.profile.TryGet(out _colorAdj))
        {
            if (brightnessSlider)
                brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
#endif

        // Load saved values (atau default) lalu apply
        LoadAll();
    }

    // ===== AUDIO HANDLERS =====
    void SetMasterVolume(float v) => SetMixerLinear(masterParam, v);
    void SetMusicVolume(float v)  => SetMixerLinear(musicParam,  v);
    void SetSfxVolume(float v)    => SetMixerLinear(sfxParam,    v);

    void SetMixerLinear(string param, float linear01)
    {
        if (!mixer) return;
        // linear(0..1) → dB (-80 .. 0)
        float dB = Mathf.Log10(Mathf.Clamp(linear01, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(param, dB);
    }

    // ===== DISPLAY HANDLERS =====
    void SetFullscreen(bool on) => Screen.fullScreen = on;

    void SetResolution(int idx)
    {
        if (_resolutions == null || _resolutions.Length == 0) return;
        idx = Mathf.Clamp(idx, 0, _resolutions.Length - 1);
        var r = _resolutions[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }

    void SetVSync(bool on)
    {
        QualitySettings.vSyncCount = on ? 1 : 0;
    }

    void SetQuality(int levelIndex)
    {
        levelIndex = Mathf.Clamp(levelIndex, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(levelIndex, true);
    }

    // ===== BRIGHTNESS (opsional) =====
    void SetBrightness(float x)
    {
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
        if (_colorAdj != null)
        {
            float v = Mathf.Lerp(brightnessMin, brightnessMax, (x + 1f) * 0.5f); // jika slider -1..+1
            _colorAdj.postExposure.Override(v); // di EV
        }
#endif
    }

    // ===== SAVE / LOAD =====
    public void SaveAll()
    {
        if (masterSlider) PlayerPrefs.SetFloat(PP_MASTER, masterSlider.value);
        if (musicSlider)  PlayerPrefs.SetFloat(PP_MUSIC,  musicSlider.value);
        if (sfxSlider)    PlayerPrefs.SetFloat(PP_SFX,    sfxSlider.value);
        if (fullscreenToggle) PlayerPrefs.SetInt(PP_FS, fullscreenToggle.isOn ? 1 : 0);
        if (resolutionDropdown) PlayerPrefs.SetInt(PP_RES, resolutionDropdown.value);
        if (vSyncToggle) PlayerPrefs.SetInt(PP_VSYNC, vSyncToggle.isOn ? 1 : 0);
        if (qualityDropdown) PlayerPrefs.SetInt(PP_QUAL, qualityDropdown.value);
        if (brightnessSlider) PlayerPrefs.SetFloat(PP_BRIGHT, brightnessSlider.value);
        PlayerPrefs.Save();
    }

    public void LoadAll()
    {
        // set UI value dulu, event listeners akan apply ke sistem
        if (masterSlider) masterSlider.value = PlayerPrefs.GetFloat(PP_MASTER, 0.8f);
        if (musicSlider)  musicSlider.value  = PlayerPrefs.GetFloat(PP_MUSIC,  0.7f);
        if (sfxSlider)    sfxSlider.value    = PlayerPrefs.GetFloat(PP_SFX,    0.8f);

        if (fullscreenToggle) fullscreenToggle.isOn = PlayerPrefs.GetInt(PP_FS, 1) == 1;

        if (resolutionDropdown)
        {
            int savedRes = PlayerPrefs.GetInt(PP_RES, -1);
            resolutionDropdown.value = (savedRes >= 0 && savedRes < resolutionDropdown.options.Count)
                ? savedRes
                : Mathf.Max(0, FindCurrentResolutionIndex());
            resolutionDropdown.RefreshShownValue();
            SetResolution(resolutionDropdown.value);
        }

        if (vSyncToggle) vSyncToggle.isOn = PlayerPrefs.GetInt(PP_VSYNC, 1) == 1;

        if (qualityDropdown)
        {
            int defQual = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1);
            int q = PlayerPrefs.GetInt(PP_QUAL, defQual);
            qualityDropdown.value = q;
            qualityDropdown.RefreshShownValue();
            SetQuality(q);
        }

        if (brightnessSlider)
            brightnessSlider.value = PlayerPrefs.GetFloat(PP_BRIGHT, 0f);
    }

    int FindCurrentResolutionIndex()
    {
        if (_resolutions == null) return 0;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            var r = _resolutions[i];
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                return i;
        }
        return 0;
    }

    // panggil dari tombol "Apply" atau "Back"
    public void ApplyAndSave()
    {
        SaveAll();
        // bisa tambahkan popup "Saved" kecil
    }
}

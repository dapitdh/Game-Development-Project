using UnityEngine;
using UnityEngine.Audio;
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

public class SettingsBootstrap : MonoBehaviour
{
    [Header("Mixer (optional)")]
    public AudioMixer mixer;                 // assign OmAgusMixer (atau isi via Resources)
    public string masterParam = "MasterVolume";
    public string musicParam  = "MusicVolume";
    public string sfxParam    = "SfxVolume";

    [Header("Post (optional)")]
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
    public Volume globalVolume;
    ColorAdjustments _colorAdj;
#endif

    // PlayerPrefs keys (match SettingsUI)
    const string PP_MASTER = "SET_Master";
    const string PP_MUSIC  = "SET_Music";
    const string PP_SFX    = "SET_Sfx";
    const string PP_FS     = "SET_Fullscreen";
    const string PP_RES    = "SET_ResolutionIndex";
    const string PP_VSYNC  = "SET_VSync";
    const string PP_QUAL   = "SET_Quality";
    const string PP_BRIGHT = "SET_Brightness";

    void Awake()
    {
        // Fallback: coba load mixer dari Resources kalau kosong
        if (!mixer)
            mixer = Resources.Load<AudioMixer>("OmAgusMixer"); // butuh file di Assets/Resources/OmAgusMixer.mixer

        ApplyAll();
    }

    public void ApplyAll()
    {
        float m = PlayerPrefs.GetFloat(PP_MASTER, 0.8f);
        float mu= PlayerPrefs.GetFloat(PP_MUSIC,  0.7f);
        float s = PlayerPrefs.GetFloat(PP_SFX,    0.8f);
        bool fs = PlayerPrefs.GetInt(PP_FS, 1) == 1;
        int  vs = PlayerPrefs.GetInt(PP_VSYNC, 1);
        int  q  = PlayerPrefs.GetInt(PP_QUAL, QualitySettings.GetQualityLevel());
        int  ri = PlayerPrefs.GetInt(PP_RES, -1);
        float br= PlayerPrefs.GetFloat(PP_BRIGHT, 0f);

        Debug.Log($"[Bootstrap] Load Prefs → Master:{m:F2} Music:{mu:F2} SFX:{s:F2}  FS:{fs} VSync:{vs} Q:{q} ResIdx:{ri} Bright:{br:F2}");

        ApplyAudio(m, mu, s);
        ApplyDisplay(fs, vs, q, ri);
        ApplyBrightness(br);
    }

    void ApplyAudio(float master, float music, float sfx)
    {
        if (!mixer) { Debug.LogWarning("[Bootstrap] Mixer not assigned."); return; }
        SetDb(masterParam, master);
        SetDb(musicParam,  music);
        SetDb(sfxParam,    sfx);
    }
    void SetDb(string param, float linear01)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear01, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(param, dB);
        // Debug.Log($"[Bootstrap] {param} set to {dB:F1} dB");
    }

    void ApplyDisplay(bool fullscreen, int vSyncOn, int qualityIndex, int resIndex)
    {
        Screen.fullScreen = fullscreen;
        QualitySettings.vSyncCount = vSyncOn == 1 ? 1 : 0;

        qualityIndex = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityIndex, true);

        var res = Screen.resolutions;
        if (res != null && res.Length > 0)
        {
            int idx = (resIndex >= 0 && resIndex < res.Length) ? resIndex : FindCurrentResIndex(res);
            var r = res[idx];
            Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
        }
    }

    int FindCurrentResIndex(Resolution[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
            if (arr[i].width == Screen.currentResolution.width && arr[i].height == Screen.currentResolution.height)
                return i;
        return 0;
    }

    void ApplyBrightness(float value)
    {
#if UNITY_RENDER_PIPELINE_UNIVERSAL || UNITY_2021_2_OR_NEWER
        if (!globalVolume) return;
        if (globalVolume.profile.TryGet(out _colorAdj))
            _colorAdj.postExposure.Override(value);
#endif
    }
}

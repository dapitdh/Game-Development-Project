using UnityEngine;
using System.Collections;

public class PanelSwitcherTween : MonoBehaviour
{
    [System.Serializable]
    public class Panel
    {
        public GameObject root;       // panel GameObject
        public CanvasGroup cg;        // CanvasGroup panel
        public RectTransform rt;      // RectTransform panel
    }

    [Header("Panels")]
    public Panel leftPanel;
    public Panel howToPanel;
    public Panel settingsPanel;
    public Panel creditsPanel;

    [Header("Anim Params")]
    public float slideDist = 620f;
    public float slideTime = 0.35f;
    public float fadeTime  = 0.25f;
    public string easeOut = "easeInOutCubic";
    public string easeIn  = "easeInOutCubic";

    bool busy;

    void Awake()
    {
        // default visible hanya leftPanel
        SetImmediate(leftPanel, true);
        SetImmediate(howToPanel, false);
        SetImmediate(settingsPanel, false);
        SetImmediate(creditsPanel, false);
    }

    void SetImmediate(Panel p, bool on)
    {
        if (!p.root) return;
        p.root.SetActive(true); // aktif dulu supaya cg apply
        if (p.cg) p.cg.alpha = on ? 1f : 0f;
        if (p.rt) p.rt.anchoredPosition = Vector2.zero;
        p.root.SetActive(on);
    }

    public void ShowHowTo()     => Switch(leftPanel, howToPanel, +1);
    public void ShowSettings()  => Switch(leftPanel, settingsPanel, +1);
    public void ShowCredits()   => Switch(leftPanel, creditsPanel, +1);

    public void BackFromHowTo()    => Switch(howToPanel, leftPanel, -1);
    public void BackFromSettings() => Switch(settingsPanel, leftPanel, -1);
    public void BackFromCredits()  => Switch(creditsPanel, leftPanel, -1);

    void Switch(Panel from, Panel to, int dir)
    {
        if (busy || from.root == to.root) return;
        StartCoroutine(CoSwitch(from, to, dir));
    }

    IEnumerator CoSwitch(Panel from, Panel to, int dir)
    {
        busy = true;
        if (to.root) { to.root.SetActive(true); if (to.cg) to.cg.alpha = 0f; }

        // From: slide ke kiri (dir=+1) atau ke kanan (dir=-1) + fade out
        if (from.rt)
        {
            iTween.MoveTo(from.rt.gameObject, iTween.Hash(
                "islocal", true,
                "time", slideTime,
                "easetype", easeOut,
                "position", (Vector3)(new Vector2(-dir * slideDist, 0f))
            ));
        }
        if (from.cg)
        {
            iTween.ValueTo(gameObject, iTween.Hash(
                "from", from.cg.alpha, "to", 0f, "time", fadeTime,
                "onupdate", (System.Action<object>)((val)=> from.cg.alpha = (float)val)
            ));
        }

        // To: start dari kanan (dir=+1) atau kiri (dir=-1)
        if (to.rt) to.rt.anchoredPosition = new Vector2(dir * slideDist, 0f);
        if (to.cg) to.cg.alpha = 0f;

        // To slide in + fade in (sedikit delay supaya tumpang tindih mulus)
        yield return new WaitForSeconds(0.02f);
        if (to.rt)
        {
            iTween.MoveTo(to.rt.gameObject, iTween.Hash(
                "islocal", true,
                "time", slideTime,
                "easetype", easeIn,
                "position", Vector3.zero
            ));
        }
        if (to.cg)
        {
            iTween.ValueTo(gameObject, iTween.Hash(
                "from", 0f, "to", 1f, "time", fadeTime,
                "onupdate", (System.Action<object>)((val)=> to.cg.alpha = (float)val)
            ));
        }

        yield return new WaitForSeconds(Mathf.Max(slideTime, fadeTime));
        if (from.root) from.root.SetActive(false);
        busy = false;
    }
}

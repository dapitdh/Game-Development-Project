using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MenuIntroTween : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform title;
    public RectTransform buttonGroup;
    public RectTransform windowFrameRight; // optional, boleh null
    public CanvasGroup leftPanelCg;        // CanvasGroup left panel

    [Header("Timing")]
    public float delayBefore = 0.2f;
    public float titleDropTime = 0.9f;
    public float buttonStagger = 0.07f;
    public float groupFadeTime = 0.25f;

    [Header("Easing")]
    public string easeTitle = "easeOutBounce";
    public string easeButtons = "easeOutBack";

    bool played;

    void Start()
    {
        // Pastikan default state
        if (leftPanelCg) leftPanelCg.alpha = 0f;

        if (!played) StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        played = true;
        yield return new WaitForSeconds(delayBefore);

        // Fade in panel
        if (leftPanelCg)
        {
            iTween.ValueTo(gameObject, iTween.Hash(
                "from", 0f, "to", 1f, "time", groupFadeTime,
                "onupdate", (System.Action<object>)((val) => { leftPanelCg.alpha = (float)val; }),
                "easetype", iTween.EaseType.linear
            ));
        }

        // Title drop
        if (title)
        {
            var startY = title.anchoredPosition.y + 220f;
            var endY = title.anchoredPosition.y;
            title.anchoredPosition = new Vector2(title.anchoredPosition.x, startY);
            iTween.ValueTo(gameObject, iTween.Hash(
                "from", startY, "to", endY, "time", titleDropTime,
                "easetype", easeTitle,
                "onupdate", (System.Action<object>)((val) =>
                {
                    var p = title.anchoredPosition;
                    p.y = (float)val;
                    title.anchoredPosition = p;
                })
            ));
        }

        // Buttons staggered scale-in
        if (buttonGroup)
        {
            for (int i = 0; i < buttonGroup.childCount; i++)
            {
                var b = buttonGroup.GetChild(i) as RectTransform;
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

        // Subtle parallax pada window kanan
        if (windowFrameRight)
        {
            var p0 = windowFrameRight.anchoredPosition;
            windowFrameRight.anchoredPosition = p0 + new Vector2(30f, 0f);
            iTween.MoveTo(windowFrameRight.gameObject, iTween.Hash(
                "position", (Vector3) (p0),
                "islocal", true,
                "time", 1.2f,
                "easetype", "easeOutCubic"
            ));
        }
    }
}

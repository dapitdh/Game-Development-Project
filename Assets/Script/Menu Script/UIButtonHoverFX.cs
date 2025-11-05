using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform rect;
    public float hoverScale = 1.06f;
    public float hoverTime = 0.08f;
    public float pressScale = 0.97f;
    float baseRot;
    Vector3 baseScale;

    void Reset(){ rect = GetComponent<RectTransform>(); }
    void Awake(){ if (!rect) rect = GetComponent<RectTransform>(); baseScale = rect.localScale; baseRot = rect.localEulerAngles.z; }

    public void OnPointerEnter(PointerEventData e) => Hover(true);
    public void OnPointerExit(PointerEventData e)  => Hover(false);
    public void OnSelect(BaseEventData e)          => Hover(true);
    public void OnDeselect(BaseEventData e)        => Hover(false);

    public void OnPointerDown(PointerEventData e)  { StopAllCoroutines(); StartCoroutine(TweenScale(pressScale, 0.06f)); }
    public void OnPointerUp(PointerEventData e)    { StopAllCoroutines(); StartCoroutine(TweenScale(hoverScale, 0.06f)); }

    void Hover(bool on)
    {
        StopAllCoroutines();
        float target = on ? hoverScale : 1f;
        StartCoroutine(TweenScale(target, hoverTime));
        // wobble kecil ±1.3°
        float rot = on ? Random.Range(-1.3f, 1.3f) : 0f;
        rect.localEulerAngles = new Vector3(0,0, baseRot + rot);
    }

    System.Collections.IEnumerator TweenScale(float target, float t)
    {
        Vector3 start = rect.localScale;
        Vector3 end = baseScale * target;
        float k = 0f;
        while (k < t)
        {
            k += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(start, end, k / t);
            yield return null;
        }
        rect.localScale = end;
    }
}

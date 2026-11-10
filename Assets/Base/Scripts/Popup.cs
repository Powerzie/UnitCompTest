using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    void Awake()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<PressScale>() == null)
                button.gameObject.AddComponent<PressScale>();
        }

        StartCoroutine(Fade(group));
    }

    IEnumerator Fade(CanvasGroup group)
    {
        float time = 0f;
        while (time < 0.2f)
        {
            time += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(time / 0.2f);
            yield return null;
        }

        group.alpha = 1f;
    }
}

public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Vector3 _scale;

    void Awake()
    {
        _scale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = _scale * 0.96f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = _scale;
    }
}

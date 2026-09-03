using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RespawnFadeUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    public bool IsReady => fadeCanvasGroup != null;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();

        if (canvas != null &&
            canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
            transform.parent != null)
        {
            transform.SetParent(null, false);
            transform.localScale = Vector3.one;
        }

        SetImmediate(0f, false);
    }

    public IEnumerator FadeToBlack(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    public void SetImmediate(float alpha, bool blockInput)
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = blockInput;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true;
        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            fadeCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }

    private void OnValidate()
    {
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }
    }
}

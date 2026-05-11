using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneStartTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text loadingPercentText;
    [SerializeField] private float resumeToFullDuration = 0.6f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private void Awake()
    {
        Debug.Log($"GameSceneStartTransition Awake: HasPending={TransitionState.HasPending}, CanvasGroup={(transitionCanvasGroup!=null)}, Slider={(loadingSlider!=null)}, TMP={(loadingPercentText!=null)}");

        // try to auto-assign common references if they were left unassigned
        if (transitionCanvasGroup == null)
        {
            transitionCanvasGroup = GetComponent<CanvasGroup>();
            if (transitionCanvasGroup != null) Debug.Log("GameSceneStartTransition: found CanvasGroup on same GameObject");
        }

        if (loadingSlider == null)
        {
            loadingSlider = GetComponentInChildren<Slider>(true);
            if (loadingSlider != null) Debug.Log($"GameSceneStartTransition: found Slider '{loadingSlider.name}' in children");
        }

        if (loadingPercentText == null)
        {
            loadingPercentText = GetComponentInChildren<TMP_Text>(true);
            if (loadingPercentText != null) Debug.Log($"GameSceneStartTransition: found TMP_Text '{loadingPercentText.name}' in children");
        }

        // If there's no pending transition, hide the overlay
        if (!TransitionState.HasPending)
        {
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 0f;
                transitionCanvasGroup.interactable = false;
                transitionCanvasGroup.blocksRaycasts = false;
            }

            if (loadingSlider != null)
            {
                loadingSlider.gameObject.SetActive(false);
            }

            if (loadingPercentText != null)
            {
                loadingPercentText.text = string.Empty;
            }

            return;
        }

        // Start the resume animation from the pending value
        float start = Mathf.Clamp01(TransitionState.PendingStartPercent);
        TransitionState.HasPending = false;
        Debug.Log($"GameSceneStartTransition: starting resume from {start * 100f}%");
        StartCoroutine(ResumeAndFade(start));
    }

    private IEnumerator ResumeAndFade(float startNormalized)
    {
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 1f;
            transitionCanvasGroup.interactable = true;
            transitionCanvasGroup.blocksRaycasts = true;
        }

        if (loadingSlider != null)
        {
            loadingSlider.gameObject.SetActive(true);
            loadingSlider.normalizedValue = startNormalized;
        }

        UpdatePercentText(startNormalized);

        float elapsed = 0f;
        while (elapsed < resumeToFullDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / resumeToFullDuration);
            float value = Mathf.Lerp(startNormalized, 1f, t);
            if (loadingSlider != null) loadingSlider.normalizedValue = value;
            UpdatePercentText(value);
            yield return null;
        }

        if (transitionCanvasGroup != null)
        {
            float e = 0f;
            while (e < fadeOutDuration)
            {
                e += Time.unscaledDeltaTime;
                transitionCanvasGroup.alpha = Mathf.Lerp(1f, 0f, e / fadeOutDuration);
                yield return null;
            }

            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.interactable = false;
            transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    private void UpdatePercentText(float normalized)
    {
        if (loadingPercentText == null) return;
        int percent = Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f);
        loadingPercentText.text = percent.ToString() + "\u0025";
    }
}

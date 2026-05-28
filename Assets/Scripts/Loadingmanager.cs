using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("Black Screen")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    [Header("Loading Bar")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Timing")]
    [SerializeField] private float sliderFillDuration = 1.2f;
    [SerializeField] private float holdBeforeFade = 0.3f;
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("Slider Range")]
    [SerializeField] private float sliderStartValue = 0.75f;
    [SerializeField] private float sliderEndValue = 1.0f;

    void Start()
    {
        if (blackScreenCanvasGroup == null)
        {
            Debug.LogError("LoadingManager: blackScreenCanvasGroup is not assigned.");
            return;
        }

        blackScreenCanvasGroup.alpha = 1f;
        blackScreenCanvasGroup.blocksRaycasts = true;
        blackScreenCanvasGroup.interactable = true;

        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = sliderStartValue;
        }

        if (loadingText != null)
            loadingText.text = Mathf.RoundToInt(sliderStartValue * 100f) + "%";

        StartCoroutine(RunLoadingSequence());
    }

    private IEnumerator RunLoadingSequence()
    {
        if (loadingSlider != null)
        {
            float elapsed = 0f;

            while (elapsed < sliderFillDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sliderFillDuration);
                float eased = EaseOutCubic(t);
                loadingSlider.value = Mathf.Lerp(sliderStartValue, sliderEndValue, eased);

                if (loadingText != null)
                    loadingText.text = Mathf.RoundToInt(loadingSlider.value * 100f) + "%";
                yield return null;
            }

            loadingSlider.value = sliderEndValue;

        if (loadingText != null)
            loadingText.text = "100%";
        }

        yield return new WaitForSeconds(holdBeforeFade);

        float fadeElapsed = 0f;

        while (fadeElapsed < fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            blackScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeOutDuration);
            yield return null;
        }

        blackScreenCanvasGroup.alpha = 0f;
        blackScreenCanvasGroup.blocksRaycasts = false;
        blackScreenCanvasGroup.interactable = false;

        blackScreenCanvasGroup.gameObject.SetActive(false);
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
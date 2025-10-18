using UnityEngine;
using System.Collections;

public class MainmenuManager : MonoBehaviour
{
    [SerializeField] private float _fadeDuration = 1f;

    public void StartGame() => UnityEngine.SceneManagement.SceneManager.LoadScene(1);

    public void ExitGame() => Application.Quit();

    public void ShowCanvas(Canvas canvas) => StartCoroutine(FadeIn(canvas));

    public void HideCanvas(Canvas canvas) => StartCoroutine(FadeOut(canvas));

    private IEnumerator FadeIn(Canvas canvas)
    {
        if (!canvas.TryGetComponent<CanvasGroup>(out var canvasGroup))
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        canvas.enabled = true;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut(Canvas canvas)
    {
        if (!canvas.TryGetComponent<CanvasGroup>(out var canvasGroup))
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / _fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvas.enabled = false;
    }
}
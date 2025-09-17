using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;    // ÉãÅ[ÉgÇ…CanvasGroup
    [SerializeField] private TMP_Text diedText;          // ÅgYOU DIEDÅh
    [SerializeField] private float fadeInSeconds = 1.2f;

    void Reset()
    {
        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        diedText = GetComponentInChildren<TMP_Text>(true);
    }

    public IEnumerator PlayAndReload(float holdSeconds)
    {
        if (canvasGroup) canvasGroup.gameObject.SetActive(true);
        if (diedText) diedText.gameObject.SetActive(true);

        float t = 0f;
        if (canvasGroup) canvasGroup.alpha = 0f;

        while (t < fadeInSeconds)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup) canvasGroup.alpha = Mathf.Clamp01(t / fadeInSeconds);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(holdSeconds);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

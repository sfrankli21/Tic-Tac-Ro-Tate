using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public enum StartFadeMode
    {
        None,
        FadeIn,
        FadeOut
    }

    [SerializeField]
    Image targetImage;

    [SerializeField]
    float FadeInDuration;

    [SerializeField]
    float FadeoutDuration;

    [SerializeField]
    UnityEvent FadeOutEvent;

    [SerializeField]
    UnityEvent FadeInEvent;

    [SerializeField, InspectorName("Fade In Target Alpha (0-255)")]
    int FadeInTargetAlpha = 0;

    [SerializeField, InspectorName("Fade Out Target Alpha (0-255)")]
    int FadeOutTargetAlpha = 255;

    [SerializeField]
    StartFadeMode FadeOnStart = StartFadeMode.None;

    [SerializeField, InspectorName("Start Alpha Before Fade (0-255)")]
    int StartAlphaBeforeFade = 255;

    [SerializeField]
    bool SetStartAlphaBeforeFade = true;

    [SerializeField]
    List<string> SceneList = new List<string>();

    Coroutine currentFade;

    void Start()
    {
        if (targetImage == null)
        {
            return;
        }

        if (FadeOnStart == StartFadeMode.None)
        {
            return;
        }

        if (SetStartAlphaBeforeFade)
        {
            float startAlpha = StartAlphaBeforeFade / 255f;
            Color startColor = targetImage.color;
            startColor.a = startAlpha;
            targetImage.color = startColor;
        }

        if (FadeOnStart == StartFadeMode.FadeIn)
        {
            FadeIn();
        }

        if (FadeOnStart == StartFadeMode.FadeOut)
        {
            FadeOut();
        }
    }

    void OnValidate()
    {
        if (FadeInTargetAlpha < 0) FadeInTargetAlpha = 0;
        if (FadeInTargetAlpha > 255) FadeInTargetAlpha = 255;

        if (FadeOutTargetAlpha < 0) FadeOutTargetAlpha = 0;
        if (FadeOutTargetAlpha > 255) FadeOutTargetAlpha = 255;

        if (StartAlphaBeforeFade < 0) StartAlphaBeforeFade = 0;
        if (StartAlphaBeforeFade > 255) StartAlphaBeforeFade = 255;
    }

    void EnableTargetImage()
    {
        targetImage.gameObject.SetActive(true);
        targetImage.enabled = true;
    }

    public void FadeOut()
    {
        if (targetImage == null) return;

        EnableTargetImage();

        if (currentFade != null) StopCoroutine(currentFade);

        float targetAlpha = FadeOutTargetAlpha / 255f;
        currentFade = StartCoroutine(FadeRoutine(targetAlpha, FadeoutDuration, FadeOutEvent));
    }

    public void FadeIn()
    {
        if (targetImage == null) return;

        EnableTargetImage();

        if (currentFade != null) StopCoroutine(currentFade);

        float targetAlpha = FadeInTargetAlpha / 255f;
        currentFade = StartCoroutine(FadeRoutine(targetAlpha, FadeInDuration, FadeInEvent));
    }

    public void FadeOutTo(int index)
    {
        if (index < 0 || index >= SceneList.Count) return;
        if (targetImage == null) return;

        EnableTargetImage();

        if (currentFade != null) StopCoroutine(currentFade);

        float targetAlpha = FadeOutTargetAlpha / 255f;
        currentFade = StartCoroutine(FadeRoutineLoadScene(targetAlpha, FadeoutDuration, FadeOutEvent, SceneList[index]));
    }

    public void ReloadScene()
    {
        if (targetImage == null) return;

        EnableTargetImage();

        if (currentFade != null) StopCoroutine(currentFade);

        float targetAlpha = FadeOutTargetAlpha / 255f;
        currentFade = StartCoroutine(FadeRoutineReloadScene(targetAlpha, FadeoutDuration, FadeOutEvent));
    }

    public void QuitApp()
    {
        if (targetImage == null)
        {
            QuitAppImmediate();
            return;
        }

        EnableTargetImage();

        if (currentFade != null) StopCoroutine(currentFade);

        float targetAlpha = FadeOutTargetAlpha / 255f;
        currentFade = StartCoroutine(FadeRoutineQuitApp(targetAlpha, FadeoutDuration, FadeOutEvent));
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration, UnityEvent completeEvent)
    {
        if (duration <= 0f)
        {
            Color instantColor = targetImage.color;
            instantColor.a = targetAlpha;
            targetImage.color = instantColor;
            if (completeEvent != null) completeEvent.Invoke();
            currentFade = null;
            yield break;
        }

        float startAlpha = targetImage.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            Color c = targetImage.color;
            c.a = alpha;
            targetImage.color = c;
            yield return null;
        }

        Color finalColor = targetImage.color;
        finalColor.a = targetAlpha;
        targetImage.color = finalColor;

        if (completeEvent != null) completeEvent.Invoke();
        currentFade = null;
    }

    IEnumerator FadeRoutineLoadScene(float targetAlpha, float duration, UnityEvent completeEvent, string sceneName)
    {
        yield return FadeRoutine(targetAlpha, duration, completeEvent);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeRoutineReloadScene(float targetAlpha, float duration, UnityEvent completeEvent)
    {
        yield return FadeRoutine(targetAlpha, duration, completeEvent);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator FadeRoutineQuitApp(float targetAlpha, float duration, UnityEvent completeEvent)
    {
        yield return FadeRoutine(targetAlpha, duration, completeEvent);
        QuitAppImmediate();
    }

    void QuitAppImmediate()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
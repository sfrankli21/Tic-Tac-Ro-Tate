using System;
using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[AddComponentMenu("UI/Board UIButton", 31)]
public class BoardUIButton : Selectable, IPointerClickHandler, ISubmitHandler
{
    public enum OutputType
    {
        Button,
        Toggle
    }

    public enum ToggleState
    {
        Off,
        On
    }

    public enum AudioBackend
    {
        UnityAudio,
        FMODAudio
    }

    [Serializable]
    public class UIButtonClickedEvent : UnityEvent { }

    [Serializable]
    public class UIButtonAudioCue
    {
        public bool playAudio = true;
        public AudioClip unityClip;
        [Range(0f, 1f)] public float unityVolume = 1f;
        public EventReference fmodEvent;
        public bool fmodPlayAtButtonPosition = true;
    }

    [FormerlySerializedAs("onClick")]
    [SerializeField] private UIButtonClickedEvent m_OnClick = new UIButtonClickedEvent();

    [SerializeField] private OutputType outputType = OutputType.Button;
    [SerializeField] private AudioBackend audioBackend = AudioBackend.UnityAudio;
    [SerializeField] private AudioSource unityAudioSource;

    [SerializeField] private UIButtonAudioCue clickAudio = new UIButtonAudioCue();

    [SerializeField] private bool hoverButtonEnabled = true;
    [SerializeField] private UnityEvent onHoverButtonEnter = new UnityEvent();
    [SerializeField] private UnityEvent onHoverButtonExit = new UnityEvent();
    [SerializeField] private UIButtonAudioCue hoverEnterAudio = new UIButtonAudioCue();
    [SerializeField] private UIButtonAudioCue hoverExitAudio = new UIButtonAudioCue();

    [SerializeField] private bool hoverScaleEnabled = true;
    [SerializeField] private Transform hoverScaleTarget;
    [SerializeField] private Vector3 hoverTargetScale = new Vector3(1.1f, 1.1f, 1.1f);
    [SerializeField] private float hoverScaleInDuration = 0.1f;
    [SerializeField] private float hoverScaleOutDuration = 0.1f;
    [SerializeField] private bool useUnscaledTimeForHoverScale = true;

    [SerializeField] private bool holdButtonEnabled = true;
    [SerializeField] private float holdEnterDelay = 0.5f;
    [SerializeField] private float holdLogicFrequency = 4f;
    [SerializeField] private UnityEvent onHoldButtonEnter = new UnityEvent();
    [SerializeField] private UnityEvent onHoldButtonLogic = new UnityEvent();
    [SerializeField] private UnityEvent onHoldButtonExit = new UnityEvent();
    [SerializeField] private UIButtonAudioCue holdEnterAudio = new UIButtonAudioCue();
    [SerializeField] private UIButtonAudioCue holdLogicAudio = new UIButtonAudioCue();
    [SerializeField] private UIButtonAudioCue holdExitAudio = new UIButtonAudioCue();

    [SerializeField] private ToggleState startingToggleState = ToggleState.Off;
    [SerializeField] private UnityEvent toggleStateOn = new UnityEvent();
    [SerializeField] private UnityEvent toggleStateOff = new UnityEvent();

    private ToggleState currentToggleState;
    private int lastPressFrame = -1;
    private bool isPointerHeld;
    private bool isPointerHovering;
    private bool holdLogicActive;
    private Coroutine holdCoroutine;

    private Vector3 normalScale;
    private Coroutine hoverScaleCoroutine;

    private Color originalDisabledColor;
    private bool originalDisabledColorCaptured;

    public void SetBoardState(int currentPlayer, int owner, Color playerXColor, Color playerOColor, bool canInteract)
    {
        if (!originalDisabledColorCaptured)
        {
            originalDisabledColor = colors.disabledColor;
            originalDisabledColorCaptured = true;
        }

        Color currentPlayerColor = currentPlayer == 1 ? playerXColor : playerOColor;
        ColorBlock colorBlock = colors;
        colorBlock.highlightedColor = currentPlayerColor;
        colorBlock.selectedColor = currentPlayerColor;
        colorBlock.disabledColor = owner == 1 ? playerXColor : owner == 2 ? playerOColor : originalDisabledColor;
        colors = colorBlock;
        interactable = canInteract;
    }

    protected BoardUIButton()
    {
    }

    protected override void Awake()
    {
        base.Awake();

        if (hoverScaleTarget == null)
            hoverScaleTarget = transform;

        normalScale = hoverScaleTarget.localScale;
        currentToggleState = startingToggleState;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (hoverScaleTarget == null)
            hoverScaleTarget = transform;

        normalScale = hoverScaleTarget.localScale;
        isPointerHovering = false;
        isPointerHeld = false;
        holdLogicActive = false;
    }

    public UIButtonClickedEvent onClick
    {
        get { return m_OnClick; }
        set { m_OnClick = value; }
    }

    public UnityEvent OnHoverButtonEnter
    {
        get { return onHoverButtonEnter; }
        set { onHoverButtonEnter = value; }
    }

    public UnityEvent OnHoverButtonExit
    {
        get { return onHoverButtonExit; }
        set { onHoverButtonExit = value; }
    }

    public UnityEvent OnHoldButtonEnter
    {
        get { return onHoldButtonEnter; }
        set { onHoldButtonEnter = value; }
    }

    public UnityEvent OnHoldButtonLogic
    {
        get { return onHoldButtonLogic; }
        set { onHoldButtonLogic = value; }
    }

    public UnityEvent OnHoldButtonExit
    {
        get { return onHoldButtonExit; }
        set { onHoldButtonExit = value; }
    }

    public OutputType CurrentOutputType
    {
        get { return outputType; }
        set { outputType = value; }
    }

    public ToggleState CurrentToggleState
    {
        get { return currentToggleState; }
    }

    public UnityEvent ToggleStateOn
    {
        get { return toggleStateOn; }
        set { toggleStateOn = value; }
    }

    public UnityEvent ToggleStateOff
    {
        get { return toggleStateOff; }
        set { toggleStateOff = value; }
    }

    public AudioBackend CurrentAudioBackend
    {
        get { return audioBackend; }
        set { audioBackend = value; }
    }

    public Transform HoverScaleTarget
    {
        get { return hoverScaleTarget; }
        set
        {
            hoverScaleTarget = value;

            if (hoverScaleTarget == null)
                hoverScaleTarget = transform;

            normalScale = hoverScaleTarget.localScale;
        }
    }

    public void SetButtonOutputType()
    {
        outputType = OutputType.Button;
    }

    public void SetToggleOutputType()
    {
        outputType = OutputType.Toggle;
    }

    public void SetUnityAudioMode()
    {
        audioBackend = AudioBackend.UnityAudio;
    }

    public void SetFMODAudioMode()
    {
        audioBackend = AudioBackend.FMODAudio;
    }

    private void Press()
    {
        if (!IsActive() || !IsInteractable())
            return;

        if (lastPressFrame == Time.frameCount)
            return;

        lastPressFrame = Time.frameCount;

        PlayAudioCue(clickAudio);

        if (outputType == OutputType.Toggle)
            FlipToggleState();

        UISystemProfilerApi.AddMarker("UIButton.onClick", this);
        m_OnClick.Invoke();
    }

    private void FlipToggleState()
    {
        if (currentToggleState == ToggleState.Off)
        {
            currentToggleState = ToggleState.On;
            toggleStateOn.Invoke();
            return;
        }

        currentToggleState = ToggleState.Off;
        toggleStateOff.Invoke();
    }

    private void PlayAudioCue(UIButtonAudioCue audioCue)
    {
        if (audioCue == null || !audioCue.playAudio)
            return;

        if (audioBackend == AudioBackend.UnityAudio)
        {
            PlayUnityAudioCue(audioCue);
            return;
        }

        if (audioBackend == AudioBackend.FMODAudio)
        {
            PlayFMODAudioCue(audioCue);
        }
    }

    private void PlayUnityAudioCue(UIButtonAudioCue audioCue)
    {
        if (unityAudioSource == null || audioCue.unityClip == null)
            return;

        unityAudioSource.PlayOneShot(audioCue.unityClip, audioCue.unityVolume);
    }

    private void PlayFMODAudioCue(UIButtonAudioCue audioCue)
    {
        if (audioCue.fmodEvent.IsNull)
            return;

        if (audioCue.fmodPlayAtButtonPosition)
        {
            RuntimeManager.PlayOneShot(audioCue.fmodEvent, GetAudioPosition());
            return;
        }

        RuntimeManager.PlayOneShot(audioCue.fmodEvent);
    }

    private Vector3 GetAudioPosition()
    {
        if (hoverScaleTarget != null)
            return hoverScaleTarget.position;

        return transform.position;
    }

    private void StartHoverScale()
    {
        if (!hoverScaleEnabled)
            return;

        if (hoverScaleTarget == null)
            hoverScaleTarget = transform;

        if (hoverScaleCoroutine != null)
            StopCoroutine(hoverScaleCoroutine);

        hoverScaleCoroutine = StartCoroutine(HoverScaleRoutine(hoverTargetScale, hoverScaleInDuration));
    }

    private void StopHoverScale()
    {
        if (!hoverScaleEnabled)
            return;

        if (hoverScaleTarget == null)
            hoverScaleTarget = transform;

        if (hoverScaleCoroutine != null)
            StopCoroutine(hoverScaleCoroutine);

        hoverScaleCoroutine = StartCoroutine(HoverScaleRoutine(normalScale, hoverScaleOutDuration));
    }

    private IEnumerator HoverScaleRoutine(Vector3 targetScale, float duration)
    {
        if (hoverScaleTarget == null)
            hoverScaleTarget = transform;

        if (duration <= 0f)
        {
            hoverScaleTarget.localScale = targetScale;
            hoverScaleCoroutine = null;
            yield break;
        }

        Vector3 startScale = hoverScaleTarget.localScale;
        float time = 0f;

        while (time < duration)
        {
            if (useUnscaledTimeForHoverScale)
                time += Time.unscaledDeltaTime;
            else
                time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            hoverScaleTarget.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        hoverScaleTarget.localScale = targetScale;
        hoverScaleCoroutine = null;
    }

    private void StartHoldLogic()
    {
        if (!holdButtonEnabled || !IsActive() || !IsInteractable())
            return;

        StopHoldCoroutineOnly();

        isPointerHeld = true;
        holdCoroutine = StartCoroutine(HoldLogicRoutine());
    }

    private void StopHoldLogic()
    {
        bool shouldFireExit = holdLogicActive;

        isPointerHeld = false;
        holdLogicActive = false;

        StopHoldCoroutineOnly();

        if (shouldFireExit && IsActive())
        {
            PlayAudioCue(holdExitAudio);
            onHoldButtonExit.Invoke();
        }
    }

    private void StopHoldCoroutineOnly()
    {
        if (holdCoroutine == null)
            return;

        StopCoroutine(holdCoroutine);
        holdCoroutine = null;
    }

    private IEnumerator HoldLogicRoutine()
    {
        float delayElapsed = 0f;

        while (delayElapsed < holdEnterDelay)
        {
            if (!isPointerHeld || !IsActive() || !IsInteractable())
            {
                holdCoroutine = null;
                yield break;
            }

            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!isPointerHeld || !IsActive() || !IsInteractable())
        {
            holdCoroutine = null;
            yield break;
        }

        holdLogicActive = true;
        PlayAudioCue(holdEnterAudio);
        onHoldButtonEnter.Invoke();

        float interval = 0f;

        if (holdLogicFrequency > 0f)
            interval = 1f / holdLogicFrequency;

        while (isPointerHeld && IsActive() && IsInteractable())
        {
            if (interval > 0f)
            {
                float intervalElapsed = 0f;

                while (intervalElapsed < interval)
                {
                    if (!isPointerHeld || !IsActive() || !IsInteractable())
                    {
                        holdCoroutine = null;
                        yield break;
                    }

                    intervalElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }

            if (!isPointerHeld || !IsActive() || !IsInteractable())
            {
                holdCoroutine = null;
                yield break;
            }

            PlayAudioCue(holdLogicAudio);
            onHoldButtonLogic.Invoke();
        }

        holdCoroutine = null;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        if (isPointerHovering)
            return;

        isPointerHovering = true;

        if (!hoverButtonEnabled || !IsActive() || !IsInteractable())
            return;

        StartHoverScale();
        PlayAudioCue(hoverEnterAudio);
        onHoverButtonEnter.Invoke();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (!isPointerHovering)
            return;

        isPointerHovering = false;

        if (hoverButtonEnabled && IsActive())
        {
            StopHoverScale();
            PlayAudioCue(hoverExitAudio);
            onHoverButtonExit.Invoke();
        }

        StopHoldLogic();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        StartHoldLogic();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        StopHoldLogic();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        Press();
    }

    public virtual void OnSubmit(BaseEventData eventData)
    {
        Press();

        if (!IsActive() || !IsInteractable())
            return;

        DoStateTransition(SelectionState.Pressed, false);
        StartCoroutine(OnFinishSubmit());
    }

    protected override void OnDisable()
    {
        StopHoldLogic();

        isPointerHovering = false;
        isPointerHeld = false;
        holdLogicActive = false;

        if (hoverScaleCoroutine != null)
        {
            StopCoroutine(hoverScaleCoroutine);
            hoverScaleCoroutine = null;
        }

        if (hoverScaleTarget == null)
            hoverScaleTarget = transform;

        hoverScaleTarget.localScale = normalScale;

        base.OnDisable();
    }

    private IEnumerator OnFinishSubmit()
    {
        var fadeTime = colors.fadeDuration;
        var elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        DoStateTransition(currentSelectionState, false);
    }
}
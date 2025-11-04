using UnityEngine;

public class TabMenuSlider : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private RectTransform lPanel;
    [SerializeField] private RectTransform rPanel;

    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float offScreenOffset = 100f;

    [Header("State")]
    [SerializeField] private bool isPanelsVisible = false;

    private Vector2 lPanelOnScreenPos;
    private Vector2 lPanelOffScreenPos;
    private Vector2 rPanelOnScreenPos;
    private Vector2 rPanelOffScreenPos;

    private float currentAnimationTime = 0f;
    private bool isAnimating = false;
    private bool targetState = false;

    private void Start()
    {
        InitializePanelPositions();
        SetPanelsImmediate(isPanelsVisible);
    }

    private void Update()
    {
        if (isAnimating)
        {
            currentAnimationTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentAnimationTime / slideDuration);
            float curveValue = slideCurve.Evaluate(progress);

            if (targetState)
            {
                lPanel.anchoredPosition = Vector2.Lerp(lPanelOffScreenPos, lPanelOnScreenPos, curveValue);
                rPanel.anchoredPosition = Vector2.Lerp(rPanelOffScreenPos, rPanelOnScreenPos, curveValue);
            }
            else
            {
                lPanel.anchoredPosition = Vector2.Lerp(lPanelOnScreenPos, lPanelOffScreenPos, curveValue);
                rPanel.anchoredPosition = Vector2.Lerp(rPanelOnScreenPos, rPanelOffScreenPos, curveValue);
            }

            if (progress >= 1f)
            {
                isAnimating = false;
                isPanelsVisible = targetState;
            }
        }
    }

    private void InitializePanelPositions()
    {
        lPanelOnScreenPos = lPanel.anchoredPosition;
        rPanelOnScreenPos = rPanel.anchoredPosition;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        float screenWidth = canvasRect.rect.width;

        lPanelOffScreenPos = new Vector2(-(screenWidth / 2f + lPanel.rect.width / 2f + offScreenOffset), lPanelOnScreenPos.y);
        rPanelOffScreenPos = new Vector2(screenWidth / 2f + rPanel.rect.width / 2f + offScreenOffset, rPanelOnScreenPos.y);
    }

    public void SetPanelsVisible(bool visible)
    {
        if (targetState != visible)
        {
            targetState = visible;
            currentAnimationTime = 0f;
            isAnimating = true;
        }
    }

    public void TogglePanels()
    {
        SetPanelsVisible(!targetState);
    }

    private void SetPanelsImmediate(bool visible)
    {
        if (visible)
        {
            lPanel.anchoredPosition = lPanelOnScreenPos;
            rPanel.anchoredPosition = rPanelOnScreenPos;
        }
        else
        {
            lPanel.anchoredPosition = lPanelOffScreenPos;
            rPanel.anchoredPosition = rPanelOffScreenPos;
        }

        isPanelsVisible = visible;
        targetState = visible;
    }

    public bool IsPanelsVisible => isPanelsVisible;
}

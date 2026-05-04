using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a timing-based hacking minigame where a moving arrow must be stopped in a specific zone.
/// </summary>
public class HackingMinigame : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [Tooltip("Movement speed of the UI arrow.")]
    public float arrowSpeed = 100f;

    [Header("UI References")]
    [Tooltip("The UI element that moves back and forth.")]
    public RectTransform arrow;
    [Tooltip("The container for the minigame.")]
    public RectTransform background;
    [Tooltip("The target zone that grants maximum points.")]
    public RectTransform greenZone;
    [Tooltip("The safety zone on the left of the green zone.")]
    public RectTransform yellowZoneLeft;
    [Tooltip("The safety zone on the right of the green zone.")]
    public RectTransform yellowZoneRight;
    [Tooltip("Slider representing the hacking progress.")]
    public Slider slider;

    private float greenZoneWidth, yellowZoneWidth, panelWidth;
    private int curScore = 0;
    private bool isMovingRight = true;
    private Computer currentComputer;
    private bool hasSetup = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Handles minigame initialization, input subscription, and UI control toggling.
    /// </summary>
    private void OnEnable()
    {
        if (!hasSetup) { hasSetup = true; return; }

        RandomiseZones();
        InputReader.Instance.HackingEvent += OnHackingButtonPressed;
        InputReader.Instance.ExitEvent += ExitHack;
        InputReader.Instance.ToggleControls(ControlType.UI);
    }

    /// <summary>
    /// Cleans up input listeners and resets controls on disable.
    /// </summary>
    private void OnDisable()
    {
        InputReader.Instance.HackingEvent -= OnHackingButtonPressed;
        InputReader.Instance.ExitEvent -= ExitHack;
        InputReader.Instance.ToggleControls(ControlType.Foot);
        curScore = 0;
    }

    /// <summary>
    /// Caches UI dimensions and initial state.
    /// </summary>
    private void Start()
    {
        greenZoneWidth = greenZone.rect.width;
        yellowZoneWidth = yellowZoneLeft.rect.width;
        panelWidth = background.rect.width;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates UI progress and moves the arrow.
    /// </summary>
    private void Update()
    {
        slider.value = curScore;
        MoveArrow();

        // Check for completion condition
        if (curScore >= 3) OnHackComplete();
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Initializes the game for a specific computer object.
    /// </summary>
    public void StartHacking(Computer computer)
    {
        currentComputer = computer;
        curScore = 0;
        RandomiseZones();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Moves target zones to a random position within the background panel.
    /// </summary>
    private void RandomiseZones()
    {
        float halfWidth = panelWidth / 2;
        float greenZoneCenter = Random.Range(-halfWidth + greenZoneWidth / 2, halfWidth - greenZoneWidth / 2);

        greenZone.anchoredPosition = new Vector2(greenZoneCenter, greenZone.anchoredPosition.y);
        yellowZoneLeft.anchoredPosition = new Vector2(greenZoneCenter - (greenZoneWidth / 2) - (yellowZoneWidth / 2), yellowZoneLeft.anchoredPosition.y);
        yellowZoneRight.anchoredPosition = new Vector2(greenZoneCenter + (greenZoneWidth / 2) + (yellowZoneWidth / 2), yellowZoneRight.anchoredPosition.y);
    }

    /// <summary>
    /// Moves the arrow UI element horizontally across the bar.
    /// </summary>
    private void MoveArrow()
    {
        float moveAmount = arrowSpeed * Time.deltaTime * (isMovingRight ? 1 : -1);
        arrow.anchoredPosition += new Vector2(moveAmount, 0);

        if (arrow.anchoredPosition.x > panelWidth / 2) isMovingRight = false;
        else if (arrow.anchoredPosition.x < -panelWidth / 2) isMovingRight = true;
    }

    /// <summary>
    /// Processes user input, checks zone collisions, and updates score.
    /// </summary>
    private void OnHackingButtonPressed()
    {
        if (IsArrowInZone(greenZone)) curScore += 2;
        else if (IsArrowInZone(yellowZoneLeft) || IsArrowInZone(yellowZoneRight)) curScore += 1;
        else curScore = Mathf.Max(curScore - 1, 0);

        SoundManager.Instance.PlayKeyboardSoundServerRpc(currentComputer.transform.position);
        RandomiseZones();
    }

    /// <summary>
    /// Utility to check if the arrow is currently within a specific RectTransform's bounds.
    /// </summary>
    private bool IsArrowInZone(RectTransform zone)
    {
        float arrowPosX = arrow.anchoredPosition.x;
        float zonePosX = zone.anchoredPosition.x;
        float zoneHalfWidth = zone.rect.width / 2;

        return arrowPosX >= (zonePosX - zoneHalfWidth) && arrowPosX <= (zonePosX + zoneHalfWidth);
    }

    #endregion

    #region Completion

    /// <summary>
    /// Logic for finishing a successful hack.
    /// </summary>
    private void OnHackComplete()
    {
        if (currentComputer != null) currentComputer.OnHackComplete();
        SoundManager.Instance.PlaySoundServerRpc(SoundType.HACK_COMPLETE, currentComputer.transform.position);
        ExitHack();
    }

    /// <summary>
    /// Closes the UI and reverts control schemes.
    /// </summary>
    private void ExitHack()
    {
        InputReader.Instance.ToggleControls(ControlType.Foot);
        gameObject.SetActive(false);
    }

    #endregion
}
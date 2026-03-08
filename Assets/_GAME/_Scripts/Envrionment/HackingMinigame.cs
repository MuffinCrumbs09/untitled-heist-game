using UnityEngine;
using UnityEngine.UI;

public class HackingMinigame : MonoBehaviour
{

    [Header("Settings")]
    public float arrowSpeed = 100f;

    [Header("UI")]
    public RectTransform arrow;
    public RectTransform background;
    public RectTransform greenZone;
    public RectTransform yellowZoneLeft;
    public RectTransform yellowZoneRight;
    public Slider slider;
    //Width
    private float greenZoneWidth;
    private float yellowZoneWidth;
    private float panelWidth;
    // Misc 
    private int curScore = 0;
    private bool isMovingRight = true;
    private Computer currentComputer;
    bool hasSetup = false;

    private void OnEnable()
    {
        if (!hasSetup)
        {
            hasSetup = true;
            return;
        }

        RandomiseZones();
        InputReader.Instance.HackingEvent += OnHackingButtonPressed;
        InputReader.Instance.ExitEvent += ExitHack;
        InputReader.Instance.ToggleControls(ControlType.UI);
    }

    private void OnDisable()
    {
        InputReader.Instance.HackingEvent -= OnHackingButtonPressed;
        InputReader.Instance.ExitEvent -= ExitHack;
        InputReader.Instance.ToggleControls(ControlType.Foot);
        curScore = 0;
    }

    private void Start()
    {
        greenZoneWidth = greenZone.rect.width;
        yellowZoneWidth = yellowZoneLeft.rect.width;
        panelWidth = background.rect.width;

        InputReader.Instance.HackingEvent += OnHackingButtonPressed;
        InputReader.Instance.ToggleControls(ControlType.UI);

        gameObject.SetActive(false);
    }

    private void Update()
    {
        slider.value = curScore;
        MoveArrow();

        // Check if hacking is complete
        if (curScore >= 3)
        {
            OnHackComplete();
        }
    }

    public void StartHacking(Computer computer)
    {
        currentComputer = computer;
        curScore = 0;
        RandomiseZones();
        gameObject.SetActive(true);
    }

    private void RandomiseZones()
    {
        float halfWidth = panelWidth / 2;

        // randomise greenzone
        float greenZoneCenter = Random.Range(-halfWidth + greenZoneWidth / 2, halfWidth - greenZoneWidth / 2);
        greenZone.anchoredPosition = new Vector2(greenZoneCenter, greenZone.anchoredPosition.y);
        greenZone.sizeDelta = new Vector2(greenZoneWidth, greenZone.sizeDelta.y);

        // set yellow zones
        yellowZoneLeft.anchoredPosition = new Vector2(greenZoneCenter - (greenZoneWidth / 2) - (yellowZoneWidth / 2), yellowZoneLeft.anchoredPosition.y);
        yellowZoneLeft.sizeDelta = new Vector2(yellowZoneWidth, yellowZoneLeft.sizeDelta.y);

        yellowZoneRight.anchoredPosition = new Vector2(greenZoneCenter + (greenZoneWidth / 2) + (yellowZoneWidth / 2), yellowZoneRight.anchoredPosition.y);
        yellowZoneRight.sizeDelta = new Vector2(yellowZoneWidth, yellowZoneRight.sizeDelta.y);
    }

    private void MoveArrow()
    {
        float moveAmount = arrowSpeed * Time.deltaTime * (isMovingRight ? 1 : -1);
        arrow.anchoredPosition += new Vector2(moveAmount, 0);

        if (arrow.anchoredPosition.x > panelWidth / 2)
            isMovingRight = false;
        else if (arrow.anchoredPosition.x < -panelWidth / 2)
            isMovingRight = true;
    }

    private void OnHackingButtonPressed()
    {
        // Check if the arrow is in the green zone
        if (IsArrowInZone(greenZone))
        {
            curScore += 2;
        }
        // Check if the arrow is in the yellow zones
        else if (IsArrowInZone(yellowZoneLeft) || IsArrowInZone(yellowZoneRight))
        {
            curScore += 1;
        }
        // Else remove a score
        else
        {
            curScore = Mathf.Max(curScore - 1, 0);
        }

        SoundManager.Instance.PlayKeyboardSoundServerRpc(currentComputer.transform.position);
        RandomiseZones();
    }

    private bool IsArrowInZone(RectTransform zone)
    {
        float arrowPosX = arrow.anchoredPosition.x;
        float zonePosX = zone.anchoredPosition.x;

        float zoneHalfWidth = zone.rect.width / 2;
        float minX = zonePosX - zoneHalfWidth;
        float maxX = zonePosX + zoneHalfWidth;

        return arrowPosX >= minX && arrowPosX <= maxX;
    }

    private void OnHackComplete()
    {
        if (currentComputer != null)
            currentComputer.OnHackComplete();

        SoundManager.Instance.PlaySoundServerRpc(SoundType.HACK_COMPLETE, currentComputer.transform.position);
        InputReader.Instance.ToggleControls(ControlType.Foot);
        gameObject.SetActive(false);
    }

    private void ExitHack()
    {
        InputReader.Instance.ToggleControls(ControlType.Foot);
        gameObject.SetActive(false);
    }
}

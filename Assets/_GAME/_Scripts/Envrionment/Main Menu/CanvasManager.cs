using System.Collections;
using UnityEngine;

public enum CurrentCanvas
{
    MainMenu,
    InLobby,
    Username,
    Connecting,
    Options
}

public class CanvasManager : MonoBehaviour
{
    // Instance
    public static CanvasManager Instance;

    [Header("Setup")]
    [SerializeField] private CurrentCanvas Current = CurrentCanvas.MainMenu;
    [SerializeField] private Canvas[] Canvases;
    [SerializeField] private Animator _eraseAnim;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    public void PickCanvas(CurrentCanvas canvas)
    {
        if (canvas == Current) return;
        
        _eraseAnim.SetTrigger("Erase");
        StartCoroutine(PickCanvasRoutine(canvas));
    }

    private IEnumerator PickCanvasRoutine(CurrentCanvas canvas)
    {
        yield return new WaitForSeconds(.5f);

        Canvases[(int)Current].enabled = false; // Disable Old

        yield return new WaitForSeconds(1f);

        Canvases[(int)canvas].enabled = true; // Enable New
        Current = canvas;
    }
}

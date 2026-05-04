using System.Collections;
using UnityEngine;

/// <summary>
/// Defines the different UI screens available in the game.
/// </summary>
public enum CurrentCanvas
{
    MainMenu,
    InLobby,
    Username,
    Connecting,
    Options
}

/// <summary>
/// Singleton manager that handles switching between different UI canvases with a transition animation.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    #region Variables

    /// <summary> Static instance for global access. </summary>
    public static CanvasManager Instance;

    [Header("Setup")]
    [SerializeField] 
    [Tooltip("The starting canvas screen when the scene loads.")]
    private CurrentCanvas Current = CurrentCanvas.MainMenu;

    [SerializeField] 
    [Tooltip("List of all canvas components corresponding to the CurrentCanvas enum order.")]
    private Canvas[] Canvases;

    [SerializeField] 
    [Tooltip("Animator responsible for the screen transition/erase effect.")]
    private Animator _eraseAnim;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initiates a canvas switch by triggering the erase animation and starting the transition routine.
    /// </summary>
    /// <param name="canvas">The target canvas to switch to.</param>
    public void PickCanvas(CurrentCanvas canvas)
    {
        if (canvas == Current) return;
        
        _eraseAnim.SetTrigger("Erase");
        StartCoroutine(PickCanvasRoutine(canvas));
    }

    #endregion

    #region Private Logic

    /// <summary>
    /// Coroutine that handles the timing of disabling the old canvas and enabling the new one.
    /// </summary>
    /// <param name="canvas">The target canvas enum.</param>
    private IEnumerator PickCanvasRoutine(CurrentCanvas canvas)
    {
        yield return new WaitForSeconds(.5f);

        Canvases[(int)Current].enabled = false;

        yield return new WaitForSeconds(1f);

        Canvases[(int)canvas].enabled = true;
        Current = canvas;
    }

    #endregion
}
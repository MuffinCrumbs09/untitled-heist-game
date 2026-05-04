using UnityEngine;
using System.Collections;

/// <summary>
/// Makes an attached Light component flicker on and off at random intervals.
/// The on/off wait time is randomised within the MinMaxTime range each cycle.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField, Tooltip("The random interval range (in seconds) between each on/off toggle.\nX = min, Y = max")] private Vector2 MinMaxTime;
    [SerializeField, Tooltip("Whether the light starts in the on or off state when the scene loads.")] private bool IsOn;

    // Tracks whether a flicker coroutine is currently running to prevent stacking.
    private bool _isDoing;
    private Light _light;

    #region Unity Lifecycle
    private void Start()
    {
        _light = GetComponent<Light>();

        // Apply the initial on/off state from the Inspector.
        _light.enabled = IsOn;
    }

    private void Update()
    {
        // Only start a new toggle cycle if one isn't already running.
        if (!_isDoing)
            StartCoroutine(ToggleLight(!IsOn));
    }
    #endregion

    /// <summary>
    /// Waits a random duration then toggles the light to the given state.
    /// </summary>
    /// <param name="toggle">The state to set the light to after the wait.</param>
    private IEnumerator ToggleLight(bool toggle)
    {
        _isDoing = true;

        // Wait a random amount of time within the configured range before toggling.
        yield return new WaitForSeconds(Random.Range(MinMaxTime.x, MinMaxTime.y));

        _light.enabled = toggle;
        IsOn = toggle;

        _isDoing = false;
    }
}
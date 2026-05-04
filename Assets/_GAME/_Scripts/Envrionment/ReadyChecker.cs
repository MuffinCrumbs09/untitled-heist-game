using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Polls a list of MonoBehaviour components each frame and fires a UnityEvent
/// once every component in the list reports that it is ready.
/// Components must implement the IReady interface and return true from IsReady()
/// to be considered done. Any component that doesn't implement IReady is treated
/// as not ready and logs a warning.
/// The script disables itself after firing OnAllReady to prevent repeated invocations.
/// Useful for sequencing initialisation steps that depend on multiple async systems
/// </summary>
public class ReadyChecker : MonoBehaviour
{
    [Tooltip("The list of components to poll. Each must implement the IReady interface.\nAssign these in the Inspector — order does not matter, all must be ready simultaneously.")]
    [SerializeField] private List<MonoBehaviour> componentsToCheck;

    [Tooltip("Fired once when every component in componentsToCheck returns true from IsReady().\nWire up any logic here that should run only after all systems are initialised.")]
    public UnityEvent OnAllReady;

    private void Update()
    {
        // Check every component in the list — if any are not ready, return early
        foreach (var component in componentsToCheck)
        {
            if (component is IReady readyComponent)
            {
                if (!readyComponent.IsReady())
                    return;
            }
            else
            {
                // A component was added that doesn't implement IReady — warn and treat as not ready.
                Debug.LogWarning($"Component {component.name} does not implement IReady interface.");
                return;
            }
        }

        // All components reported ready — fire the event and stop checking.
        OnAllReady?.Invoke();
        enabled = false;
    }
}
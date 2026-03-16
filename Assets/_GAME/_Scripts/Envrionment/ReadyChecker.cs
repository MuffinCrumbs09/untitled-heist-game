using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ReadyChecker : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<MonoBehaviour> componentsToCheck;
    public UnityEvent OnAllReady;

    private void Update()
    {
        foreach (var component in componentsToCheck)
        {
            if (component is IReady readyComponent)
            {
                if (!readyComponent.IsReady())
                    return;
            }
            else
            {
                Debug.LogWarning($"Component {component.name} does not implement IReady interface.");
                return;
            }
        }

        OnAllReady?.Invoke();
        enabled = false; // Disable this script after invoking the event to prevent repeated checks
    }
}
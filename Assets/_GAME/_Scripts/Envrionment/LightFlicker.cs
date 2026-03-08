using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField, Tooltip("X = min, Y = max")] private Vector2 MinMaxTime;
    [SerializeField, Tooltip("Starts on or off")] private bool IsOn;

    private float _time;
    private bool _isDoing;
    private Light _light;

    private void Start()
    {
        _light = GetComponent<Light>();
        _light.enabled = IsOn;
    }

    private void Update()
    {
        if(!_isDoing)
            StartCoroutine(ToggleLight(!IsOn));
    }

    private IEnumerator ToggleLight(bool toggle)
    {
        _isDoing = true;

        yield return new WaitForSeconds(Random.Range(MinMaxTime.x, MinMaxTime.y));

        _light.enabled = toggle;
        IsOn = toggle;
        
        _isDoing = false;
    }
}

using Unity.Netcode;
using UnityEngine;

public class PlayerHeadbobController : NetworkBehaviour
{
    #region Settings
    [Header("Toggle")]
    public bool Enabled = true;

    [Header("Amplitude (Intensity)")]
    public float walkAmp   = 0.05f;
    public float sprintAmp = 0.10f;

    [Header("Frequency (Speed)")]
    [SerializeField] private float _frequency   = 10.0f;
    [SerializeField] private float _smoothSpeed = 10.0f;
    #endregion

    #region Private Variables
    private float   _timer;
    private float   _currentAmp;
    private Vector3 _bobOffset;          // Current bob displacement only
    private Vector3 _previousBobOffset;  // Last frame's displacement

    [SerializeField] private Transform _camera;
    #endregion

    // ─────────────────────────────────────────────
    void Start()
    {
        if (_camera == null) _camera = Camera.main.transform;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) enabled = false;
    }

    void Update()
    {
        if (!Enabled) return;

        // Remove last frame's bob before applying new one,
        // so this system stays independent of camera base position.
        _camera.localPosition -= _previousBobOffset;

        float moveMag = InputReader.Instance.MovementValue.magnitude;

        if (moveMag > 0.1f)
        {
            _currentAmp  = InputReader.Instance.IsSprinting ? sprintAmp : walkAmp;
            _timer      += Time.deltaTime * _frequency;

            Vector3 targetBob = FootStepMotion(_timer);

            // Lerp toward target for smooth entry/exit
            _bobOffset = Vector3.Lerp(_bobOffset, targetBob, _smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Let the timer coast to the nearest zero-crossing to avoid a pop
            _timer += Time.deltaTime * _frequency;
            float snapWindow = _frequency * Time.deltaTime * 1.5f; // ~1.5 frames of tolerance

            if (Mathf.Abs(Mathf.Sin(_timer)) < snapWindow)
                _timer = 0f;   // Close enough to neutral — safe to reset

            // Lerp bob offset back to zero
            _bobOffset = Vector3.Lerp(_bobOffset, Vector3.zero, _smoothSpeed * Time.deltaTime);
        }

        _previousBobOffset       = _bobOffset;
        _camera.localPosition   += _bobOffset;
    }

    private Vector3 FootStepMotion(float time)
    {
        float x = Mathf.Cos(time / 2f) * _currentAmp * 2f;
        float y = Mathf.Sin(time)       * _currentAmp;
        return new Vector3(x, y, 0f);
    }
}
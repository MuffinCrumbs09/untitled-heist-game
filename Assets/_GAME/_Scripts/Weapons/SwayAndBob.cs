using Unity.Netcode;
using UnityEngine;

public class SwayAndBob : NetworkBehaviour
{
    // ─────────────────────────────────── SWAY ────────────────────────────────
    [Header("Sway")]
    [Tooltip("How strongly the gun rotates against mouse movement.")]
    public float swayAmount        = 4f;

    [Tooltip("How strongly the gun tilts when strafing left/right.")]
    public float strafeRollAmount  = 3f;

    [Tooltip("Scale applied to all sway while aiming.")]
    [Range(0f, 1f)]
    public float aimSwayMultiplier = 0.25f;

    [Tooltip("How quickly the sway rotation returns to rest.")]
    public float swaySmoothing     = 8f;

    [Tooltip("Maximum rotation in degrees on any axis.")]
    public float maxSwayAngle      = 5f;

    // ─────────────────────────────────── BOB ─────────────────────────────────
    [Header("Weapon Bob")]
    [Tooltip("Multiplier applied to the camera headbob offset. 1 = matches camera exactly.")]
    public float bobPositionMultiplier = 0.4f;

    [Tooltip("How quickly the weapon position lerps toward the target bob position.")]
    public float bobSmoothing          = 12f;

    // ─────────────────────────────────── IDLE BREATH ─────────────────────────
    [Header("Idle Breath")]
    [Tooltip("Amplitude of the subtle idle sway when the player is standing still.")]
    public float idleBreathAmplitude  = 0.0015f;

    [Tooltip("Speed of the idle breathing cycle.")]
    public float idleBreathFrequency  = 1.2f;

    // ─────────────────────────────────── REFS ────────────────────────────────
    [Header("References")]
    [Tooltip("The PlayerHeadbobController on this player. Auto-found if left empty.")]
    [SerializeField] private PlayerHeadbobController _headbobController;

    [Tooltip("The Gun component on this weapon. Auto-found if left empty.")]
    [SerializeField] private Gun _gun;

    // ─────────────────────────────────── PRIVATE ─────────────────────────────
    private Quaternion _swayTargetRotation;
    private Quaternion _currentSwayRotation;

    private Vector3 _restPosition;
    private Vector3 _currentBobPosition;

    private float _idleTimer;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _restPosition        = transform.localPosition;
        _swayTargetRotation  = Quaternion.identity;
        _currentSwayRotation = Quaternion.identity;

        if (_headbobController == null)
            _headbobController = GetComponentInParent<PlayerHeadbobController>();

        if (_gun == null)
            _gun = GetComponentInParent<Gun>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) enabled = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        // While the sprint lowered pose is active, sway/bob would fight the
        // arm-model animation driven by Gun.AimControl, so we fade them out.
        bool sprintActive = _gun != null && _gun.IsSprintPoseActive;

        UpdateSway(sprintActive);
        UpdateBob(sprintActive);
    }

    // ─────────────────────── SWAY ────────────────────────────────────────────
    private void UpdateSway(bool suppress)
    {
        // When sprinting, drive sway toward identity so it doesn't jitter.
        if (suppress)
        {
            _currentSwayRotation = Quaternion.Slerp(
                _currentSwayRotation,
                Quaternion.identity,
                swaySmoothing * Time.deltaTime
            );
            transform.localRotation = _currentSwayRotation;
            return;
        }

        Vector2 look     = InputReader.Instance.LookValue;
        Vector2 movement = InputReader.Instance.MovementValue;
        bool isAiming    = InputReader.Instance.IsAiming;

        float multiplier = isAiming ? aimSwayMultiplier : 1f;

        float targetX = Mathf.Clamp(-look.y * swayAmount * multiplier, -maxSwayAngle, maxSwayAngle);
        float targetY = Mathf.Clamp( look.x * swayAmount * multiplier, -maxSwayAngle, maxSwayAngle);
        float targetZ = Mathf.Clamp(-movement.x * strafeRollAmount * multiplier, -maxSwayAngle, maxSwayAngle);

        _swayTargetRotation  = Quaternion.Euler(targetX, targetY, targetZ);
        _currentSwayRotation = Quaternion.Slerp(
            _currentSwayRotation,
            _swayTargetRotation,
            swaySmoothing * Time.deltaTime
        );

        transform.localRotation = _currentSwayRotation;
    }

    // ─────────────────────── BOB ─────────────────────────────────────────────
    private void UpdateBob(bool suppress)
    {
        // Return weapon to rest position smoothly while sprint pose is playing.
        if (suppress)
        {
            _currentBobPosition = Vector3.Lerp(
                _currentBobPosition,
                _restPosition,
                bobSmoothing * Time.deltaTime
            );
            transform.localPosition = _currentBobPosition;
            return;
        }

        bool isMoving = InputReader.Instance.MovementValue.magnitude > 0.1f;

        Vector3 targetPosition;

        if (isMoving)
        {
            Vector3 camBob = _headbobController != null
                ? _headbobController.BobOffset
                : Vector3.zero;

            targetPosition = _restPosition + camBob * bobPositionMultiplier;
            _idleTimer     = 0f;
        }
        else
        {
            _idleTimer += Time.deltaTime * idleBreathFrequency;

            float breathY = Mathf.Sin(_idleTimer * Mathf.PI * 2f) * idleBreathAmplitude;
            float breathX = Mathf.Cos(_idleTimer * Mathf.PI)      * idleBreathAmplitude * 0.5f;

            targetPosition = _restPosition + new Vector3(breathX, breathY, 0f);
        }

        _currentBobPosition = Vector3.Lerp(
            _currentBobPosition,
            targetPosition,
            bobSmoothing * Time.deltaTime
        );

        transform.localPosition = _currentBobPosition;
    }
}
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    #region Hidden Public
    [HideInInspector] public float Stamina { get; private set; }
    #endregion

    #region Public
    public PlayerMovementStats MovementStats;
    #endregion

    #region Private
    private CharacterController _cc;
    private bool canSprint;
    private bool isGrounded;
    private Vector3 _playerVelocity;
    private const float GRAVITY = -9.8f;
    #endregion


    #region Unity Events
    private void Start()
    {
        _cc = GetComponent<CharacterController>();
        Stamina = MovementStats.maxStamina;
    }

    void Update()
    {
        HandleStamina();

        isGrounded = _cc.isGrounded;
    }

    private void FixedUpdate()
    {
        CalculateMovement(InputReader.Instance.MovementValue, InputReader.Instance.IsSprinting);
    }

    private void OnEnable()
    {
        InputReader.Instance.JumpEvent += HandleJump;
    }

    private void OnDisable()
    {
        InputReader.Instance.JumpEvent -= HandleJump;
    }

    #endregion

    #region Functions
    // Calculate movement, if we can sprint, and apply it
    private void CalculateMovement(Vector2 input, bool isSprinting)
    {
        Vector3 moveDir = Vector3.zero;
        float curSpeed = (isSprinting && canSprint) ? MovementStats.sprintSpeed : MovementStats.walkSpeed;
        moveDir.x = input.x;
        moveDir.z = input.y;
        _cc.Move(transform.TransformDirection(curSpeed * Time.deltaTime * moveDir));

        _playerVelocity.y += GRAVITY * Time.deltaTime;
        if (isGrounded && _playerVelocity.y < 0)
            _playerVelocity.y = -2f;
        _cc.Move(_playerVelocity * Time.deltaTime);
    }

    // Handles stamina gain and loss
    private void HandleStamina()
    {
        Stamina = Math.Min(Stamina, MovementStats.maxStamina);

        if (InputReader.Instance.IsSprinting && canSprint)
            Stamina -= MovementStats.staminaDrainSpeed;
        else
            Stamina += 1;

        if (canSprint && Stamina <= 0)
            canSprint = false;

        if (!canSprint)
            canSprint = Stamina >= (MovementStats.maxStamina * 0.75);
    }

    private void HandleJump()
    {
        if (NetPlayerManager.Instance.GetCurrentPlayerState(GetComponent<NetworkBehaviour>().OwnerClientId) != PlayerState.MaskOn || !isGrounded)
            return;

        _playerVelocity.y += MovementStats.jumpHeight;
    }
    #endregion

    // If client doesn't own this, disable me
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) enabled = false;
    }
}

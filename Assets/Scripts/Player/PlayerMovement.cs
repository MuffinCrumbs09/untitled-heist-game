using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Public
    [SerializeField]
    private PlayerMovementStats MovementStats;
    #endregion

    #region Private
    private CharacterController _cc;
    private bool canSprint;
    private bool isGrounded;
    private Vector3 _playerVelocity;
    private const float GRAVITY = -9.8f;
    #endregion

    #region Stamina
    private float _stamina;
    #endregion

    #region Unity Events
    private void Start()
    {
        _cc = GetComponent<CharacterController>();
        _stamina = MovementStats.maxStamina;
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
        _stamina = Math.Min(_stamina, MovementStats.maxStamina);

        if (InputReader.Instance.IsSprinting && canSprint)
            _stamina -= MovementStats.staminaDrainSpeed;
        else
            _stamina += 1;

        if (canSprint && _stamina <= 0)
            canSprint = false;

        if (!canSprint)
            canSprint = _stamina >= 40;
    }
    #endregion
}

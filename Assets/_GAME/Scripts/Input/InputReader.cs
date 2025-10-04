using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputReader : MonoBehaviour, Controls.IOnFootActions
{
    #region Public
    public static InputReader Instance;
    public Vector2 MovementValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsFiring { get; private set; }
    #endregion

    #region Private
    private Controls _controls;
    #endregion

    #region Actions
    public event Action ReloadEvent;
    #endregion

    #region Unity Events
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }

    private void Start()
    {
        _controls = new Controls();
        _controls.OnFoot.SetCallbacks(this);

        ToggleControls(true);
        ToggleCursor(false);
    }
    #endregion

    #region Functions
    // Toggle Controls
    public void ToggleControls(bool toggle)
    {
        if (toggle)
            _controls.OnFoot.Enable();
        else
            _controls.OnFoot.Disable();
    }

    // Toggle Cursor
    public void ToggleCursor(bool toggle)
    {
        Cursor.visible = toggle;

        if (toggle)
            Cursor.lockState = CursorLockMode.Confined;
        else
            Cursor.lockState = CursorLockMode.Locked;
    }
    #endregion

    #region Input
    public void OnMovement(InputAction.CallbackContext context) => MovementValue = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => LookValue = context.ReadValue<Vector2>();
    public void OnSprint(InputAction.CallbackContext context) => IsSprinting = context.performed;
    public void OnFire(InputAction.CallbackContext context) => IsFiring = context.performed;
    public void OnReload(InputAction.CallbackContext context) => ReloadEvent?.Invoke();
    #endregion
}
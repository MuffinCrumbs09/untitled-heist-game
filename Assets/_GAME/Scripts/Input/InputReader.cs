using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ControlType { Foot, UI }

public enum InputDeviceType { Gamepad, KeyboardMouse, None }

public class InputReader : MonoBehaviour, Controls.IOnFootActions, Controls.IUIActions
{

    #region Public
    public static InputReader Instance;
    public InputDeviceType CurrentInputDevice { get; private set; } = InputDeviceType.None;
    public Vector2 MovementValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsFiring { get; private set; }
    public bool IsAiming { get; private set; }
    public bool IsTabbing { get; private set; }
    #endregion

    #region Private
    private Controls _controls;
    [SerializeField] private bool MouseVisible = false;
    #endregion

    #region Actions
    public event Action ReloadEvent;
    public event Action MaskEvent;
    public event Action JumpEvent;
    public event Action InteractEvent;
    public event Action HackingEvent;
    public event Action ExitEvent;
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
        _controls.UI.SetCallbacks(this);

        ToggleControls(ControlType.Foot);
        ToggleCursor(MouseVisible);
    }
    #endregion

    #region Functions
    // Toggle Controls
    public void ToggleControls(ControlType type)
    {
        _controls.OnFoot.Disable();
        _controls.UI.Disable();

        switch (type)
        {
            case ControlType.Foot:
                {
                    _controls.OnFoot.Enable();
                    break;
                }
            case ControlType.UI:
                {
                    _controls.UI.Enable();
                    break;
                }
        }
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

    // Updates InputDevice Based On Input 
    private void UpdateInputDevice(InputAction.CallbackContext context)
    {
        if (context.control?.device is Gamepad)
            CurrentInputDevice = InputDeviceType.Gamepad;
        else if (context.control?.device is Keyboard || context.control?.device is Mouse)
            CurrentInputDevice = InputDeviceType.KeyboardMouse;
    }
    #endregion

    #region Input
    // OnFoot
    public void OnMovement(InputAction.CallbackContext context)
    {
        MovementValue = context.ReadValue<Vector2>();
        UpdateInputDevice(context);
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        LookValue = context.ReadValue<Vector2>();
        UpdateInputDevice(context);
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        IsSprinting = context.performed;
        UpdateInputDevice(context);
    }
    public void OnFire(InputAction.CallbackContext context)
    {
        IsFiring = context.performed;
        UpdateInputDevice(context);
    }
    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ReloadEvent?.Invoke();
            UpdateInputDevice(context);
        }
    }
    public void OnMask(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            MaskEvent?.Invoke();
            UpdateInputDevice(context);
        }
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            JumpEvent?.Invoke();
            UpdateInputDevice(context);
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InteractEvent?.Invoke();
            UpdateInputDevice(context);
        }
    }
    public void OnAim(InputAction.CallbackContext context)
    {
        IsAiming = context.performed;
        UpdateInputDevice(context);
    }
    public void OnTab(InputAction.CallbackContext context)
    {
        IsTabbing = context.performed;
        UpdateInputDevice(context);
    }

    // UI
    public void OnHacking(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HackingEvent?.Invoke();
            UpdateInputDevice(context);
        }
    }
    public void OnExit(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ExitEvent?.Invoke();
            UpdateInputDevice(context);
        }
    }
    #endregion
}
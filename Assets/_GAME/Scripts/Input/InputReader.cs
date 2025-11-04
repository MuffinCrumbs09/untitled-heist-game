using System;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ControlType
{
    Foot,
    UI
}

public class InputReader : MonoBehaviour, Controls.IOnFootActions, Controls.IUIActions
{

    #region Public
    public static InputReader Instance;
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
    #endregion

    #region Input
    // OnFoot
    public void OnMovement(InputAction.CallbackContext context) => MovementValue = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => LookValue = context.ReadValue<Vector2>();
    public void OnSprint(InputAction.CallbackContext context) => IsSprinting = context.performed;
    public void OnFire(InputAction.CallbackContext context) => IsFiring = context.performed;
    public void OnReload(InputAction.CallbackContext context) => ReloadEvent?.Invoke();
    public void OnMask(InputAction.CallbackContext context) => MaskEvent?.Invoke();
    public void OnJump(InputAction.CallbackContext context) => JumpEvent?.Invoke();
    public void OnInteract(InputAction.CallbackContext context) => InteractEvent?.Invoke();
    public void OnAim(InputAction.CallbackContext context) => IsAiming = context.performed;
    public void OnTab(InputAction.CallbackContext context) => IsTabbing = context.performed;

    // UI
    public void OnHacking(InputAction.CallbackContext context)
    {
        if(context.performed)
            HackingEvent?.Invoke();
    }
    #endregion
}
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : NetworkBehaviour
{
    #region Public
    [Header("Camera Settings")]
    public float xSens = 30f;
    public float ySens = 30f;
    public Camera Cam;
    public Camera weaponCam;
    #endregion

    #region Private
    // Camera Settings
    private float xRotation = 0f;

    // Recoil Settings
    private Vector3 _targetRecoil = Vector3.zero;
    [HideInInspector] public Vector3 _curRecoil = Vector3.zero;
    #endregion

    #region Unity Events
    private void LateUpdate()
    {
        CalculateLook(InputReader.Instance.LookValue);
    }
    #endregion

    #region Functions
    public void ApplyRecoil(GunData gun)
    {
        float recoilX = 0;
        float recoilY = 0;

        if (InputReader.Instance.IsAiming)
        {
            recoilX = Random.Range(-gun.aimMaxRecoil.x, gun.aimMaxRecoil.x) * gun.RecoilAmount;
            recoilY = Random.Range(-gun.aimMaxRecoil.y, gun.aimMaxRecoil.y) * gun.RecoilAmount;
        }
        else
        {
            recoilX = Random.Range(-gun.hipMaxRecoil.x, gun.hipMaxRecoil.x) * gun.RecoilAmount;
            recoilY = Random.Range(-gun.hipMaxRecoil.y, gun.hipMaxRecoil.y) * gun.RecoilAmount;
        }

        _targetRecoil += new Vector3(recoilX, recoilY, 0);
        _curRecoil = Vector3.MoveTowards(
            _curRecoil,
            _targetRecoil,
            Time.deltaTime * gun.RecoilSpeed
        );
    }

    public void ResetRecoil(GunData gun)
    {
        _curRecoil = Vector3.Lerp(_curRecoil, _targetRecoil, Time.deltaTime * gun.RecoilSpeed);
        _targetRecoil = Vector3.MoveTowards(
            _targetRecoil,
            Vector3.zero,
            Time.deltaTime * gun.ResetRecoilSpeed
        );
    }

    private void CalculateLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;
        float multiplier = InputReader.Instance.CurrentInputDevice == InputDeviceType.Gamepad ? 10 : 1;

        // Y Movement
        xRotation -= (mouseY * Time.deltaTime) * ySens * multiplier;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        // Apply Recoil to camera
        Cam.transform.localRotation = Quaternion.Euler(xRotation + _curRecoil.y, _curRecoil.x, 0);

        // X Movement
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSens * multiplier);
    }
    #endregion

    // If client doesn't own this, disable me
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Make sure only the local player can use this
            Cam.enabled = false;
            weaponCam.enabled = false;
            Destroy(GetComponent<AudioListener>());
            enabled = false;
        }
        else
        {
            // Disable the player model for the local.
            GetComponent<Renderer>().enabled = false;
        }
    }
}

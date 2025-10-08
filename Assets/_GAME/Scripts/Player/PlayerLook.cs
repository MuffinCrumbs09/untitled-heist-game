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
    public Transform AimTransform;
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
        float recoilX = Random.Range(-gun.MaxRecoil.x, gun.MaxRecoil.x) * gun.RecoilAmount;
        float recoilY = Random.Range(-gun.MaxRecoil.y, gun.MaxRecoil.y) * gun.RecoilAmount;

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

        // Y Movement
        xRotation -= (mouseY * Time.deltaTime) * ySens;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        // Apply Recoil to camera
        Cam.transform.localRotation = Quaternion.Euler(xRotation + _curRecoil.y, _curRecoil.x, 0);
        // Aim transforms exists so the player can shoot in all directions, without any problems.
        AimTransform = Cam.transform;

        // X Movement
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSens);
    }
    #endregion

    // If client doesn't own this, disable me
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Cam.enabled = false;
            enabled = false;
        }
    }
}

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class Gun : NetworkBehaviour
{
    #region Public
    public GunData GunData;
    public Transform GunMuzzle;
    public PlayerLook Look;
    public Transform AimTransform;
    public GameObject ArmModel;
    #endregion

    #region Private
    private float _curAmmo = 0f;
    private float _nextTimeToFire = 0f;

    private bool _isReloading = false;
    #endregion

    #region Unity Evets
    private void Start()
    {
        _curAmmo = GunData.MagazineSize;
        Look = transform.root.GetComponent<PlayerLook>();
        AimTransform = Look.Cam.transform;
    }
    #endregion

    #region Functions
    public void TryReload()
    {
        if (!_isReloading && _curAmmo < GunData.MagazineSize)
        {
            StartCoroutine(Reload());
        }
    }

    public bool CanShoot()
    {
        if (_isReloading)
            return false;

        if (_curAmmo <= 0f)
        {
            Debug.Log(GunData.GunName + " is out of ammo");
            return false;
        }

        return true;
    }

    public void TryShoot()
    {
        if (!CanShoot())
            return;

        if (Time.time >= _nextTimeToFire)
        {
            _nextTimeToFire = Time.time + (1 / GunData.FireRate);
            HandleShoot();
        }
    }

    private void HandleShoot()
    {
        _curAmmo--;

        Debug.Log(_curAmmo);
        Shoot();

        Look.ApplyRecoil(GunData);
    }

    private IEnumerator Reload()
    {
        _isReloading = true;

        //Play Anim
        Debug.Log(GunData.GunName + " is reloading");

        yield return new WaitForSeconds(GunData.ReloadTime);

        _curAmmo = GunData.MagazineSize;
        _isReloading = false;

        Debug.Log("Gun Reloaded");
    }

    #endregion

    #region Virtual

    public abstract void Shoot();

    public virtual void Update()
    {
        Look.ResetRecoil(GunData);
        AimControl();
    }
    public virtual void OnEnable() { }

    public virtual void OnDisable() { }
    #endregion

    private void AimControl()
    {
        if (InputReader.Instance.IsAiming)
        {
            ArmModel.transform.localPosition = GunData.AimPosition;
            ArmModel.transform.localEulerAngles = GunData.AimRotation;
        }
        else
        {
            ArmModel.transform.localPosition = Vector3.zero;
            ArmModel.transform.localEulerAngles = Vector3.zero;
        }
    }


    // If client doesn't own this, disable me
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) enabled = false;
    }
}
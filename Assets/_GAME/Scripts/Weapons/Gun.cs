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

    public void TryShoot()
    {
        if (_isReloading)
            return;

        if (_curAmmo <= 0f)
        {
            Debug.Log(GunData.GunName + " is out of ammo");
            return;
        }

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
    }
    public virtual void OnEnable() { }

    public virtual void OnDisable() { }
    #endregion


    // If client doesn't own this, disable me
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) enabled = false;
    }
}
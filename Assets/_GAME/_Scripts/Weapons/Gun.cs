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

    #region Protected
    protected IWeaponInput _weaponInput;
    protected bool _isAI = false;
    #endregion

    #region Private
    public float _curAmmo { private set; get; } = 0f;
    private float _nextTimeToFire = 0f;
    private bool _isReloading = false;
    #endregion

    // ── Sprint Lowered Pose ───────────────────────────────────────────────────
    [Header("Sprint Lowered Pose")]
    [Tooltip("Local position the arm model moves TO while sprinting.")]
    public Vector3 SprintPosition = new Vector3(0.15f, -0.25f, 0.1f);

    [Tooltip("Local euler angles the arm model rotates TO while sprinting.")]
    public Vector3 SprintRotation = new Vector3(30f, 15f, -10f);

    [Tooltip("How fast the arm model lerps into/out of the sprint pose.")]
    public float SprintLerpSpeed = 10f;

    /// <summary>True while the weapon is fully in (or animating into) the sprint lowered pose.</summary>
    public bool IsSprintPoseActive { get; private set; }

    // Internal blend weight 0 = rest/aim, 1 = sprint
    private float _sprintBlend = 0f;

    // ─────────────────────────────────────────────────────────────────────────

    #region Unity Events
    private void Awake()
    {
        _curAmmo = GunData.MagazineSize;
        Look = transform.root.GetComponent<PlayerLook>();

        if (Look != null)
        {
            AimTransform = Look.Cam.transform;
            GunData = Instantiate(GunData);
            _weaponInput = new PlayerWeaponInput();
        }
        else
        {
            _isAI = true;
            _weaponInput = transform.root.GetComponent<AIWeaponInput>();
            AimTransform = transform.root.GetComponent<EnemyShootingBrain>().gunAimTransform;
        }
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
            return false;

        // Block firing while the weapon is raised to the sprint pose.
        // Allow a tiny threshold so a tap of fire cancels sprint instead of blocking completely.
        if (_sprintBlend > 0.1f)
            return false;

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

        Shoot();

        if (Look != null)
        {
            Look.ApplyRecoil(GunData);
        }
    }

    private IEnumerator Reload()
    {
        _isReloading = true;

        if (!_isAI)
            SubtitleManager.Instance.ShowPlayerSubtitle("Reloading!");

        yield return new WaitForSeconds(GunData.ReloadTime);

        _curAmmo = GunData.MagazineSize;
        _isReloading = false;
    }

    #endregion

    #region Virtual

    public abstract void Shoot();

    public virtual void Update()
    {
        if (!_isAI)
            Look.ResetRecoil(GunData);

        AimControl();
    }
    public virtual void OnEnable() { }

    public virtual void OnDisable() { }
    #endregion

    private void AimControl()
    {
        // ── Determine target pose ────────────────────────────────────────────
        // Priority: sprint > aim > hip
        bool wantSprint = !_isAI
                          && InputReader.Instance.IsSprinting
                          && InputReader.Instance.MovementValue.magnitude > 0.1f
                          && !_weaponInput.IsAiming;

        float sprintTarget = wantSprint ? 1f : 0f;
        _sprintBlend = Mathf.Lerp(_sprintBlend, sprintTarget, SprintLerpSpeed * Time.deltaTime);
        IsSprintPoseActive = _sprintBlend > 0.01f;

        if (IsSprintPoseActive)
        {
            // Blend between rest (or aim) and sprint pose
            Vector3 basePos = _weaponInput.IsAiming ? GunData.AimPosition : Vector3.zero;
            Vector3 baseRot = _weaponInput.IsAiming ? GunData.AimRotation : Vector3.zero;

            ArmModel.transform.localPosition = Vector3.Lerp(basePos, SprintPosition, _sprintBlend);
            ArmModel.transform.localEulerAngles = Vector3.Lerp(baseRot, SprintRotation, _sprintBlend);
        }
        else if (_weaponInput.IsAiming)
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
        if (!IsOwner) StartCoroutine(DisableAfterSpawn());
    }

    private IEnumerator DisableAfterSpawn()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        enabled = false;
    }
}
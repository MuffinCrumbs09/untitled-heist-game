using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Rifle : Gun
{
    #region Variables
    private float recoilTimer = 0f;
    private float currentRecoilRotX = 0f;
    private float recoilVelocity = 0f;
    #endregion

    #region Unity Events
    public override void Update()
    {
        base.Update();

        if (!_isAI && !transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer) return;

        if (_weaponInput.IsFiring)
        {
            TryShoot();

            if (CanShoot())
                CalculateGunRecoil();
            else
                ResetRecoil();
        }
        else
            ResetRecoil();

        // Apply rotation
        Vector3 local = transform.localEulerAngles;
        local.x = currentRecoilRotX;
        transform.localEulerAngles = local;
    }

    private void ResetRecoil()
    {
        recoilTimer = 0f;
        currentRecoilRotX = Mathf.SmoothDamp(
            currentRecoilRotX,
            GunData.defaultAnimRot,
            ref recoilVelocity,
            0.1f
        );
    }

    private void CalculateGunRecoil()
    {
        recoilTimer += Time.deltaTime * GunData.RecoilSpeed;

        float oscillation = Mathf.Sin(recoilTimer * Mathf.PI * 2f) * 0.5f + 0.5f;
        float amplitude = GunData.maxAnimRot - GunData.defaultAnimRot;
        float targetRot = GunData.defaultAnimRot + oscillation * amplitude;

        currentRecoilRotX = Mathf.Lerp(currentRecoilRotX, targetRot, Time.deltaTime * GunData.RecoilSpeed);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Fixed: was incorrectly calling base.OnNetworkDespawn()

        if (_isAI) return;

        StartCoroutine(WaitForLocalPlayer());
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (_isAI) return;

        if (!transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer) return;
        InputReader.Instance.ReloadEvent -= TryReload;
    }
    #endregion

    #region Functions

    public override void Shoot()
    {
        // Notify shooting brain a shot was fired
        if (_isAI)
        {
            EnemyShootingBrain brain = transform.root.GetComponent<EnemyShootingBrain>();
            if (brain != null) brain.OnShotFired();
        }

        RaycastHit hit;
        Vector3 targetPos = Vector3.zero;
        Vector3 rayOrigin = _isAI
            ? AimTransform.position
            : Look.Cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));
        Vector3 shootDirection = _isAI ? GetAIShootDirection() : Look.Cam.transform.forward;

        if (Physics.Raycast(rayOrigin, shootDirection, out hit, GunData.Range, GunData.TargetLayer))
        {
            targetPos = hit.point;

            if (_isAI)
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Enemy, "Hit", this);
#endif
                PlayerStats hitHealth = hit.transform.root.GetComponentInChildren<PlayerStats>();
                if (hitHealth != null)
                    hitHealth.TakeDamageServerRpc(GunData.Damage, true);
            }
            else
            {
                if (hit.transform.TryGetComponent(out IDamageable damageable))
                {
                    damageable.ChangeHealth(-GunData.Damage, OwnerClientId);
                }
            }
        }
        else
        {
            targetPos = rayOrigin + shootDirection * GunData.Range;

            if (_isAI)
            {
#if UNITY_EDITOR
                LoggerEvent.Log(LogPrefix.Enemy, "Miss", this);
#endif
                Debug.DrawRay(rayOrigin, shootDirection * 10f, Color.red, 2f);
            }
        }

        SpawnBulletTrailServerRpc(GunMuzzle.position, targetPos);
        SoundManager.Instance.PlaySoundServerRpc(SoundType.RIFLE, transform.position);

        recoilTimer = Random.Range(0f, 1f);
    }

    /// <summary>
    /// Returns a shoot direction for the AI with randomised spread.
    /// Spread is scaled by distance to target — the further away, the less accurate.
    /// </summary>
    private Vector3 GetAIShootDirection()
    {
        Vector3 ideal = AimTransform.forward;

        // 1% of shots are pixel perfect
        if (Random.value <= 0.01f)
            return ideal;

        // 99% apply spread scaled by distance
        float distanceFraction = 0f;
        if (Physics.Raycast(AimTransform.position, ideal, out RaycastHit rangeCheck, GunData.Range, GunData.TargetLayer))
            distanceFraction = rangeCheck.distance / GunData.Range;

        float spreadDegrees = Mathf.Lerp(GunData.AIMinSpread, GunData.AISpreadAtRange, distanceFraction);
        spreadDegrees = Mathf.Min(spreadDegrees, GunData.AIMaxSpread);

        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-spreadDegrees, spreadDegrees),
            Random.Range(-spreadDegrees, spreadDegrees),
            0f
        );

        return randomRotation * ideal;
    }

    private void SetAllLayers(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetAllLayers(child.gameObject, newLayer);
    }

    [Rpc(SendTo.Server)]
    private void SpawnBulletTrailServerRpc(Vector3 startPos, Vector3 targetPos)
    {
        SpawnBulletTrailClientRpc(startPos, targetPos);
    }

    [ClientRpc]
    private void SpawnBulletTrailClientRpc(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(BulletFire(startPos, targetPos));
    }

    private IEnumerator BulletFire(Vector3 startPos, Vector3 targetPos)
    {
        GameObject bulletTrail = Instantiate(GunData.BulletTrailPrefab, startPos, Quaternion.identity);

        while (bulletTrail != null && Vector3.Distance(bulletTrail.transform.position, targetPos) > 0.1f)
        {
            bulletTrail.transform.position = Vector3.MoveTowards(bulletTrail.transform.position, targetPos, Time.deltaTime * GunData.BulletSpeed);
            yield return null;
        }

        Destroy(bulletTrail);
    }
    #endregion

    private IEnumerator WaitForLocalPlayer()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        if (transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer)
            InputReader.Instance.ReloadEvent += TryReload;
        else
            SetAllLayers(ArmModel, 0);
    }
}
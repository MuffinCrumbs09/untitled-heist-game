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
        // Reset Recoil
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
        // Gun Anim
        recoilTimer += Time.deltaTime * GunData.RecoilSpeed;

        float oscillation = Mathf.Sin(recoilTimer * Mathf.PI * 2f) * 0.5f + 0.5f;
        float amplitude = GunData.maxAnimRot - GunData.defaultAnimRot;

        float targetRot = (GunData.defaultAnimRot + oscillation * amplitude);

        currentRecoilRotX = Mathf.Lerp(currentRecoilRotX, targetRot, Time.deltaTime * GunData.RecoilSpeed);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (_isAI) return;

        if (transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer)
        {
            InputReader.Instance.ReloadEvent += TryReload;
        }
        else
        {
            // If we arent the local player, set their weapon layer mask back to default to stop weapon clipping
            SetAllLayers(ArmModel, 0);
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_isAI) return;

        if (!transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer) return;
        InputReader.Instance.ReloadEvent -= TryReload;
    }
    #endregion

    #region Functions

    public override void Shoot()
    {
        RaycastHit hit;
        Vector3 targetPos = Vector3.zero;
        Vector3 rayOrigin = _isAI ? AimTransform.position : Look.Cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));
        Vector3 shootDirection = _isAI ? AimTransform.forward : Look.Cam.transform.forward;

        if (Physics.Raycast(rayOrigin, shootDirection, out hit, GunData.Range, GunData.TargetLayer))
        {
            Debug.Log(GunData.GunName + " hit " + hit.collider.name);
            targetPos = hit.point;

            if (hit.transform.TryGetComponent(out Health hitHealth))
            {
                hitHealth.ChangeHealth(-GunData.Damage, transform.root.gameObject);
            }
        }
        else
        {
            targetPos = rayOrigin + shootDirection * GunData.Range;
            Debug.Log("Hit nothing");
        }

        SpawnBulletTrailServerRpc(GunMuzzle.position, targetPos);
        SoundManager.Instance.PlaySoundServerRpc(SoundType.RIFLE, transform.position);

        recoilTimer = Random.Range(0f, 1f);
    }

    private void SetAllLayers(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetAllLayers(child.gameObject, newLayer);
        }
    }

    [ServerRpc(RequireOwnership = true)]
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
}

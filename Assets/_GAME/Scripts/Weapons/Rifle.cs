using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Rifle : Gun
{
    #region Unity Events

    public override void Update()
    {
        base.Update();

        if (!transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer) return;

        if (InputReader.Instance.IsFiring)
            TryShoot();
    }

    public override void OnEnable()
    {
        base.OnEnable();

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

        if (!transform.root.GetComponent<NetworkBehaviour>().IsLocalPlayer) return;
        InputReader.Instance.ReloadEvent -= TryReload;
    }
    #endregion

    #region Functions

    public override void Shoot()
    {
        RaycastHit hit;
        Vector3 targetPos = Vector3.zero;
        Vector3 rayOrigin = Look.Cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));

        if (Physics.Raycast(rayOrigin, AimTransform.forward.normalized, out hit, GunData.Range, GunData.TargetLayer))
        {
            Debug.Log(GunData.GunName + " hit " + hit.collider.name);
            targetPos = hit.point;
        }
        else
        {
            targetPos =
                rayOrigin + (AimTransform.forward.normalized + Look._curRecoil) * GunData.Range;
            Debug.Log("Hit nothing");
        }

        SpawnBulletTrailServerRpc(GunMuzzle.position, targetPos);
        SoundManager.Instance.PlaySoundServerRpc(SoundType.RIFLE, transform.position);
    }

    private void SetAllLayers(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach(Transform child in obj.transform)
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

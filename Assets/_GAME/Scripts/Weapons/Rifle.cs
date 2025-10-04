using System.Collections;
using UnityEngine;

public class Rifle : Gun
{
    #region Unity Events

    public override void Update()
    {
        base.Update();

        if (InputReader.Instance.IsFiring)
            TryShoot();
    }

    public override void OnEnable()
    {
        base.OnEnable();

        InputReader.Instance.ReloadEvent += TryReload;
    }

    public override void OnDisable()
    {
        base.OnDisable();

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

        StartCoroutine(BulletFire(targetPos));
    }

    private IEnumerator BulletFire(Vector3 targetPos)
    {
        GameObject bulletTrail = Instantiate(GunData.BulletTrailPrefab, GunMuzzle.position, Quaternion.identity);

        while (bulletTrail != null && Vector3.Distance(bulletTrail.transform.position, targetPos) > 0.1f)
        {
            bulletTrail.transform.position = Vector3.MoveTowards(bulletTrail.transform.position, targetPos, Time.deltaTime * GunData.BulletSpeed);
            yield return null;
        }

        Destroy(bulletTrail);
    }
    #endregion
}

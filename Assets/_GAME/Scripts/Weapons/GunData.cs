using UnityEngine;

[CreateAssetMenu(fileName = "New GunData", menuName = "Gun Data", order = 0)]
public class GunData : ScriptableObject
{
    public string GunName;
    public LayerMask TargetLayer;

    [Header("Fire Config")]
    public float Range;
    public float FireRate;

    [Header("Reload Config")]
    public float MagazineSize;
    public float ReloadTime;

    [Header("Recoil Settings")]
    public float RecoilAmount;
    public Vector2 MaxRecoil;
    public float RecoilSpeed;
    public float ResetRecoilSpeed;

    [Header("VFX")]
    public GameObject BulletTrailPrefab;
    public float BulletSpeed;
}


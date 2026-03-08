using UnityEngine;

[CreateAssetMenu(fileName = "New GunData", menuName = "Gun Data", order = 0)]
public class GunData : ScriptableObject
{
    public string GunName;
    public LayerMask TargetLayer;

    [Header("Fire Config")]
    public float Range;
    public float FireRate;
    public int Damage;
    public float maxAnimRot;
    public float defaultAnimRot;

    [Header("Reload Config")]
    public float MagazineSize;
    public float ReloadTime;
    [Header("Aim Settings")]
    public Vector3 AimPosition;
    public Vector3 AimRotation;

    [Header("Recoil Settings")]
    public float RecoilAmount;
    public Vector2 hipMaxRecoil;
    public Vector2 aimMaxRecoil;
    public float RecoilSpeed;
    public float ResetRecoilSpeed;

    [Header("VFX")]
    public GameObject BulletTrailPrefab;
    public float BulletSpeed;
}

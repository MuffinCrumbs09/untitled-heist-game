using UnityEngine;

[CreateAssetMenu(fileName = "New GunData", menuName = "Gun Data", order = 0)]
public class GunData : ScriptableObject
{
    public string GunName;
    public LayerMask TargetLayer;

    [Header("Fire Config")]
    public float Range;
    public float FireRate;
    public float Damage;
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

    [Header("AI Accuracy")]
    public float AIMaxSpread = 5f;   // degrees of spread at close range
    public float AIMinSpread = 0.5f; // degrees of spread at point-blank
    public float AISpreadAtRange = 23f; // degrees of spread at max range
}

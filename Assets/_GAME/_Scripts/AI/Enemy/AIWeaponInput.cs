using UnityEngine;

public class AIWeaponInput : MonoBehaviour, IWeaponInput
{
    public bool IsFiring { get; private set; }
    public bool IsAiming { get; private set; }

    private Gun _curGun;

    void Start()
    {
        _curGun = GetComponentInChildren<Gun>();
    }

    public void TriggerReload()
    {
        _curGun.TryReload();
    }

    public void SetFiring(bool toggle)
    {
        IsFiring = toggle;
    }

    public void SetAiming(bool toggle)
    {
        IsAiming = toggle;
    }
}

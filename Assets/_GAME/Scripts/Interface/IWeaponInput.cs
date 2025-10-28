public interface IWeaponInput
{
    bool IsAiming { get; }
    bool IsFiring { get; }
    void TriggerReload();
}

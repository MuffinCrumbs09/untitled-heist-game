public class PlayerWeaponInput : IWeaponInput
{
    public bool IsAiming => InputReader.Instance.IsAiming;
    public bool IsFiring => InputReader.Instance.IsFiring;

    // Unused from interface
    public void TriggerReload() { }
}

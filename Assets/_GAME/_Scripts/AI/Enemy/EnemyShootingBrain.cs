using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class EnemyShootingBrain : MonoBehaviour
{
    [Header("Burst Settings")]
    public int burstMinShots = 3;
    public int burstMaxShots = 5;
    public float burstCooldown = 1f;


    [Header("References")]
    public Transform gunAimTransform;  // assign the AI's AimTransform in inspector

    private AIWeaponInput _weaponInput;
    private Gun _gun;

    private int _shotsRemainingInBurst = 0;
    private float _cooldownTimer = 0f;
    private bool _inCooldown = false;

    private void Awake()
    {
        _weaponInput = GetComponent<AIWeaponInput>();
        _gun = GetComponentInChildren<Gun>();
    }

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            this.enabled = false;
    }

    public void UpdateShooting(bool canSee, Vector3 targetPos)
    {
        AimAtTarget(targetPos); // Always track target, even during cooldown

        if (!canSee)
        {
            CeaseFire();
            return;
        }

        // Tick cooldown
        if (_inCooldown)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _inCooldown = false;
                StartBurst();
            }
            return;
        }

        // Currently in a burst
        if (_shotsRemainingInBurst > 0)
        {
            if (!_gun.CanShoot())
            {
                // Out of ammo mid-burst — reload and end burst
                _weaponInput.TriggerReload();
                EndBurst();
                return;
            }

            _weaponInput.SetFiring(true);
            _gun.TryShoot();

            // TryShoot internally gates on fire rate, so we track shots
            // by listening to ammo dropping rather than counting calls.
            // Instead, count down when a shot actually fired this frame.
            // We compare ammo before/after to know if a shot landed.
        }
        else
        {
            // Burst exhausted — go to cooldown
            EndBurst();

        }
    }

    private void StartBurst()
    {
        _shotsRemainingInBurst = Random.Range(burstMinShots, burstMaxShots + 1);
        _weaponInput.SetFiring(true);
    }

    private void EndBurst()
    {
        CeaseFire();
        _inCooldown = true;
        _cooldownTimer = burstCooldown;
    }

    public void CeaseFire()
    {
        _weaponInput.SetFiring(false);
        _shotsRemainingInBurst = 0;
    }

    // Called by Gun (or a wrapper) when a shot is confirmed fired
    public void OnShotFired()
    {
        _shotsRemainingInBurst = Mathf.Max(0, _shotsRemainingInBurst - 1);
    }

    public void AimAtTarget(Vector3 targetPos)
    {
        if (gunAimTransform == null) return;

        // Aim slightly above ground level — target center of mass not feet
        Vector3 aimPoint = targetPos + Vector3.up * 0.5f;
        gunAimTransform.LookAt(aimPoint);

#if UNITY_EDITOR
        Debug.DrawRay(gunAimTransform.position, gunAimTransform.forward * 20f, Color.green);
#endif
    }
}

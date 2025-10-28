using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ShootInBurst", story: "[Agent] fires at [Target] in bursts of [BurstRange]", category: "Action", id: "8fc25377170af90080e4c74884ce3f8d")]
public partial class ShootInBurstAction : Action
{
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<Vector2Int> BurstRange;

    // Convert to scriptable object
    private float BurstDelay = 0.15f;
    private float BurstCooldown = 1.5f;
    private float MinAccuracyDist = 10f;
    private float MaxAccuracyDist = 30f;
    private float MaxSpreadAngle = 15f;

    // Components
    private AIWeaponInput _weaponInput;
    private Gun _gun;
    // Misc
    private float _nextBurstTime;
    private int _shotsInBurst;
    private float _nextShotTime;
    private bool _isBursting;
    private bool _isAiming;
    private int _burstSize;

    int burstTimes;

    protected override Status OnStart()
    {
        _weaponInput = Agent.Value.GetComponent<AIWeaponInput>();
        _gun = Agent.Value.GetComponentInChildren<Gun>();

        _burstSize = UnityEngine.Random.Range(BurstRange.Value.x, BurstRange.Value.y + 1);
        _nextBurstTime = 0f;
        _shotsInBurst = 0;
        _isBursting = false;
        _isAiming = UnityEngine.Random.Range(0f, 1f) == 0 ? true : false;
        burstTimes = 0;   

        return Status.Running;
    }

    protected override Status OnUpdate()
    {

        if (!_gun.CanShoot())
        {
            _weaponInput.SetFiring(false);
            _weaponInput.TriggerReload();
            return Status.Success;
        }

        if (Time.time >= _nextBurstTime)
        {
            if (!_isBursting)
            {
                _isBursting = true;
                _shotsInBurst = 0;
            }

            if (_shotsInBurst < _burstSize && Time.time >= _nextShotTime)
            {
                AimAtTargetWithSpread();
                _weaponInput.SetFiring(true);
                _weaponInput.SetAiming(_isAiming);

                _shotsInBurst++;
                _nextShotTime = Time.time + BurstDelay;
            }
            else if (_shotsInBurst >= _burstSize)
            {
                _weaponInput.SetFiring(false);
                _weaponInput.SetAiming(false);
                _isBursting = false;
                _nextBurstTime = Time.time + BurstCooldown;
                burstTimes++;

                if (burstTimes >= 3)
                    return Status.Success;
            }
        }
        else
        {
            _weaponInput.SetFiring(false);
            _weaponInput.SetAiming(false);
        }


        return Status.Running;
    }

    protected override void OnEnd()
    {
        _weaponInput.SetFiring(false);
        _weaponInput.SetAiming(false);
    }

    private void AimAtTargetWithSpread()
    {
        Vector3 directionToTarget = Target.Value.transform.position - Agent.Value.transform.position;
        float distance = directionToTarget.magnitude;

        float spreadAngle = CalculateSpread(distance);

        Vector3 spread = new Vector3(
            UnityEngine.Random.Range(-spreadAngle, spreadAngle),
            UnityEngine.Random.Range(-spreadAngle, spreadAngle),
            0f
        );

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        Quaternion spreadRotation = targetRotation * Quaternion.Euler(spread);

        Agent.Value.transform.rotation = Quaternion.Slerp(
            Agent.Value.transform.rotation,
            spreadRotation,
            Time.deltaTime * 10f
        );
    }

    private float CalculateSpread(float distance)
    {
        if (distance <= MinAccuracyDist)
        {
            return 0f;
        }
        else if (distance >= MaxAccuracyDist)
        {
            return MaxSpreadAngle;
        }
        else
        {
            float normalizedDistance = (distance - MinAccuracyDist) / (MaxAccuracyDist - MinAccuracyDist);
            return Mathf.Lerp(0f, MaxSpreadAngle, normalizedDistance);
        }
    }
}


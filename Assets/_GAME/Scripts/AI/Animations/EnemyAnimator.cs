using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Unity.Netcode.Components;

public class EnemyAnimator : NetworkBehaviour
{
    private static readonly int moving = Animator.StringToHash("Moving");
    private static readonly int aiming = Animator.StringToHash("Aiming");
    private static readonly int blend = Animator.StringToHash("Speed");

    private Animator _animator;
    private NetworkAnimator _networkAnimator;
    private NavMeshAgent _navMeshAgent;
    private AIWeaponInput _weaponInput;

    private bool _isStrafing;

    public override void OnNetworkSpawn()
    {
        _animator = GetComponent<Animator>();
        _networkAnimator = GetComponent<NetworkAnimator>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _weaponInput = GetComponent<AIWeaponInput>();
    }

    private void Update()
    {
        if (!IsServer) return;

        _animator.SetBool(moving, _navMeshAgent.velocity.sqrMagnitude > 0.01f);
        _animator.SetBool(aiming, _weaponInput.IsAiming);
        _animator.SetFloat(blend, _isStrafing ? 0f : 1f);
    }

    public void SetStrafing(bool isStrafing)
    {
        _isStrafing = isStrafing;
    }
}

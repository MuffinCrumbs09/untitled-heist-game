using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerAnimator : NetworkBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");

    private bool _masked = false;
    public Animator _animator;
    public ClientNetworkAnimator networkAnimator;

    public override void OnNetworkSpawn()
    {
        // Only the owner drives animation state; NetworkAnimator syncs it to others
        if (!IsOwner) enabled = false;
    }

    private void Update()
    {
        float moveAmount = InputReader.Instance.MovementValue.magnitude;
        bool isSprinting = InputReader.Instance.IsSprinting;

        float targetSpeed = moveAmount > 0.1f ? (isSprinting ? 1f : 0.5f) : 0f;

        _animator.SetFloat(Speed, targetSpeed, 0.1f, Time.deltaTime);

        if (!_masked && NetPlayerManager.Instance.GetCurrentPlayerState(NetworkManager.Singleton.LocalClientId) == PlayerState.MaskOn)
            MaskedUp();
    }

    private void MaskedUp()
    {
        _masked = true;
        networkAnimator.SetLayerWeight(1, 1f);
    }
}

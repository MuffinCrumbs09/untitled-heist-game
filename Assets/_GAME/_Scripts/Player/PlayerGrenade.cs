using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerGrenade : NetworkBehaviour
{
    [Header("Grenade Settings")]
    [SerializeField] private float grenadeCooldown = 30f;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float throwUpwardAngle = 10f; // Degrees above look dir

    [Header("References")]
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private GameObject grenadePrefab;

    // Read by PlayerUI
    public float CooldownRemaining { get; private set; } = 0f;
    public float GrenadeCooldown => grenadeCooldown;
    public bool IsReady => CooldownRemaining <= 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        InputReader.Instance.MaskEvent += OnMaskButtonPressed;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        InputReader.Instance.MaskEvent -= OnMaskButtonPressed;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (CooldownRemaining > 0f)
            CooldownRemaining -= Time.deltaTime;
    }

    private void OnMaskButtonPressed()
    {
        // First press is for masking up
        PlayerState state = NetPlayerManager.Instance.GetCurrentPlayerState(NetworkManager.Singleton.LocalClientId);
        if (state == PlayerState.MaskOff)
            return;

        if (!IsReady) return;

        CooldownRemaining = grenadeCooldown;

        Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.5f;

        // Tilt so grenade arcs naturally
        Vector3 forward = throwOrigin != null ? throwOrigin.forward : transform.forward;
        Vector3 up = throwOrigin != null ? throwOrigin.up : Vector3.up;
        Vector3 throwDir = Quaternion.AngleAxis(-throwUpwardAngle, throwOrigin.right) * forward;

        Vector3 velocity = throwDir.normalized * throwForce;

        ThrowGrenadeServerRpc(origin, velocity);
    }

    [Rpc(SendTo.Server)]
    private void ThrowGrenadeServerRpc(Vector3 origin, Vector3 velocity)
    {
        if(grenadePrefab == null)
        {
            #if UNITY_EDITOR
            LoggerEvent.LogError(LogPrefix.Player, "Grenade prefab not assigned!", this);
            #endif
            return;
        }

        // Spawn grenade as netowrk object
        GameObject grenadeObj = Instantiate(grenadePrefab, origin, Quaternion.identity);
        NetworkObject netObj = grenadeObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        // Apply velocity and record ownership
        GrenadeProjectile grenade = grenadeObj.GetComponent<GrenadeProjectile>();
        grenade.Launch(velocity, OwnerClientId);
    }
}

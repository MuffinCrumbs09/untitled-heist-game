using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// Handles networked door interactions, rotation animations, and NavMesh obstacle states.
/// </summary>
public class Door : NetworkBehaviour, IInteractable, IReady
{
    [Header("Settings")]
    [SerializeField] private string interactionText = "Door";
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private Vector3 doorOpenAngles;
    [SerializeField] private Vector3 doorClosedAngles;

    public NetworkVariable<bool> isOpen = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NavMeshObstacle _obstacle;
    private bool _isReady = false;

    public override void OnNetworkSpawn()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
        _obstacle.enabled = false;

        if (doorClosedAngles == Vector3.zero) doorClosedAngles = transform.localEulerAngles;
        
        isOpen.OnValueChanged += (oldVal, newVal) => {
            StopAllCoroutines();
            StartCoroutine(AnimateDoor(newVal));
        };

        _isReady = true;
    }

    private IEnumerator AnimateDoor(bool open)
    {
        SoundManager.Instance.PlaySoundServerRpc(open ? SoundType.DOOR_OPEN : SoundType.DOOR_CLOSED, transform.position);

        Quaternion targetRot = Quaternion.Euler(open ? doorOpenAngles : doorClosedAngles);
        
        while (Quaternion.Angle(transform.localRotation, targetRot) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * openSpeed);
            yield return null;
        }
        transform.localRotation = targetRot;
    }

    #region Interface

    public void Interact() => ToggleDoorServerRpc();

    public string InteractText() => isOpen.Value ? $"Close {interactionText}" : $"Open {interactionText}";

    public bool CanInteract() => true;

    public bool IsReady() => _isReady;

    #endregion

    [Rpc(SendTo.Server)]
    public void ToggleDoorServerRpc() => isOpen.Value = !isOpen.Value;
}
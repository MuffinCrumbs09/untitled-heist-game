using UnityEngine;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactRange = 2f;
    private float checkTime;

    IInteractable interacion;
    IInteractable previousInteraction;

    #region Serialized Private
    public GameObject ArmModel;
    #endregion

    #region Unity Events
    private void OnEnable()
    {
        NetPlayerManager.Instance.playerStates.OnListChanged += UpdatePlayerStates;
    }

    private void OnDisable()
    {
        NetPlayerManager.Instance.playerStates.OnListChanged -= UpdatePlayerStates;

        if (IsOwner)
        {
            InputReader.Instance.MaskEvent -= MaskOn;
            InputReader.Instance.InteractEvent -= TryInteract;
        }

        HandleLootExit();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        UpdateInteractions();
        HandleLootInteractionState();
    }

    private void UpdateInteractions()
    {
        Ray ray = new(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                continue;

            if (hit.transform.TryGetComponent(out IInteractable interact))
            {
                interacion = interact;
                return;
            }
            else if (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out IInteractable parentInteract))
            {
                interacion = parentInteract;
                return;
            }
        }

        interacion = null;
    }
    #endregion
    private void HandleLootInteractionState()
    {
        if (interacion != previousInteraction)
        {
            HandleLootExit();
            HandleLootEnter();

            previousInteraction = interacion;
        }
    }

    private void HandleLootEnter()
    {
        if (interacion is Loot loot)
        {
            loot.OnPlayerEnter();
        }
    }

    private void HandleLootExit()
    {
        if (previousInteraction is Loot loot)
        {
            loot.OnPlayerExit();
        }
    }

    public string GiveNearbyInteractText()
    {
        if (interacion == null)
            return string.Empty;

        return interacion.InteractText();
    }

    private void TryInteract()
    {
        if (interacion == null)
            return;

        interacion.Interact();
    }

    private void MaskOn()
    {
        ulong cliendID = NetworkManager.Singleton.LocalClientId;

        if (NetPlayerManager.Instance.GetCurrentPlayerState(cliendID) != PlayerState.MaskOff)
            return;

        NetPlayerManager.Instance.SetPlayerStateServerRpc(PlayerState.MaskOn);

        Debug.Log(NetPlayerManager.Instance.GetCurrentPlayerState(cliendID));
    }

    private void UpdatePlayerStates(NetworkListEvent<NetPlayerState> changeEvent)
    {
        GameObject changedPlayer = null;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player.GetComponent<NetworkBehaviour>().OwnerClientId == changeEvent.Value.clientID)
            {
                changedPlayer = player;
                break;
            }
        }

        PlayerInteraction armModel = changedPlayer.GetComponent<PlayerInteraction>();
        armModel.SetArmModelVisibility(true);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InputReader.Instance.MaskEvent += MaskOn;
            InputReader.Instance.InteractEvent += TryInteract;
        }

        SetArmModelVisibility(false);
    }

    private void SetArmModelVisibility(bool isVisible)
    {
        if (isVisible)
        {
            Animator anim = ArmModel.GetComponentInChildren<Animator>();
            anim.enabled = true;
        }
        Renderer[] renderers = ArmModel.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = isVisible;
        }

        Gun gun = ArmModel.GetComponentInChildren<Gun>();
        if (gun != null)
        {
            gun.enabled = isVisible;
        }
    }

    // Draw gizmos for debug
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
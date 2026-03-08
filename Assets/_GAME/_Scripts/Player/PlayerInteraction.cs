using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Network References")]
    public GameObject GunModel;
    public GameObject ArmModel;
    public MeshRenderer gunRender;

    IInteractable interacion;
    IInteractable previousInteraction;

    #region Serialized Private
    [Header("Local References")]
    [SerializeField] private float interactRange = 2f;
    #endregion

    #region Unity Events
    private void OnEnable()
    {
        NetPlayerManager.Instance.playerData.OnListChanged += UpdatePlayerStates;
    }

    private void OnDisable()
    {
        NetPlayerManager.Instance.playerData.OnListChanged -= UpdatePlayerStates;

        if (IsOwner)
        {
            InputReader.Instance.MaskEvent -= MaskOn;
            InputReader.Instance.InteractEvent -= TryInteract;
        }

        HandleUIExit();
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
            HandleUIExit();
            HandleUIEnter();

            previousInteraction = interacion;
        }
    }

    private void HandleUIEnter()
    {
        if (interacion is Loot loot)
            loot.OnPlayerEnter();
        else if (interacion is Drill drill)
            drill.OnPlayerEnter();
    }

    private void HandleUIExit()
    {
        if (previousInteraction is Loot loot)
            loot.OnPlayerExit();
        else if (interacion is Drill drill)
            drill.OnPlayerExit();
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
        SetArmModelVisibility(true);
    }

    private void UpdatePlayerStates(NetworkListEvent<NetPlayerData> changeEvent)
    {
        ulong targetID = changeEvent.Value.CLIENTID;
        GameObject changedPlayer = null;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player.GetComponent<NetworkBehaviour>().OwnerClientId == targetID)
            {
                changedPlayer = player;
                break;
            }
        }

        if (changedPlayer == NetworkManager.Singleton.LocalClient.PlayerObject)
            return;

        // if (changedPlayer.TryGetComponent(out PlayerAnimator targetAnimator))
        //     targetAnimator.networkAnimator.SetLayerWeight(1, 1f);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InputReader.Instance.MaskEvent += MaskOn;
            InputReader.Instance.InteractEvent += TryInteract;
        }
        else
        {
            gunRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
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

#if UNITY_EDITOR
    // Draw gizmos for debug
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
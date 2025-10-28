using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using Unity.Burst.Intrinsics;
using System.Collections;

public class PlayerInteraction : NetworkBehaviour
{
    float interactRange = 2f;
    float checkTime;

    List<IInteractable> nearbyInteractions = new List<IInteractable>();

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
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        checkTime += Time.deltaTime;

        if (checkTime > 0.2f)
        {
            checkTime = 0;
            FindNearbyInteractions();
        }
    }
    #endregion

    public string GiveNearbyInteractText()
    {
        if (nearbyInteractions.Count == 0)
            return string.Empty;

        return nearbyInteractions[0].InteractText();
    }

    private void FindNearbyInteractions()
    {
        nearbyInteractions.Clear();
        Collider[] nearbyCol = Physics.OverlapSphere(transform.position, interactRange);
        foreach (Collider col in nearbyCol)
        {
            if (col.TryGetComponent<IInteractable>(out IInteractable interactable))
                nearbyInteractions.Add(interactable);
        }
    }

    private void TryInteract()
    {
        if (nearbyInteractions.Count == 0)
            return;

        nearbyInteractions[0].Interact();
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
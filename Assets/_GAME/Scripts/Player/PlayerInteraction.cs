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

    IInteractable interacion;

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

        Ray ray = new Ray(Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f)), transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            if (hit.transform.TryGetComponent(out IInteractable interact))
                interacion = interact;
            else if (hit.transform.parent.TryGetComponent(out IInteractable parentInteract))
                interacion = parentInteract;
            else
                interacion = null;
        }
        else
            interacion = null;
    }
    #endregion

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
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    #region Private Serialized
    [Header("UI")]
    [SerializeField] private Slider StaminaSlider;
    #endregion

    #region Private
    // Components
    private PlayerMovement _playerMovement;

    //  Misc
    private GameObject _player;
    #endregion

    #region Unity Events
    private void Start()
    {
        StartCoroutine(WaitForLocalPlayer());
    }

    private void Update()
    {
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        UpdateStamina();
    }
    #endregion

    #region Functions
    private IEnumerator WaitForLocalPlayer()
    {
        // Wait until the local player exists
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        // Get the local player GameObject
        _player = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

        // Get player Components
        _playerMovement = _player.GetComponent<PlayerMovement>();

        // Set the max values
        StaminaSlider.maxValue = _playerMovement.MovementStats.maxStamina;
    }
    private void UpdateStamina()
    {
        float stamina = _playerMovement.Stamina;
        StaminaSlider.value = stamina;
    }
    #endregion
}

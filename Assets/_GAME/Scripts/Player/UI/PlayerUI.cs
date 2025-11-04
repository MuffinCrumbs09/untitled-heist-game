using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    #region Private Serialized
    [Header("UI")]
    [SerializeField] private Slider StaminaSlider;
    [SerializeField] private TMP_Text InteractText;
    [SerializeField] private TMP_Text AmmoText;
    [SerializeField] private TabMenuSlider tabMenu;
    #endregion

    #region Private
    // Components
    private PlayerMovement _playerMovement;
    private PlayerInteraction _playerInteract;
    private Gun _gun;

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
        UpdateInteractText();
        UpdateAmmoText();
        tabMenu.SetPanelsVisible(InputReader.Instance.IsTabbing);
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
        _playerInteract = _player.GetComponent<PlayerInteraction>();
        _gun = _playerInteract.ArmModel.transform.GetChild(0).GetChild(0).GetComponent<Gun>();

        // Set the max values
        StaminaSlider.maxValue = _playerMovement.MovementStats.maxStamina;
    }
    private void UpdateStamina()
    {
        float stamina = _playerMovement.Stamina;
        StaminaSlider.value = stamina;
    }

    private void UpdateInteractText()
    {
        string toAdd = _playerInteract.GiveNearbyInteractText();

        if (toAdd == string.Empty)
            InteractText.text = string.Empty;
        else
            InteractText.text = string.Format("Click E to {0}", toAdd);
    }
    
    private void UpdateAmmoText()
    {
        float ammo = _gun._curAmmo; float maxAmmo = _gun.GunData.MagazineSize;

        string text = string.Format("{0}/{1}", ammo, maxAmmo);

        AmmoText.text = text;
    }
    #endregion
}

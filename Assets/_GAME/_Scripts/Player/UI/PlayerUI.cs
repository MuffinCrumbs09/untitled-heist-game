using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

    #region Private Serialized
    [Header("UI - Materials")]
    [SerializeField] private Material ShieldSlider;
    [SerializeField] private Material[] HealthSliders;
    [Header("UI - Text")]
    [SerializeField] private TMP_Text InteractText;
    [SerializeField] private TMP_Text AmmoText;
    [SerializeField] private TMP_Text MaskText;
    [Header("UI - Misc")]
    [SerializeField] private TabMenuController tabMenu;
    #endregion

    #region Private
    // Components
    private PlayerMovement _playerMovement;
    private PlayerInteraction _playerInteract;
    private PlayerStats _playerStats;
    private Camera _playerCamera;
    private Gun _gun;

    //  Misc
    private GameObject _player;
    private float _healthPercent => _playerStats.GetHealthNormalised();
    private float _shieldPercent => _playerStats.GetShieldNormalised();
    #endregion

    #region Unity Events
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;
    }
    private void Start()
    {
        StartCoroutine(WaitForLocalPlayer());
    }

    private void Update()
    {
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        UpdateSliders();
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
        _playerInteract = _player.GetComponentInChildren<PlayerInteraction>();
        _playerStats = _player.GetComponentInChildren<PlayerStats>();
        _playerCamera = _player.GetComponent<PlayerLook>().Cam.transform.GetChild(2).GetComponent<Camera>();
        _gun = _playerInteract.ArmModel.transform.GetComponentInChildren<Gun>();

        GetComponent<Canvas>().worldCamera = _playerCamera;
    }
    private void UpdateSliders()
    {
        ShieldSlider.SetFloat("_Shield", _shieldPercent);

        for (int i = 0; i < HealthSliders.Length; i++)
            HealthSliders[i].SetFloat("_Health", _healthPercent);
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

    public void UpdateMask()
    {
        MaskText.enabled = false;
    }
    #endregion
}

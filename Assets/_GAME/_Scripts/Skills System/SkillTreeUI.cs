using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Config")]
    public SkillTreeConfig Config;

    [Header("Point Display")]
    public TMP_Text pointsRemainingLabel;

    [Header("Slots List")]
    public List<SkillSlotUI> _slots = new();
    public List<SkillDefinition> _skillDefinitions = new();

    [Header("UI Elements")]
    public Button confirmButton;
    public TMP_Text hoverNameLabel;
    public TMP_Text hoverDescLabel;

    public LobbySkillSelector _lobbySelector;

    private void Awake()
    {
        InitialiseLobby(_lobbySelector);
    }

    public void InitialiseLobby(LobbySkillSelector selector)
    {
        _lobbySelector = selector;
        _lobbySelector.OnPendingMaskChanged += _ => RefreshLobbyUI();

        foreach (SkillSlotUI skill in _slots)
        {
            var definition = Config.Get(skill.SkillType);
            skill.Initialise(definition, skill.SkillType, OnSlotClicked, OnSlotHoverEnter);
        }

        RefreshLobbyUI();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void RefreshLobbyUI()
    {
        if (_lobbySelector == null) return;

        int remaining = _lobbySelector.PointsRemaining();
        pointsRemainingLabel.text = $"Points: {remaining} / {Config.TotalPoints}";

        foreach (SkillSlotUI slot in _slots)
        {
            bool selected  = _lobbySelector.HasSkill(slot.SkillType);
            int  cost      = Config.Get(slot.SkillType)?.PointCost ?? 1;
            bool canAfford = remaining >= cost;

            slot.SetState(selected, canAfford || selected);
        }

        confirmButton.interactable = true;
    }

    private void OnSlotClicked(SkillType type)
    {
        _lobbySelector.ToggleSkill(type);
    }

    private void OnSlotHoverEnter(SkillDefinition def)
    {
        if (def == null) return;
        hoverNameLabel.text = def.DisplayName;
        hoverDescLabel.text = def.Description;
    }

    public void OnConfirmClicked()
    {
        _lobbySelector?.ConfirmSkills();
        Hide();
    }
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler
{
    [Header("Visuals")]
    public Image iconImage;
    public TMP_Text nameLabel;
    public TMP_Text descLabel;
    public TMP_Text costLabel;
    public Image backgroundImage;
    public GameObject selectedOverlay;
    public GameObject lockedOverlay;

    [Header("Colors")]
    public Color normalColor   = new Color(0.15f, 0.15f, 0.2f);
    public Color selectedColor = new Color(0.2f,  0.6f,  1.0f);
    public Color unaffordColor = new Color(0.1f,  0.1f,  0.1f);

    public SkillType SkillType;
    private SkillDefinition _definition;
    private Action<SkillType> _onClick;
    private Action<SkillDefinition> _onHoverEnter;

    public void Initialise(SkillDefinition def, SkillType type, Action<SkillType> onClick, Action<SkillDefinition> onHoverEnter)
    {
        SkillType     = type;
        _definition   = def;
        _onClick      = onClick;
        _onHoverEnter = onHoverEnter;

        if (def != null)
        {
            if (iconImage  != null) iconImage.sprite  = def.Icon;
            if (nameLabel  != null) nameLabel.text    = def.DisplayName;
            if (descLabel  != null) descLabel.text    = def.Description;
            if (costLabel  != null) costLabel.text    = $"{def.PointCost} pt";
        }

        GetComponent<Button>()?.onClick.AddListener(() => _onClick?.Invoke(SkillType));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHoverEnter?.Invoke(_definition);
    }

    public void SetState(bool selected, bool affordable)
    {
        if (selectedOverlay != null) selectedOverlay.SetActive(selected);
        if (lockedOverlay   != null) lockedOverlay.SetActive(!affordable && !selected);

        if (backgroundImage != null)
        {
            backgroundImage.color = selected    ? selectedColor
                                  : !affordable ? unaffordColor
                                  : normalColor;
        }

        if (GetComponent<Button>() is Button btn)
            btn.interactable = affordable || selected;
    }
}
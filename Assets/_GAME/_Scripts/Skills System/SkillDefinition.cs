using UnityEngine;

public enum SkillCategory { Gun, Health, Armour }

public enum SkillType
{
    // Gun
    FMJ,            // +25% damage
    Overclocked,    // +40% fire rate
    SpeedCola,      // -40% reload time
    ExtendedMag,    // +50% magazine size

    // Health
    Tough,          // +50 max health
    Medic,          // x2 health regen rate
    LastStand,      // Heal health when shield breaks
    Adrenaline,     // Sprint speed boost at low health

    // Armour
    Bulletproof,    // +15% bullet damage reduction
    RegenerativePlate, // x2 shield regen rate
    HardenedPlate,  // Shield absorbs damage even when broken (one time per regen cycle)
    QuickPlate      // Regen cooldown after taking damage reduced by 2s
}

[CreateAssetMenu(fileName = "New Skill", menuName = "Skill Tree/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    public SkillType SkillType;
    public SkillCategory Category;
    public string DisplayName;
    [TextArea(2, 4)]
    public string Description;
    public Sprite Icon;

    [Header("Cost")]
    [Range(1, 4)]
    public int PointCost = 1;
}
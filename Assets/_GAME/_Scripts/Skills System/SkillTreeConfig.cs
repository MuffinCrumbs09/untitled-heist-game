using UnityEngine;

[CreateAssetMenu(fileName = "SkillTreeConfig", menuName = "Skill Tree/Skill Tree Config")]
public class SkillTreeConfig : ScriptableObject
{
    [Header("Economy")]
    [Tooltip("Total skill points each player receives to spend.")]
    public int TotalPoints = 4;

    [Header("Gun Skills")]
    public SkillDefinition FMJ;
    public SkillDefinition Overclocked;
    public SkillDefinition SpeedCola;
    public SkillDefinition ExtendedMag;

    [Header("Health Skills")]
    public SkillDefinition Tough;
    public SkillDefinition Medic;
    public SkillDefinition LastStand;
    public SkillDefinition Adrenaline;

    [Header("Armour Skills")]
    public SkillDefinition Bulletproof;
    public SkillDefinition RegenerativePlate;
    public SkillDefinition HardenedPlate;
    public SkillDefinition QuickPlate;

    /// <summary>Returns the SkillDefinition matching a given SkillType.</summary>
    public SkillDefinition Get(SkillType type) => type switch
    {
        SkillType.FMJ               => FMJ,
        SkillType.Overclocked       => Overclocked,
        SkillType.SpeedCola         => SpeedCola,
        SkillType.ExtendedMag       => ExtendedMag,
        SkillType.Tough             => Tough,
        SkillType.Medic             => Medic,
        SkillType.LastStand         => LastStand,
        SkillType.Adrenaline        => Adrenaline,
        SkillType.Bulletproof       => Bulletproof,
        SkillType.RegenerativePlate => RegenerativePlate,
        SkillType.HardenedPlate     => HardenedPlate,
        SkillType.QuickPlate        => QuickPlate,
        _                           => null
    };
}
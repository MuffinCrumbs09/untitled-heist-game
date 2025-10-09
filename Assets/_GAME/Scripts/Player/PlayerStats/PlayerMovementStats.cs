using UnityEngine;

[CreateAssetMenu(fileName ="New Movement Stats", menuName ="Player Movement Stats")]
public class PlayerMovementStats : ScriptableObject
{
    [Header("Speed")]
    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    [Header("Stamina")]
    public float staminaDrainSpeed;
    public int maxStamina;
    [Header("Jump")]
    public float jumpHeight;
    [Header("Misc")]
    public int carryAmount;
}

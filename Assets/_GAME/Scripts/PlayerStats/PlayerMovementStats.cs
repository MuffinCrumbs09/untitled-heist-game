using UnityEngine;

[CreateAssetMenu(fileName ="New Movement Stats", menuName ="Player Movement Stats")]
public class PlayerMovementStats : ScriptableObject
{
    public float walkSpeed;
    public float sprintSpeed;
    public float staminaDrainSpeed;
    public int maxStamina;
    public float jumpHeight;
    public float crouchSpeed;
    public int carryAmount;
}

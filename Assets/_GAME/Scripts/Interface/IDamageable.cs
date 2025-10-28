using UnityEngine;

public interface IDamageable
{   
    public void ChangeHealth(int toChange, GameObject attacker);
}
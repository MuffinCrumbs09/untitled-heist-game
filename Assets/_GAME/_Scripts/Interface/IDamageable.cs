using UnityEngine;

public interface IDamageable
{   
    public void ChangeHealth(float toChange, GameObject attacker);
}
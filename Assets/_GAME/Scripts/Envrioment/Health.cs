using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth;
    private float health;

    public void ChangeHealth(int toChange)
    {
        health += toChange;

        if (health <= 0)
            Dead();
    }

    public void Dead()
    {
        Destroy(gameObject);
    }

    private void Start()
    {
        health = maxHealth;
    }
}

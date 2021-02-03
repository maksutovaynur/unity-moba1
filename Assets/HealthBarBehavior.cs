using UnityEngine;

public class HealthBarBehavior : MonoBehaviour, ChangeHealthListener
{
    public Damageable damage;
    public GameObject fill;

    private float maxHealth = 100.0f;
    private float health = 0.0f;
    private float initialScale;

    private void Start()
    {
        initialScale = fill.transform.localScale.x;
        damage.addChangeHealthListener(this);
    }

    public void ChangeHealth(int health)
    {
        this.health = health;
        ChangeFill();
    }

    public void SetMaxHealth()
    {
        maxHealth = damage.maxHealth;
        ChangeFill();
    }

    public void SetHealth()
    {
        health = damage.GetHealth();
        ChangeFill();
    }

    private void ChangeFill()
    {
        var oldScale = fill.transform.localScale;
        fill.transform.localScale = new Vector3(initialScale * health / maxHealth, oldScale.y, oldScale.z);
    }
}

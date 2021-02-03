using System;
using System.Linq;
using UnityEngine;


public interface ChangeHealthListener
{
    void ChangeHealth(int health);
}

public class Damageable : MonoBehaviour
{
    public int maxHealth = 100;
    private int health;
    private ChangeHealthListener[] callbackListeners;

    public void Start()
    {
        checkInit();
    }

    public void addChangeHealthListener(ChangeHealthListener listener)
    {
        checkInit();
        callbackListeners.Append(listener);
    }

    private void checkInit()
    {
        if (callbackListeners == null)
        {
            health = maxHealth;
            callbackListeners = new ChangeHealthListener[] { };
        }
    }

    private void NotifyHealthListeners()
    {
        checkInit();
        foreach (var listener in callbackListeners)
        {
            listener.ChangeHealth(health);
        }
    }
    
    public void MakeDamage(int value)
    {
        checkInit();
        health = Math.Max(health - value, 0);
        Debug.Log("Final Health = " + health);
        NotifyHealthListeners();
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    public int GetHealth()
    {
        checkInit();
        return health;
    }

    public void SetHealth(int health)
    {
        checkInit();
        this.health = Math.Max(0, Math.Min(health, maxHealth));
        Debug.Log("Damageable Set Health to " + this.health + ", tried to " + health);
        NotifyHealthListeners();
    }
}
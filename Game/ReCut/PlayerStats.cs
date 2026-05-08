using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReCut;

public class PlayerStats
{
    // Текущие значения
    public float Health { get; private set; }
    public float Mana { get; private set; }

    public float MaxHealth = 100f;
    public float MaxMana = 100f;
    public float ManaRegenSpeed = 15f; 

    public PlayerStats()
    {
        Health = MaxHealth;
        Mana = MaxMana;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (Mana < MaxMana)
        {
            Mana += ManaRegenSpeed * dt;
            if (Mana > MaxMana)
                Mana = MaxMana;
        }
    }

    public bool CanAfford(float cost)
    {
        if (Mana >= cost)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health < 0)
            Health = 0;
    }

    public void UseMana(float amount)
    {
        Mana -= amount;
        if (Mana < 0)
            Mana = 0;
    }

    public void Heal(float amount)
    {
        Health += amount;
        if (Health > MaxHealth)
            Health = MaxHealth;
    }
}
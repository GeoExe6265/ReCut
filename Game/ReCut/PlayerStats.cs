using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReCut;

public class PlayerStats
{
    public float Health { get; private set; }
    public float Mana { get; private set; }

    public float MaxHealth = 100f;
    public float MaxMana = 100f;
    public float ManaRegenSpeed = 15f; 
    public bool HasKnockback { get; private set; }
    public Vector2 KnockbackImpulse { get; private set; }

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

    public void TakeDamage(float amount, Vector2 knockbackDirection)
    {
        TakeDamage(amount);

        if (knockbackDirection != Vector2.Zero)
        {
            KnockbackImpulse = new Vector2(knockbackDirection.X * 560f, -220f);
            HasKnockback = true;
        }
    }

    public Vector2 ConsumeKnockback()
    {
        HasKnockback = false;
        return KnockbackImpulse;
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

    public void DrawHealthBar(SpriteBatch spriteBatch, Texture2D whitePixel, GameTime gameTime)
    {
        int barX = 20;
        int barY = 20;
        int barWidth = 230;
        int barHeight = 24;
        int innerX = barX + 2;
        int innerY = barY + 2;
        int innerWidth = barWidth - 4;
        int innerHeight = barHeight - 4;
        float healthRatio = MaxHealth > 0 ? Health / MaxHealth : 0f;

        if (healthRatio < 0f)
            healthRatio = 0f;

        if (healthRatio > 1f)
            healthRatio = 1f;

        spriteBatch.Draw(whitePixel, new Rectangle(barX, barY, barWidth, barHeight), Color.Black * 0.55f);
        spriteBatch.Draw(whitePixel, new Rectangle(innerX, innerY, innerWidth, innerHeight), Color.DimGray * 0.9f);

        int healthWidth = (int)(innerWidth * healthRatio);
        if (healthWidth > 0)
        {
            float waveTime = (float)gameTime.TotalGameTime.TotalSeconds;
            spriteBatch.Draw(whitePixel, new Rectangle(innerX, innerY + 1, healthWidth, innerHeight - 1), Color.DarkRed);

            int edgeX = innerX + healthWidth - 3;
            if (edgeX < innerX)
                edgeX = innerX;

            for (int y = 0; y < innerHeight; y++)
            {
                float wave = (float)Math.Sin(waveTime * 5.5f + y * 0.35f) * 2f;
                int waveWidth = 4 + (int)(Math.Sin(waveTime * 7f + y * 0.45f) * 2f);
                if (waveWidth < 2)
                    waveWidth = 2;

                spriteBatch.Draw(whitePixel, new Rectangle(edgeX + (int)wave, innerY + y, waveWidth, 1), Color.Red);
            }
        }
    }
}
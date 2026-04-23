using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ReCut;

public class DamageText
{
    public string Value;
    public float Alpha = 1.0f;
    private float _timer = 0;
    public bool IsDead = false;
    private Vector2 _velocity;
    public Vector2 Position;

    public DamageText(Vector2 pos, int damage)
    {
        Position = pos;
        Value = damage.ToString();
        Random rnd = new Random();
        _velocity = new Vector2((float)rnd.NextDouble() * 80 - 40, -120);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += _velocity * dt;
        _velocity.Y += 300f * dt;
        
        _timer += dt;
        if (_timer > 0.5f)
        {
            Alpha -= 2.0f * dt;
            if (Alpha <= 0)
            {
                IsDead = true;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        spriteBatch.DrawString(font, Value, Position + new Vector2(1, 1), Color.Black * Alpha);
        spriteBatch.DrawString(font, Value, Position, Color.White * Alpha);
    }
}
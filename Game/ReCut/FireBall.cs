using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ReCut;

public class FireBall
{
    private Vector2 _position;
    private Vector2 _velocity;
    private float _lifeTimer = 0f;
    private bool _isDead = false;

    private const float Speed = 420f;
    private const int Damage = 10;
    private const int Size = 14;
    private const float MaxLifeTime = 5f;

    public bool IsDead => _isDead;

    public FireBall(Vector2 position, Vector2 direction)
    {
        _position = position;

        if (direction == Vector2.Zero)
            direction = new Vector2(1f, 0f);

        direction.Normalize();
        _velocity = direction * Speed;
    }

    public Rectangle Hitbox => new Rectangle((int)_position.X - (Size / 2), (int)_position.Y - (Size / 2), Size, Size);

    public void Update(GameTime gameTime, List<CollisionObject> collisions, Vector2 playerPos, PlayerStats stats, List<DamageText> damageTexts)
    {
        if (_isDead)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _lifeTimer += dt;
        if (_lifeTimer >= MaxLifeTime)
        {
            _isDead = true;
            return;
        }

        Vector2 nextPosition = _position + _velocity * dt;
        Rectangle nextHitbox = new Rectangle((int)nextPosition.X - (Size / 2), (int)nextPosition.Y - (Size / 2), Size, Size);

        foreach (var collision in collisions)
        {
            if (collision.IsSpikes)
                continue;

            if (nextHitbox.Intersects(collision.Bounds))
            {
                _isDead = true;
                return;
            }
        }

        Rectangle playerRect = new Rectangle((int)playerPos.X + 24, (int)playerPos.Y + 14, 16, 18);
        if (nextHitbox.Intersects(playerRect))
        {
            stats?.TakeDamage(Damage);
            damageTexts.Add(new DamageText(playerPos + new Vector2(24, -10), Damage));
            _isDead = true;
            return;
        }

        _position = nextPosition;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D whitePixel)
    {
        if (_isDead || spriteBatch == null || whitePixel == null)
            return;

        Rectangle outer = new Rectangle((int)_position.X - 10, (int)_position.Y - 10, 20, 20);
        Rectangle mid = new Rectangle((int)_position.X - 7, (int)_position.Y - 7, 14, 14);
        Rectangle core = new Rectangle((int)_position.X - 3, (int)_position.Y - 3, 6, 6);

        spriteBatch.Draw(whitePixel, outer, Color.OrangeRed * 0.6f);
        spriteBatch.Draw(whitePixel, mid, Color.Orange);
        spriteBatch.Draw(whitePixel, core, Color.Yellow);
    }
}
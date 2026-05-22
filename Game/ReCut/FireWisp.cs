using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ReCut;

public class FireWisp
{
    private readonly Texture2D _texture;
    private readonly Vector2 _spawnPosition;
    private readonly List<FireBall> _fireBalls = new List<FireBall>();

    private Vector2 _position;
    private float _animTimer = 0f;
    private int _currentFrame = 0;
    private int _hp = 1;
    private bool _isDead = false;
    private float _attackTimer = 0f;
    private float _contactDamageTimer = 0f;
    private SpriteEffects _facing = SpriteEffects.None;

    private const int FrameWidth = 288;
    private const int FrameHeight = 288;
    private const float DrawScale = 0.125f;
    private const float ChaseRadius = 420f;
    private const float AttackRadius = ChaseRadius * 0.5f;
    private const float MoveSpeed = 125f;
    private const float ShootInterval = 3f;
    private const float ContactDamageCooldown = 0.6f;
    private const int ContactDamage = 10;

    public FireWisp(Texture2D texture, Vector2 position)
    {
        _texture = texture;
        _position = position;
        _spawnPosition = position;
    }

    public Rectangle Hitbox => new Rectangle((int)_position.X + 18, (int)_position.Y + 18, 36, 36);
    public bool IsDead() => _isDead;

    public bool TakeDamage(int amount)
    {
        if (_isDead)
            return false;

        _hp -= amount;
        if (_hp <= 0)
        {
            _isDead = true;
            return true;
        }

        return false;
    }

    public void Update(GameTime gameTime, Vector2 playerPos, List<CollisionObject> collisions, PlayerStats stats, List<DamageText> damageTexts)
    {
        if (_isDead)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _animTimer += dt;
        if (_animTimer > 0.08f)
        {
            _currentFrame = (_currentFrame + 1) % 10;
            _animTimer = 0f;
        }

        if (_contactDamageTimer > 0f)
        {
            _contactDamageTimer -= dt;
            if (_contactDamageTimer < 0f)
                _contactDamageTimer = 0f;
        }

        Vector2 playerCenter = playerPos + new Vector2(32f, 16f);
        Vector2 wispCenter = _position + new Vector2((FrameWidth * DrawScale) * 0.5f, (FrameHeight * DrawScale) * 0.5f);
        float distanceToPlayer = Vector2.Distance(wispCenter, playerCenter);
        float chaseRadius = ChaseRadius;
        float attackRadius = AttackRadius;

        if (distanceToPlayer <= chaseRadius)
        {
            Vector2 moveDirection = playerCenter - wispCenter;
            if (moveDirection != Vector2.Zero)
                moveDirection.Normalize();

            _facing = moveDirection.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (distanceToPlayer > attackRadius)
            {
                Vector2 nextPosition = _position + moveDirection * (MoveSpeed * dt);
                Rectangle nextHitbox = GetHitbox(nextPosition);

                bool blocked = false;
                foreach (var collision in collisions)
                {
                    if (collision.IsOneWay || collision.IsSpikes)
                        continue;

                    if (nextHitbox.Intersects(collision.Bounds))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                    _position = nextPosition;
            }

            _attackTimer += dt;
            if (distanceToPlayer <= attackRadius && _attackTimer >= ShootInterval)
            {
                Vector2 shootDirection = playerCenter - wispCenter;
                _fireBalls.Add(new FireBall(wispCenter, shootDirection));
                _attackTimer = 0f;
            }
        }
        else
        {
            _attackTimer = 0f;

            Vector2 homeDirection = _spawnPosition - _position;
            if (homeDirection.LengthSquared() > 4f)
            {
                homeDirection.Normalize();
                Vector2 nextPosition = _position + homeDirection * (MoveSpeed * 0.35f * dt);
                Rectangle nextHitbox = GetHitbox(nextPosition);

                bool blocked = false;
                foreach (var collision in collisions)
                {
                    if (collision.IsOneWay || collision.IsSpikes)
                        continue;

                    if (nextHitbox.Intersects(collision.Bounds))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                    _position = nextPosition;
            }
        }

        Rectangle playerRect = new Rectangle((int)playerPos.X + 24, (int)playerPos.Y + 14, 16, 18);
        if (_contactDamageTimer <= 0f && Hitbox.Intersects(playerRect))
        {
            stats?.TakeDamage(ContactDamage);
            damageTexts.Add(new DamageText(playerPos + new Vector2(24, -10), ContactDamage));
            _contactDamageTimer = ContactDamageCooldown;
        }

        for (int i = _fireBalls.Count - 1; i >= 0; i--)
        {
            _fireBalls[i].Update(gameTime, collisions, playerPos, stats, damageTexts);
            if (_fireBalls[i].IsDead)
                _fireBalls.RemoveAt(i);
        }
    }

    private Rectangle GetHitbox(Vector2 position)
    {
        return new Rectangle((int)position.X + 18, (int)position.Y + 18, 36, 36);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D whitePixel)
    {
        if (_isDead || _texture == null)
            return;

        int sourceX = _currentFrame * FrameWidth;
        Rectangle sourceRect = new Rectangle(sourceX, 0, FrameWidth, FrameHeight);
        Vector2 scale = new Vector2(DrawScale, DrawScale);
        spriteBatch.Draw(_texture, _position, sourceRect, Color.White, 0f, Vector2.Zero, scale, _facing, 0f);

        foreach (var fireBall in _fireBalls)
            fireBall.Draw(spriteBatch, whitePixel);
    }
}
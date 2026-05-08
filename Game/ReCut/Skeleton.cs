using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ReCut;

public class Skeleton
{
    public Vector2 Position;
    public Rectangle Hitbox;
    
    private Texture2D _sprite;
    private Texture2D _slash;
    private int _frameWidth = 150; 
    private int _frameHeight = 150;
    private int _currentFrame = 2;
    private float _animTimer;
    
    private int _direction = 1;
    private float _speed = 60f;
    private bool _isAttacking = false;
    private float _attackCooldown = 0f;

    public Skeleton(Texture2D sprite, Texture2D slash, Vector2 pos)
    {
        _sprite = sprite;
        _slash = slash;
        Position = pos;
    }

    public void Update(GameTime gameTime, Vector2 playerPos, List<CollisionObject> collisions, PlayerStats stats)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_attackCooldown > 0) _attackCooldown -= dt;

        float distToPlayer = Vector2.Distance(new Vector2(Position.X + 75, Position.Y + 75), playerPos);

        if (!_isAttacking)
        {
            _currentFrame = 2;
            float nextX = Position.X + _speed * _direction * dt;
            Rectangle wallCheck = new Rectangle((int)nextX + (_direction == 1 ? 90 : 50), (int)Position.Y + 70, 10, 40);
            bool hitObstacle = false;

            foreach (var col in collisions)
            {
                if (!col.IsOneWay && wallCheck.Intersects(col.Bounds))
                {
                    hitObstacle = true;
                    break;
                }
            }

            Rectangle floorCheck = new Rectangle((int)nextX + (_direction == 1 ? 100 : 40), (int)Position.Y + 152, 5, 5);
            bool floorAhead = false;

            foreach (var col in collisions)
            {
                if (floorCheck.Intersects(col.Bounds))
                {
                    floorAhead = true;
                    break;
                }
            }

            if (hitObstacle || !floorAhead)
            {
                _direction *= -1;
            }

            else
            {
                Position.X = nextX;
            }

            if (distToPlayer < 80f && _attackCooldown <= 0)
            {
                _isAttacking = true;
                _currentFrame = 0;
                _animTimer = 0;
            }
        }
        else
        {
            _animTimer += dt;
            float frameSpeed = (_currentFrame < 3) ? 0.15f : 0.07f;

            if (_animTimer > frameSpeed)
            {
                _currentFrame++;
                _animTimer = 0;

                if (_currentFrame == 4)
                {
                    if (distToPlayer < 90f)
                    stats.TakeDamage(15);
                }

                if (_currentFrame >= 6)
                {
                    _isAttacking = false;
                    _currentFrame = 2;
                    _attackCooldown = 1.5f;
                }
            }
        }

        Hitbox = new Rectangle((int)Position.X + 60, (int)Position.Y + 40, 30, 100);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        SpriteEffects flip = _direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Rectangle sourceRect = new Rectangle(_currentFrame * _frameWidth, 0, _frameWidth, _frameHeight);
        
        spriteBatch.Draw(_sprite, Position, sourceRect, Color.White, 0f, Vector2.Zero, 1f, flip, 0f);

        if (_isAttacking && _currentFrame >= 3 && _slash != null)
        {
            spriteBatch.Draw(_slash, Position, sourceRect, Color.White, 0f, Vector2.Zero, 1f, flip, 0f);
        }
    }
}
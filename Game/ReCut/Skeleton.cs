using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ReCut;

public class Skeleton
{
    public enum AnimationState
    {
        Idle,
        Walk,
        Attack1,
        Attack2,
        Hurt,
        Die
    }

    public Vector2 Position;
    public Rectangle Hitbox;

    private Texture2D _spriteIdle;
    private Texture2D _spriteWalk;
    private Texture2D _spriteAttack1;
    private Texture2D _spriteAttack2;
    private Texture2D _spriteHurt;
    private Texture2D _spriteDie;

    private int _frameWidth = 96;
    private int _frameHeight = 64;
    
    private AnimationState _currentState = AnimationState.Idle;
    private int _currentFrame = 0;
    private float _animTimer;
    private Random _random = new Random();

    private int _direction = 1;
    private float _speed = 110f;
    private float _verticalVelocity = 0f;
    private bool _onGround = false;
    private bool _isAttacking = false;
    private bool _attackDamageApplied = false;
    private float _attackPrepareTimer = 0f;
    private bool _isPreparingAttack = false;
    private bool _isHurt = false;
    private float _hurtTimer = 0f;
    private float _attackCooldown = 0f;
    private float _turnCooldown = 0f;
    private float _postAttackWaitTime = 0f;
    private int _hp = 4;
    private bool _isDead = false;

    private const float Gravity = 1600f;
    private const float MaxFallSpeed = 1800f;
        private const int BodyLeftOffset = 8;
        private const int BodyTopOffset = 6;
        private const int BodyWidth = 16;
        private const int BodyHeight = 36;
    private const int BodyBottomOffset = BodyTopOffset + BodyHeight;
    private const int VisualYOffset = -21;
    private const int DamageHitboxLeftOffset = 12;
    private const int DamageHitboxTopOffset = 12;
    private const int DamageHitboxWidth = 40;
    private const int DamageHitboxHeight = 78;
    private const float AttackRange = 90f;
    private const float BackAttackRange = 85f;
    private const float AttackPrepareDelay = 0.5f;
    private const float SightHeight = 60f;
    private const float AttackHeight = 50f;
    private const float SightRange = 130f;

    public Skeleton(Texture2D idle, Texture2D walk, Texture2D attack1, Texture2D attack2, 
                    Texture2D hurt, Texture2D die, Vector2 pos)
    {
        _spriteIdle = idle;
        _spriteWalk = walk;
        _spriteAttack1 = attack1;
        _spriteAttack2 = attack2;
        _spriteHurt = hurt;
        _spriteDie = die;
        Position = pos;
    }

    private Rectangle GetBodyRectangle(Vector2 position)
    {
        return new Rectangle((int)position.X + BodyLeftOffset, (int)position.Y + BodyTopOffset, BodyWidth, BodyHeight);
    }

    private Vector2 GetSkeletonCenter()
    {
        return new Vector2(Position.X + _frameWidth * 0.5f, Position.Y + _frameHeight * 0.5f);
    }

    private bool HasGroundAhead(float nextX, List<CollisionObject> collisions)
    {
           int probeX = (int)nextX + (_direction == 1 ? 20 : 12);
        Rectangle floorProbe = new Rectangle(probeX, (int)Position.Y + BodyBottomOffset + 1, 6, 8);

        foreach (var col in collisions)
        {
            if (floorProbe.Intersects(col.Bounds))
                return true;
        }

        return false;
    }

    private bool IsPlayerInSightHeight(Vector2 playerPos)
    {
            Vector2 skeletonCenter = GetSkeletonCenter();
        return Math.Abs(playerPos.Y - skeletonCenter.Y) <= SightHeight;
    }

    private bool IsPlayerInAttackHeight(Vector2 playerPos)
    {
            Vector2 skeletonCenter = GetSkeletonCenter();
        return Math.Abs(playerPos.Y - skeletonCenter.Y) <= AttackHeight;
    }

    private bool IsPlayerInFront(Vector2 playerPos)
    {
            Vector2 skeletonCenter = GetSkeletonCenter();
        float dx = playerPos.X - skeletonCenter.X;
        bool sameSide = (_direction == 1 && dx >= 0f) || (_direction == -1 && dx <= 0f);
        return sameSide && Math.Abs(dx) <= SightRange;
    }

    private void FacePlayer(Vector2 playerPos)
    {
            Vector2 skeletonCenter = GetSkeletonCenter();
        _direction = playerPos.X >= skeletonCenter.X ? 1 : -1;
        _turnCooldown = 0.35f;
    }

    public void Update(GameTime gameTime, Vector2 playerPos, List<CollisionObject> collisions, PlayerStats stats)
    {
        if (_isDead)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool movedHorizontally = false;
        
        if (_attackCooldown > 0)
            _attackCooldown -= dt;
        if (_turnCooldown > 0)
            _turnCooldown -= dt;
        if (_postAttackWaitTime > 0)
            _postAttackWaitTime -= dt;
        
        if (_isHurt)
        {
            _hurtTimer -= dt;
            if (_hurtTimer <= 0)
            {
                _isHurt = false;
                _currentState = AnimationState.Idle;
                _currentFrame = 0;
                _animTimer = 0;
            }
        }

        Vector2 playerCenter = playerPos + new Vector2(32f, 16f);
            float distToPlayer = Vector2.Distance(GetSkeletonCenter(), playerCenter);

        _onGround = false;
        _verticalVelocity += Gravity * dt;
        if (_verticalVelocity > MaxFallSpeed)
            _verticalVelocity = MaxFallSpeed;

        float nextY = Position.Y + _verticalVelocity * dt;
        Rectangle currentBody = GetBodyRectangle(Position);
        Rectangle nextBody = GetBodyRectangle(new Vector2(Position.X, nextY));

        foreach (var col in collisions)
        {
            Rectangle rect = col.Bounds;
            bool overlapX = nextBody.Right > rect.Left && nextBody.Left < rect.Right;

            if (!overlapX)
                continue;

            if (!col.IsOneWay && _verticalVelocity < 0f && currentBody.Top >= rect.Bottom && nextBody.Top < rect.Bottom)
            {
                Position.Y = rect.Bottom - BodyTopOffset;
                _verticalVelocity = 0f;
                nextY = Position.Y;
                nextBody = GetBodyRectangle(new Vector2(Position.X, nextY));
                continue;
            }

            if (_verticalVelocity >= 0f && currentBody.Bottom <= rect.Top && nextBody.Bottom >= rect.Top)
            {
                Position.Y = rect.Top - BodyBottomOffset;
                _verticalVelocity = 0f;
                _onGround = true;
                nextY = Position.Y;
                nextBody = GetBodyRectangle(new Vector2(Position.X, nextY));
            }
        }

        if (!_onGround)
            Position.Y = nextY;

        if (!_isHurt && !_isAttacking)
        {
            if (_onGround)
            {
                bool playerInSightHeight = IsPlayerInSightHeight(playerCenter);
                bool playerInAttackHeight = IsPlayerInAttackHeight(playerCenter);
                bool playerInFront = IsPlayerInFront(playerCenter);
                bool attackWanted = false;

                if (playerInAttackHeight && distToPlayer <= BackAttackRange && !playerInFront && _turnCooldown <= 0f && _attackCooldown <= 0f)
                {
                    attackWanted = true;
                }

                if (_postAttackWaitTime > 0f)
                {
                    _currentState = AnimationState.Idle;
                    
                    if (playerInAttackHeight && playerInFront && distToPlayer <= AttackRange && _attackCooldown <= 0f)
                    {
                        attackWanted = true;
                        _postAttackWaitTime = 0f;
                    }
                }
                else
                {
                    attackWanted = attackWanted || (playerInAttackHeight && playerInFront && distToPlayer <= AttackRange && _attackCooldown <= 0f);

                    if (!attackWanted && !_isPreparingAttack)
                    {
                        float nextX = Position.X + _speed * _direction * dt;
                        Rectangle wallCheck = new Rectangle((int)nextX + (_direction == 1 ? 20 : 12), (int)Position.Y + 12, 8, 16);
                        Rectangle nextBodyX = GetBodyRectangle(new Vector2(nextX, Position.Y));
                        bool hitObstacle = false;

                        foreach (var col in collisions)
                        {
                            if (col.IsOneWay)
                                continue;

                            if (wallCheck.Intersects(col.Bounds) || nextBodyX.Intersects(col.Bounds))
                            {
                                hitObstacle = true;
                                break;
                            }
                        }

                        bool floorAhead = HasGroundAhead(nextX, collisions);

                        if (hitObstacle || !floorAhead)
                        {
                            _direction *= -1;
                            _turnCooldown = 0.2f;
                            _currentState = AnimationState.Idle;
                            _currentFrame = 0;
                        }
                        else
                        {
                            Position.X = nextX;
                            movedHorizontally = true;
                            _currentState = AnimationState.Walk;
                        }
                    }
                }

                if (attackWanted && !_isAttacking)
                {
                    _isPreparingAttack = true;
                    _currentState = AnimationState.Idle;
                    _currentFrame = 0;
                }
            }
        }

        if (_isPreparingAttack && !_isAttacking)
        {
            _attackPrepareTimer += dt;
            _currentState = AnimationState.Idle;
            _currentFrame = 0;

            if (_attackPrepareTimer >= AttackPrepareDelay)
            {
                _attackPrepareTimer = 0f;
                _isPreparingAttack = false;
                _isAttacking = true;
                _attackDamageApplied = false;
                SelectRandomAttack();
                _currentFrame = 0;
                _animTimer = 0;
            }
        }
        else if (!_isAttacking)
        {
            _attackPrepareTimer = 0f;
            _isPreparingAttack = false;
        }

        if (!_isAttacking && !_isHurt && !_isDead && !_isPreparingAttack)
        {
            _currentState = movedHorizontally ? AnimationState.Walk : AnimationState.Idle;
        }

        _animTimer += dt;
        float frameSpeed = 0.08f;
        
        if (_isAttacking)
            frameSpeed = 0.08f;
        else if (_currentState == AnimationState.Walk)
            frameSpeed = 0.07f;
        else if (_currentState == AnimationState.Idle)
            frameSpeed = 0.1f;
        else if (_currentState == AnimationState.Hurt)
            frameSpeed = 0.06f;
        else if (_currentState == AnimationState.Die)
            frameSpeed = 0.08f;

        if (_animTimer > frameSpeed)
        {
            int maxFrames = GetCurrentFrameCount();
            _currentFrame++;
            _animTimer = 0;

            if (_isAttacking && !_attackDamageApplied)
            {
                    float damageCheckDist = Vector2.Distance(GetSkeletonCenter(), playerCenter);
                if (damageCheckDist < AttackRange && IsPlayerInFront(playerCenter) && IsPlayerInAttackHeight(playerCenter))
                {
                    stats?.TakeDamage(15);
                }

                _attackDamageApplied = true;
            }

            if (_isAttacking && _currentFrame >= maxFrames)
            {
                _isAttacking = false;
                _attackDamageApplied = false;
                _currentState = AnimationState.Idle;
                _currentFrame = 0;
                _attackCooldown = 1.2f;
                _postAttackWaitTime = 1.2f;

                bool playerInSightHeight = IsPlayerInSightHeight(playerCenter);
                if (playerInSightHeight && !IsPlayerInFront(playerCenter) && _turnCooldown <= 0f)
                {
                    FacePlayer(playerCenter);
                }
            }
            
            else if (!_isAttacking && _currentFrame >= maxFrames)
            {
                _currentFrame = 0;
            }
        }

        Hitbox = new Rectangle((int)Position.X + DamageHitboxLeftOffset, (int)Position.Y + DamageHitboxTopOffset, DamageHitboxWidth, DamageHitboxHeight);
    }

    private void SelectRandomAttack()
    {
        _currentState = _random.Next(2) == 0 ? AnimationState.Attack1 : AnimationState.Attack2;
    }

    private void TryStartAttackWithDelay(float dt)
    {
        if (_isAttacking)
            return;

        _attackPrepareTimer += dt;
        _currentState = AnimationState.Idle;
        _currentFrame = 0;

        if (_attackPrepareTimer < AttackPrepareDelay)
            return;

        _attackPrepareTimer = 0f;
        _isAttacking = true;
        _attackDamageApplied = false;
        SelectRandomAttack();
        _currentFrame = 0;
        _animTimer = 0;
    }

    public bool TakeDamage(int amount)
    {
        if (_isDead || _isHurt)
            return false;

        _hp -= amount;
        
        if (_hp <= 0)
        {
            _isDead = true;
            _currentState = AnimationState.Die;
            _currentFrame = 0;
            _animTimer = 0;
            return true;
        }

        _isHurt = true;
        _hurtTimer = 0.5f;
        _currentState = AnimationState.Hurt;
        _currentFrame = 0;
        _animTimer = 0;
        _isAttacking = false;
        _attackDamageApplied = false;
        _attackPrepareTimer = 0f;

        return false;
    }

    public bool IsDead() => _isDead;

    private Texture2D GetCurrentSprite()
    {
        return _currentState switch
        {
            AnimationState.Idle => _spriteIdle,
            AnimationState.Walk => _spriteWalk,
            AnimationState.Attack1 => _spriteAttack1,
            AnimationState.Attack2 => _spriteAttack2,
            AnimationState.Hurt => _spriteHurt,
            AnimationState.Die => _spriteDie,
            _ => _spriteIdle
        };
    }

    private int GetFrameCountForState(AnimationState state)
    {
        return state switch
        {
            AnimationState.Idle => 8,
            AnimationState.Walk => 10,
            AnimationState.Attack1 => 10,
            AnimationState.Attack2 => 9,
            AnimationState.Hurt => 5,
            AnimationState.Die => 13,
            _ => 1
        };
    }

    private int GetCurrentFrameCount() => GetFrameCountForState(_currentState);

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_isDead && _currentFrame >= GetFrameCountForState(AnimationState.Die))
            return;

        SpriteEffects flip = _direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Texture2D sprite = GetCurrentSprite();
        
        if (sprite == null)
            return;

        int frameCount = GetCurrentFrameCount();
        if (_currentFrame >= frameCount)
            _currentFrame = frameCount - 1;

        int sourceX = _currentFrame * _frameWidth;
        int sourceWidth = Math.Min(_frameWidth, Math.Max(1, sprite.Width - sourceX));
        int sourceHeight = Math.Min(_frameHeight, sprite.Height);
        Rectangle sourceRect = new Rectangle(sourceX, 0, sourceWidth, sourceHeight);
        Vector2 drawPosition = new Vector2(Position.X, Position.Y + VisualYOffset);

        spriteBatch.Draw(sprite, drawPosition, sourceRect, Color.White, 0f, Vector2.Zero, 1f, flip, 0f);
    }
}

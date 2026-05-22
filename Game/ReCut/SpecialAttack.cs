using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace ReCut;

public class SpecialAttack
{
    private bool _isCharging = false;
    private bool _isActive = false;
    private float _chargeTimer = 0f;
    private float _cooldownTimer = 0f;
    private float _attackAnimTimer = 0f;
    private float _attackDistanceLeft = 0f;
    private Vector2 _attackDirection = Vector2.Zero;
    private readonly HashSet<Skeleton> _hitSkeletons = new HashSet<Skeleton>();
    private int _currentFrame = 0;
    private int _currentRow = 8;
    private int _attackAnimState = 0;

    private const float MaxCharge = 2.0f;
    private const float MinCharge = 0.05f;
    private const float CooldownTime = 25f;
    private const float InvulnerabilityTime = 1f;
    private const int DamageMultiplier = 2;
    private const float AttackSpeed = 900f;
    private const float AttackRange = 300f;
    private const int AttackHitboxLeft = 24;
    private const int AttackHitboxTop = 14;
    private const int AttackHitboxWidth = 16;
    private const int AttackHitboxHeight = 18;

    public bool IsCharging => _isCharging;
    public bool IsActive => _isActive;
    public float CooldownRemaining => _cooldownTimer;
    public int CurrentFrame => _currentFrame;
    public int CurrentRow => _currentRow;

    public void StartCharging()
    {
        if (_cooldownTimer > 0f)
            return;

        _isCharging = true;
        _chargeTimer = 0f;
    }

    private void BeginAttack(Vector2 direction)
    {
        if (direction == Vector2.Zero)
            direction = new Vector2(1f, 0f);

        direction.Normalize();

        _isCharging = false;
        _isActive = true;
        _chargeTimer = 0f;
        _cooldownTimer = CooldownTime;
        _attackDirection = direction;
        _attackDistanceLeft = AttackRange;
        _attackAnimState = 0;
        _currentRow = 8;
        _currentFrame = 0;
        _attackAnimTimer = 0f;
        _hitSkeletons.Clear();
    }

    public void Update(float dt)
    {
        if (_isCharging)
        {
            _chargeTimer += dt;
            if (_chargeTimer > MaxCharge)
                _chargeTimer = MaxCharge;
        }

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= dt;
            if (_cooldownTimer < 0f)
                _cooldownTimer = 0f;
        }
    }

    private static Rectangle MakeAttackHitbox(Vector2 position)
    {
        return new Rectangle((int)position.X + AttackHitboxLeft, (int)position.Y + AttackHitboxTop, AttackHitboxWidth, AttackHitboxHeight);
    }

    private bool IsEnemyHitValid(Rectangle attackHitbox, Rectangle enemyHitbox)
    {
        if (!attackHitbox.Intersects(enemyHitbox))
            return false;

        int attackCenterY = attackHitbox.Top + (attackHitbox.Height / 2);
        int upperBound = enemyHitbox.Top + 18;
        int lowerBound = enemyHitbox.Bottom - 18;
        return attackCenterY >= upperBound && attackCenterY <= lowerBound;
    }

    private void ResolvePiercingHits(Rectangle attackHitbox, List<Skeleton> skeletons, Target dummy, string currentLevelName, List<DamageText> damageTexts)
    {
        if (string.Equals(currentLevelName, "level1", StringComparison.OrdinalIgnoreCase) && dummy != null && attackHitbox.Intersects(dummy.Hitbox))
        {
            dummy.IsHit = true;
            damageTexts.Add(new DamageText(dummy.Position + new Vector2(16, -10), 5));
        }

        for (int i = skeletons.Count - 1; i >= 0; i--)
        {
            Skeleton skeleton = skeletons[i];
            if (_hitSkeletons.Contains(skeleton))
                continue;

            if (!IsEnemyHitValid(attackHitbox, skeleton.Hitbox))
                continue;

            _hitSkeletons.Add(skeleton);
            bool died = skeleton.TakeDamage(DamageMultiplier);
            damageTexts.Add(new DamageText(skeleton.Position + new Vector2(16, -10), died ? 12 : 8));
            if (died)
                skeletons.RemoveAt(i);
        }
    }

    private void ResolvePiercingHits(Rectangle attackHitbox, List<FireWisp> fireWisps, List<DamageText> damageTexts)
    {
        for (int i = fireWisps.Count - 1; i >= 0; i--)
        {
            FireWisp fireWisp = fireWisps[i];
            if (!attackHitbox.Intersects(fireWisp.Hitbox))
                continue;

            bool died = fireWisp.TakeDamage(2);
            damageTexts.Add(new DamageText(new Vector2(fireWisp.Hitbox.X, fireWisp.Hitbox.Y - 10), died ? 12 : 8));
            if (died)
                fireWisps.RemoveAt(i);
        }
    }

    // Returns true if the attack was fired.
    public bool Release(Vector2 origin, Vector2 direction, List<Skeleton> skeletons, Target dummy, string currentLevelName, List<DamageText> damageTexts, PlayerStats stats)
    {
        if (!_isCharging || _cooldownTimer > 0f)
        {
            _isCharging = false;
            _chargeTimer = 0f;
            return false;
        }

        BeginAttack(direction);
        stats?.SetInvulnerable(InvulnerabilityTime);
        return true;
    }

    public void UpdateFrame(float dt, MouseState currentMouse, MouseState prevMouse, ref Vector2 playerPosition, Vector2 mouseWorldPos, List<CollisionObject> collisions, List<Skeleton> skeletons, List<FireWisp> fireWisps, Target dummy, string currentLevelName, List<DamageText> damageTexts, PlayerStats stats, ref SpriteEffects facing)
    {
        Update(dt);

        if (currentMouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released)
        {
            StartCharging();
        }

        if (currentMouse.RightButton == ButtonState.Released && prevMouse.RightButton == ButtonState.Pressed)
        {
            Vector2 direction = mouseWorldPos - (playerPosition + new Vector2(32f, 16f));
            if (direction != Vector2.Zero)
                direction.Normalize();

            if (Release(playerPosition + new Vector2(32f, 16f), direction, skeletons, dummy, currentLevelName, damageTexts, stats))
            {
                facing = direction.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            }
        }

        if (!_isActive)
            return;

        if (_attackAnimState == 0)
        {
            _currentRow = 8;
            _currentFrame = 0;

            float moveDistance = AttackSpeed * dt;
            if (moveDistance > _attackDistanceLeft)
                moveDistance = _attackDistanceLeft;

            Vector2 nextPos = playerPosition + _attackDirection * moveDistance;
            _attackDistanceLeft -= moveDistance;

            Rectangle attackHitbox = MakeAttackHitbox(nextPos);
            bool hitWall = false;

            foreach (var collision in collisions)
            {
                if (collision.IsOneWay || collision.IsSpikes)
                    continue;

                if (attackHitbox.Intersects(collision.Bounds))
                {
                    hitWall = true;
                    break;
                }
            }

            if (!hitWall)
            {
                playerPosition = nextPos;
                ResolvePiercingHits(attackHitbox, skeletons, dummy, currentLevelName, damageTexts);
                ResolvePiercingHits(attackHitbox, fireWisps, damageTexts);
            }

            if (hitWall || _attackDistanceLeft <= 0f)
            {
                _attackAnimState = 1;
                _currentFrame = 1;
                _attackAnimTimer = 0f;
            }
        }
        else
        {
            _currentRow = 8;
            _attackAnimTimer += dt;
            if (_attackAnimTimer > 0.1f)
            {
                _currentFrame++;
                _attackAnimTimer = 0f;

                if (_currentFrame > 2)
                {
                    _isActive = false;
                    _attackDistanceLeft = 0f;
                    _attackDirection = Vector2.Zero;
                    _attackAnimState = 0;
                    _currentFrame = 0;
                    _hitSkeletons.Clear();
                }
            }
        }
    }

    public float GetInvulnerabilityDuration() => InvulnerabilityTime;

}

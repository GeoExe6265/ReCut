using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.Collections.Generic;

namespace ReCut;

public struct CollisionObject 
    {
        public Rectangle Bounds;
        public bool IsOneWay;
        public bool IsSpikes;
    }
public class Game1 : Core
{
    private KeyboardState _previousState;
    private MouseState _previousMouseState;
    private Texture2D _character;
    private Texture2D _whitePixel;
    private Target _dummy;
    private SpecialAttack _specialAttack;
    private SpecialAttackHud _specialAttackHud;
    private GameOverScreen _gameOverScreen;
    private List<Skeleton> _skeletons = new List<Skeleton>();
    private List<FireWisp> _fireWisps = new List<FireWisp>();
    private Texture2D _skeletonIdle, _skeletonWalk, _skeletonAttack1, _skeletonAttack2, _skeletonHurt, _skeletonDie;
    private Texture2D _fireWispTexture;
    private TiledMap _map;
    private SpriteFont _damageFont;
    private TiledMapRenderer _mapRenderer;
    private PlayerStats _stats;
    private Dictionary<string, TiledMap> _levelCache = new Dictionary<string, TiledMap>(StringComparer.OrdinalIgnoreCase);
    private List<Texture2D> _dummyIdleFrames = new List<Texture2D>();
    private List<Texture2D> _dummyHitFrames = new List<Texture2D>();
    private List<CollisionObject> _collisionObjects = new List<CollisionObject>();
    private List<LevelExit> _levelExits = new List<LevelExit>();
    private List<DamageText> _damageTexts = new List<DamageText>();
    private Vector2 _pos;
    private Vector2 _cameraPos;
    private Vector2 _velocity;
    private Vector2 _attackDir;
    private Vector2 _lastSafePosition;
    private SpriteEffects _facing = SpriteEffects.None;
    private bool _onGround;
    private bool _isAttacking = false;
    private bool _attackLock = false;
    private bool _pendingAttackBounce = false;
    private Vector2 _pendingAttackBounceVelocity = Vector2.Zero;
    private bool _attackHitThisSwing = false;
    private bool _isOnCooldown;
    private bool _isOnWall;
    private float zoom = 2.5f;
    private float _attackAnimTimer = 0f;
    private float _attackCooldown = 0f;
    private float _postHitAttackTimer = 0f;
    private float _wallJumpTimer = 0f;
    private float _hitStopTimer = 0f;
    private float _darkScreen = 0f;
    private float _spikeImmunity = 0f;
    private float _animTimer;
    private float _attackDistanceLeft;
    private int _frameWidth = 64;
    private int _frameHeight = 32;
    private int _framesInRow = 7;
    private int _previousRow = -1;
    private int _attackAnimState = 0;
    private int _wallDirection = 0;
    private int _currentFrame, _currentRow;
    private string _currentLevelName;
    private string _currentSpawnName = "Spawn";
    private bool _isRespawning = false;
    private float _respawnTimer = 0f;
    private const float Gravity = 1600f;
    private const float JumpForce = -600f;
    private const float Speed = 350f;
    private const float AttackCooldownTime = 0.75f;
    private const float AttackDelay = 0.5f;
    private const float AttackBounceSpeed = 320f;
    private const float AttackBounceHeight = 350f;
    private const float SpikeDamageImmunityTime = 0.45f;
    private static readonly string[] PreloadLevelNames = new[] { "level1", "level2", "levelsecret" };
    public Game1() : base("ReCut", 1280, 720, false)
    {
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    

    public struct LevelExit
    {
        public Rectangle Bounds;
        public string TargetLevel;
        public string TargetSpawn;
    }

    public void ToggleFullScreen()
    {
        Graphics.IsFullScreen = true;
        Graphics.HardwareModeSwitch = false;
        Graphics.ApplyChanges();
    }

    public void ToggleWindowedMode()
    {
        Graphics.IsFullScreen = false;
        Graphics.ApplyChanges();
    }

    private TiledMap GetCachedLevel(string levelName)
    {
        if (_levelCache.TryGetValue(levelName, out var map))
            return map;

        map = Content.Load<TiledMap>("maps/" + levelName);
        _levelCache[levelName] = map;
        return map;
    }

    private void PreloadLevels()
    {
        foreach (var levelName in PreloadLevelNames)
        {
            try
            {
                GetCachedLevel(levelName);
            }
            catch
            {
                // Ignore missing optional levels and keep running.
            }
        }
    }

    private string InferSpawnNameFromExit(string exitName)
    {
        if (string.Equals(exitName, "ExitFara", StringComparison.OrdinalIgnoreCase))
            return "SpawnSecretExit";

        if (string.Equals(exitName, "ExitUp", StringComparison.OrdinalIgnoreCase))
            return "SpawnDown";

        if (string.Equals(exitName, "ExitDown", StringComparison.OrdinalIgnoreCase))
            return "SpawnUp";

        if (!string.IsNullOrEmpty(exitName) && exitName.StartsWith("Exit", StringComparison.OrdinalIgnoreCase) && exitName.Length > 4)
            return "Spawn" + exitName.Substring(4);

        return "Spawn";
    }

    private Vector2 ResolveSpawnPosition(IEnumerable<TiledMapObject> objects, string spawnName)
    {
        string normalizedSpawnName = (spawnName ?? string.Empty).Trim();

        foreach (var obj in objects)
        {
            if (string.Equals((obj.Name ?? string.Empty).Trim(), normalizedSpawnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals((obj.Type ?? string.Empty).Trim(), normalizedSpawnName, StringComparison.OrdinalIgnoreCase))
            {
                return obj.Position;
            }
        }

        return Vector2.Zero;
    }

    private void ApplyAttackBounce()
    {
        if (!_pendingAttackBounce)
            return;

        _velocity = _pendingAttackBounceVelocity;
        _wallJumpTimer = 0.18f;
        _attackLock = false;
        _isAttacking = false;
        _attackAnimState = 0;
        _currentFrame = 0;
        _attackAnimTimer = 0f;
        _attackDistanceLeft = 0f;
        _attackDir = Vector2.Zero;
        _pendingAttackBounce = false;
        _pendingAttackBounceVelocity = Vector2.Zero;
    }

    private IEnumerable<TiledMapObject> GetEnemySpawnObjects()
    {
        if (_map == null)
            return Array.Empty<TiledMapObject>();

        var enemyLayer = _map.GetLayer<TiledMapObjectLayer>("Enemies");
        if (enemyLayer != null)
            return enemyLayer.Objects;

        return Array.Empty<TiledMapObject>();
    }

    private void SpawnSkeletonsFromMap(IEnumerable<TiledMapObject> objects)
    {
        _skeletons.Clear();

        foreach (var obj in objects)
        {
            bool isSkeletonSpawn = string.Equals((obj.Name ?? string.Empty).Trim(), "Skeleton", StringComparison.OrdinalIgnoreCase)
                && string.Equals((obj.Type ?? string.Empty).Trim(), "Enemy", StringComparison.OrdinalIgnoreCase);

            if (!isSkeletonSpawn)
                continue;

            _skeletons.Add(new Skeleton(
                _skeletonIdle,
                _skeletonWalk,
                _skeletonAttack1,
                _skeletonAttack2,
                _skeletonHurt,
                _skeletonDie,
                obj.Position));
        }
    }

    private void SpawnFireWispsFromMap(IEnumerable<TiledMapObject> objects)
    {
        _fireWisps.Clear();

        foreach (var obj in objects)
        {
            bool isFireWispSpawn = string.Equals((obj.Name ?? string.Empty).Trim(), "FireWisp", StringComparison.OrdinalIgnoreCase)
                && string.Equals((obj.Type ?? string.Empty).Trim(), "Enemy", StringComparison.OrdinalIgnoreCase);

            if (!isFireWispSpawn)
                continue;

            _fireWisps.Add(new FireWisp(_fireWispTexture, obj.Position));
        }
    }

    // Я ЭТУ ПРОГРУЗКУ ДЕЛАЛ 2 С ПОЛОВИНОЙ ЧАСА. МОЖНО МНЕ ОТДОХНУТЬ? ;-;
    private void LoadLevel(string levelName, string spawnName)
    {
        _currentSpawnName = string.IsNullOrWhiteSpace(spawnName) ? "Spawn" : spawnName;
        _currentLevelName = levelName;
        _collisionObjects.Clear();
        _levelExits.Clear();
        _map = GetCachedLevel(levelName);
        _mapRenderer = new TiledMapRenderer(GraphicsDevice, _map);
        var objects = _map.GetLayer<TiledMapObjectLayer>("Collision").Objects;

        _pos = ResolveSpawnPosition(objects, _currentSpawnName);

        foreach (var obj in objects)
        {
            if (obj.Type == "LevelExit")
            {
                string resolvedTargetSpawn = obj.Properties.ContainsKey("TargetSpawn")
                    ? obj.Properties["TargetSpawn"]
                    : InferSpawnNameFromExit(obj.Name);

                _levelExits.Add(new LevelExit
                {
                    Bounds = new Rectangle((int)obj.Position.X, (int)obj.Position.Y, (int)obj.Size.Width, (int)obj.Size.Height),
                    TargetLevel = obj.Properties.ContainsKey("TargetLevel") ? obj.Properties["TargetLevel"] : "",
                    TargetSpawn = resolvedTargetSpawn
                });
            }

            else if (obj.Name != "Spawn" && obj.Name != "SpawnUp" && obj.Name != "SpawnDown")
            {
                _collisionObjects.Add(new CollisionObject {
                    Bounds = new Rectangle((int)obj.Position.X, (int)obj.Position.Y, (int)obj.Size.Width, (int)obj.Size.Height),
                    IsOneWay = obj.Type == "OneWay",
                    IsSpikes = obj.Type == "Spikes" || obj.Name == "Spikes"
                });
            }
        }

        SpawnSkeletonsFromMap(GetEnemySpawnObjects());
        SpawnFireWispsFromMap(GetEnemySpawnObjects());

        _lastSafePosition = _pos;
        _velocity = Vector2.Zero;
        _onGround = false;
        _isOnWall = false;
        _wallDirection = 0;
        _wallJumpTimer = 0f;
    }

    // ТЕХ ЧАСТЬ НЕ ТРОГАТЬ СВОИМИ РУЧКАМИ!
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _character = Content.Load<Texture2D>("images/character");
        _damageFont = Content.Load<SpriteFont>("fonts/DamageFont");
        
        _skeletonIdle = Content.Load<Texture2D>("enemies/skeleton/skeleton_idle");
        _skeletonWalk = Content.Load<Texture2D>("enemies/skeleton/skeleton_walk");
        _skeletonAttack1 = Content.Load<Texture2D>("enemies/skeleton/skeleton_attack1");
        _skeletonAttack2 = Content.Load<Texture2D>("enemies/skeleton/skeleton_attack2");
        _skeletonHurt = Content.Load<Texture2D>("enemies/skeleton/skeleton_hurt");
        _skeletonDie = Content.Load<Texture2D>("enemies/skeleton/skeleton_die");
        _fireWispTexture = Content.Load<Texture2D>("enemies/firewisp/firewisp");

        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
        _stats = new PlayerStats();
        _specialAttack = new SpecialAttack();
        _specialAttackHud = new SpecialAttackHud(new Vector2(20, 60));
        _gameOverScreen = new GameOverScreen();

        for (int i = 1; i <= 4; i++) 
            _dummyIdleFrames.Add(Content.Load<Texture2D>("enemies/dummy/dummy_idle"));

        for (int i = 1; i <= 5; i++) 
            _dummyHitFrames.Add(Content.Load<Texture2D>($"enemies/dummy/dummy_hit{i}"));

        _dummy = new Target(_dummyIdleFrames, _dummyHitFrames, new Vector2(500, 497));

        PreloadLevels();

        LoadLevel("level1", "Spawn");
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentState = Keyboard.GetState();
        MouseState currentMouseState = Mouse.GetState();
        Vector2 mouseWorldPos = (currentMouseState.Position.ToVector2() / zoom) + _cameraPos;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Rectangle playerRect = Rectangle.Empty;

        if (_spikeImmunity > 0f)
        {
            _spikeImmunity -= dt;
            if (_spikeImmunity < 0f)
                _spikeImmunity = 0f;
        }

        if (_darkScreen > 0f)
        {
            _darkScreen -= dt;
            if (_darkScreen < 0f)
                _darkScreen = 0f;
        }

        if (_postHitAttackTimer > 0f)
        {
            _postHitAttackTimer -= dt;
            if (_postHitAttackTimer < 0f)
                _postHitAttackTimer = 0f;
        }

        if (_stats != null)
            _stats.Update(gameTime);
        bool _jumpPressed = (currentState.IsKeyDown(Keys.Space) && _previousState.IsKeyUp(Keys.Space)) || (currentState.IsKeyDown(Keys.W) && _previousState.IsKeyUp(Keys.W));

        if (_isRespawning)
        {
            _respawnTimer -= dt;
            _gameOverScreen.Update(gameTime);

            if (_respawnTimer <= 0f)
            {
                _isRespawning = false;
                _respawnTimer = 0f;
                _stats?.RestoreFullHealth();
                LoadLevel(_currentLevelName, _currentSpawnName);
                UpdateCamera();
            }

            _previousState = currentState;
            _previousMouseState = currentMouseState;
            base.Update(gameTime);
            return;
        }

        if (_hitStopTimer > 0)
        {
            _hitStopTimer -= dt;
            if (_hitStopTimer <= 0f)
            {
                _hitStopTimer = 0f;

                if (_pendingAttackBounce)
                {
                    ApplyAttackBounce();
                }
            }

            return;
        }

        if (currentState.IsKeyDown(Keys.Escape))
            Exit();

        if (Graphics.IsFullScreen != true)
        {
            if (currentState.IsKeyDown(Keys.F11) && _previousState.IsKeyUp(Keys.F11))
            {
                ToggleFullScreen();
            }
        }

        else
        {
            if (currentState.IsKeyDown(Keys.F11) && _previousState.IsKeyUp(Keys.F11))
            {
                ToggleWindowedMode();
            }
        }

        if (_attackCooldown > 0)
        {
            _attackCooldown -= dt;
        }
    
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released 
        && !_isAttacking && !_attackLock && _attackCooldown <= 0 && _postHitAttackTimer <= 0f && !_specialAttack.IsCharging && !_specialAttack.IsActive) 
        {
            _isAttacking = true;
            _attackCooldown = AttackCooldownTime;
            _attackAnimState = 0;
            _currentRow = 8;
            _currentFrame = 0;
            _attackHitThisSwing = false;
            _attackDir = mouseWorldPos - (_pos + new Vector2(32, 16));

            if (_attackDir != Vector2.Zero)
            {
                _attackDir.Normalize();
            }
            _attackDistanceLeft = 150f;

            if (_attackDir.X < 0)
                _facing = SpriteEffects.FlipHorizontally;

            else if (_attackDir.X > 0)
                _facing = SpriteEffects.None;
        }

        _specialAttack.UpdateFrame(
            dt,
            currentMouseState,
            _previousMouseState,
            ref _pos,
            mouseWorldPos,
            _collisionObjects,
            _skeletons,
            _fireWisps,
            _dummy,
            _currentLevelName,
            _damageTexts,
            _stats,
            ref _facing);

        if (_isAttacking)
        {
            if (_attackAnimState == 0)
            {
                _currentFrame = 0;
                float attackDistance = 900f * dt;
                if (attackDistance > _attackDistanceLeft)
                {
                    attackDistance = _attackDistanceLeft;
                }

                Vector2 nextPos = _pos + _attackDir * attackDistance;
                _attackDistanceLeft -= attackDistance;

                Rectangle attackHitbox = new Rectangle((int)nextPos.X + 24, (int)nextPos.Y + 14, 16, 18);
                bool hitWall = false;

                foreach (var collisionAttack in _collisionObjects)
                {
                    if (collisionAttack.IsOneWay || collisionAttack.IsSpikes)
                        continue;

                    if (attackHitbox.Intersects(collisionAttack.Bounds))
                    {
                        hitWall = true;
                        break;
                    }
                }

                if (string.Equals(_currentLevelName, "level1", StringComparison.OrdinalIgnoreCase) && attackHitbox.Intersects(_dummy.Hitbox))
                {
                    _dummy.IsHit = true; 
                    hitWall = true;
                    _attackCooldown = 0;
                    _postHitAttackTimer = AttackDelay;
                    _hitStopTimer = 0.2f;
                    _pendingAttackBounce = true;
                    _attackHitThisSwing = true;
                    _attackLock = false;
                    _pendingAttackBounceVelocity = new Vector2(
                        -_attackDir.X * AttackBounceSpeed,
                        Math.Min(-_attackDir.Y * AttackBounceSpeed, -AttackBounceHeight));
                    _damageTexts.Add(new DamageText(_dummy.Position + new Vector2(16, -10), 5));
                }

                for (int i = _skeletons.Count - 1; i >= 0; i--)
                {
                    Skeleton skeleton = _skeletons[i];
                    if (!attackHitbox.Intersects(skeleton.Hitbox))
                        continue;

                    Rectangle skeletonHitbox = skeleton.Hitbox;
                    int attackCenterY = attackHitbox.Top + (attackHitbox.Height / 2);
                    int upperBound = skeletonHitbox.Top + 18;
                    int lowerBound = skeletonHitbox.Bottom - 18;

                    if (attackCenterY < upperBound || attackCenterY > lowerBound)
                        continue;

                    hitWall = true;
                    _attackCooldown = 0;
                    _postHitAttackTimer = AttackDelay;
                    _hitStopTimer = 0.2f;
                    _pendingAttackBounce = true;
                    _attackHitThisSwing = true;
                    _attackLock = false;
                    _pendingAttackBounceVelocity = new Vector2(
                        -_attackDir.X * AttackBounceSpeed,
                        Math.Min(-_attackDir.Y * AttackBounceSpeed, -AttackBounceHeight));
                    bool died = skeleton.TakeDamage(1);
                    _damageTexts.Add(new DamageText(skeleton.Position + new Vector2(16, -10), died ? 12 : 8));
                    if (died)
                        _skeletons.RemoveAt(i);

                    break;
                }

                for (int i = _fireWisps.Count - 1; i >= 0; i--)
                {
                    FireWisp fireWisp = _fireWisps[i];
                    if (!attackHitbox.Intersects(fireWisp.Hitbox))
                        continue;

                    hitWall = true;
                    _attackCooldown = 0;
                    _postHitAttackTimer = AttackDelay;
                    _hitStopTimer = 0.2f;
                    _pendingAttackBounce = true;
                    _attackHitThisSwing = true;
                    _attackLock = false;
                    _pendingAttackBounceVelocity = new Vector2(
                        -_attackDir.X * AttackBounceSpeed,
                        Math.Min(-_attackDir.Y * AttackBounceSpeed, -AttackBounceHeight));
                    bool died = fireWisp.TakeDamage(1);
                    _damageTexts.Add(new DamageText(new Vector2(fireWisp.Hitbox.X, fireWisp.Hitbox.Y - 10), died ? 12 : 8));
                    if (died)
                        _fireWisps.RemoveAt(i);

                    break;
                }

                if (hitWall || _attackDistanceLeft <= 0)
                {
                    _attackAnimState = 1;
                    _currentFrame = 1;
                    _attackAnimTimer = 0;
                }
                else _pos = nextPos;
            }
            
            else
            {
                _attackAnimTimer += dt;
                if (_attackAnimTimer > 0.1f)
                {
                    _currentFrame++;
                    _attackAnimTimer = 0;
                    if (_currentFrame > 2)
                    {
                        _isAttacking = false;
                        _currentFrame = 0;
                        _animTimer = 0;
                        _velocity = Vector2.Zero;
                        _attackLock = !_attackHitThisSwing && !_onGround && !_isOnWall;
                        _attackHitThisSwing = false;
                    }
                }
            }
        }

        else if (!_specialAttack.IsActive)
        {
                {
                    _velocity.Y += Gravity * dt;

                if (_wallJumpTimer > 0)
                {
                    _wallJumpTimer -= dt;
                }
                else 
                {
                    _velocity.X = 0; 

                    if (currentState.IsKeyDown(Keys.A)) 
                    {
                        _velocity.X = -Speed;
                        _facing = SpriteEffects.FlipHorizontally;
                    }
                    else if (currentState.IsKeyDown(Keys.D)) 
                    {
                        _velocity.X = Speed;
                        _facing = SpriteEffects.None;
                    }
                }

                float nextX = _pos.X + _velocity.X * dt;
                Rectangle wallHitbox = new Rectangle((int)nextX + 24, (int)_pos.Y + 18, 16, 6); 
                bool canMoveX = true;

                foreach (var collisionX in _collisionObjects)
                {
                    if (collisionX.IsOneWay || collisionX.IsSpikes)
                        continue;

                    if (wallHitbox.Intersects(collisionX.Bounds))
                    {
                        canMoveX = false;
                        if (_velocity.X > 0)
                            _pos.X = collisionX.Bounds.Left - 40; 
                        else if (_velocity.X < 0)
                            _pos.X = collisionX.Bounds.Right - 24;
                        _velocity.X = 0;
                        break;
                    }
                }

                if (canMoveX)
                    _pos.X = nextX;

                float nextY = _pos.Y + _velocity.Y * dt;
                bool foundGround = false; 
                Rectangle nextHeadHitbox = new Rectangle((int)_pos.X + 24, (int)nextY + 12, 16, 4);
                Rectangle nextFeetHitbox = new Rectangle((int)_pos.X + 24, (int)nextY + 28, 16, 4);
                float currentFeetY = _pos.Y + 31f;
                float projectedFeetY = nextY + 31f;

                foreach (var collisionY in _collisionObjects)
                {
                    Rectangle rect = collisionY.Bounds;

                    if (collisionY.IsSpikes)
                        continue;

                    if (!collisionY.IsOneWay && _velocity.Y < 0 && nextHeadHitbox.Intersects(rect))
                    {
                        _pos.Y = rect.Bottom - 12; 
                        _velocity.Y = 0;
                        nextY = _pos.Y;
                        projectedFeetY = nextY + 31f;
                    }

                    bool overlapX = ((int)_pos.X + 24) < rect.Right && ((int)_pos.X + 40) > rect.Left;
                    bool crossedTop = overlapX && currentFeetY <= rect.Top && projectedFeetY >= rect.Top;
                    bool touchedSolidFeet = !collisionY.IsOneWay && nextFeetHitbox.Intersects(rect);

                    if (_velocity.Y >= 0 && (crossedTop || touchedSolidFeet))
                    {
                        if (collisionY.IsOneWay)
                        {
                            if (currentState.IsKeyDown(Keys.S))
                            {
                                continue;
                            }

                            _pos.Y = rect.Top - 31;
                            _velocity.Y = 0;
                            foundGround = true;
                            nextY = _pos.Y;
                        }

                        else
                        {
                            _pos.Y = rect.Top - 31;
                            _velocity.Y = 0;
                            foundGround = true;
                            nextY = _pos.Y;
                        }

                        projectedFeetY = nextY + 31f;
                    }
                }


                if (!foundGround && _velocity.Y != 0)
                {
                    _pos.Y = nextY;
                }
                _onGround = foundGround;

                if (_jumpPressed && _onGround)
                {
                    _velocity.Y = JumpForce;
                    _onGround = false;
                }

                _isOnWall = false;
                _wallDirection = 0;

                if (!_onGround && _velocity.Y > 0) 
                {
                    Rectangle wallCheckLeft = new Rectangle((int)_pos.X + 21, (int)_pos.Y + 18, 3, 6);
                    Rectangle wallCheckRight = new Rectangle((int)_pos.X + 40, (int)_pos.Y + 18, 3, 6);

                    foreach (var collisionWall in _collisionObjects)
                    {
                        if (collisionWall.IsOneWay)
                            continue;
                            
                        if (wallCheckLeft.Intersects(collisionWall.Bounds))
                        {
                            _isOnWall = true;
                            _wallDirection = -1;
                            break;
                        }

                        if (wallCheckRight.Intersects(collisionWall.Bounds))
                        {
                            _isOnWall = true;
                            _wallDirection = 1;
                            break;
                        }
                    }
                }

                if (_isOnWall && _jumpPressed)
                {
                    _onGround = false;
                    _isOnWall = false;
                    _velocity.Y = JumpForce;
                    _velocity.X = -_wallDirection * (Speed * 0.65f); 
                    _wallJumpTimer = 0.25f; 
                }

                if (_onGround || _isOnWall)
                {
                    _attackLock = false;
                }

                playerRect = new Rectangle((int)_pos.X + 24, (int)_pos.Y + 14, 16, 18);

                bool isTouchingSpikes = false;
                foreach (var col in _collisionObjects)
                {
                    if (!col.IsSpikes)
                        continue;

                    if (playerRect.Intersects(col.Bounds))
                    {
                        isTouchingSpikes = true;
                        break;
                    }
                }

                if (_onGround && !_isAttacking && !_isOnWall && !isTouchingSpikes && (_stats == null || !_stats.HasKnockback))
                {
                    _lastSafePosition = _pos;
                }

                foreach (var col in _collisionObjects)
                {
                    if (!col.IsSpikes)
                        continue;

                    if (_spikeImmunity > 0f)
                        continue;

                    if (!playerRect.Intersects(col.Bounds))
                        continue;

                    if (_currentLevelName != "level4")
                    {
                    _stats?.TakeDamage(15);
                    _damageTexts.Add(new DamageText(_pos + new Vector2(24, -10), 15));

                    _pos = _lastSafePosition;
                    _velocity = Vector2.Zero;
                    _isAttacking = false;
                    _attackAnimState = 0;
                    _currentFrame = 0;
                    _attackAnimTimer = 0;
                    _attackDistanceLeft = 0f;
                    _attackCooldown = 0f;
                    _attackDir = Vector2.Zero;
                    _isOnWall = false;
                    _wallJumpTimer = 0f;
                    _hitStopTimer = 0.2f;
                    _darkScreen = 0.2f;
                    _spikeImmunity = SpikeDamageImmunityTime;
                    playerRect = new Rectangle((int)_pos.X + 24, (int)_pos.Y + 14, 16, 18);

                    break;
                    }

                    else
                    {
                    _stats?.TakeDamage(100);
                    _damageTexts.Add(new DamageText(_pos + new Vector2(24, -10), 15));

                    _pos = _lastSafePosition;
                    _velocity = Vector2.Zero;
                    _isAttacking = false;
                    _attackAnimState = 0;
                    _currentFrame = 0;
                    _attackAnimTimer = 0;
                    _attackDistanceLeft = 0f;
                    _attackCooldown = 0f;
                    _attackDir = Vector2.Zero;
                    _isOnWall = false;
                    _wallJumpTimer = 0f;
                    _hitStopTimer = 0.2f;
                    _darkScreen = 0.2f;
                    _spikeImmunity = SpikeDamageImmunityTime;
                    playerRect = new Rectangle((int)_pos.X + 24, (int)_pos.Y + 14, 16, 18);

                    break;
                    }
                }
            }

            playerRect = new Rectangle((int)_pos.X + 24, (int)_pos.Y + 14, 16, 18);

            foreach (var exit in _levelExits)
            {
                if (playerRect.Intersects(exit.Bounds))
                {
                    string nextSpawn = string.IsNullOrWhiteSpace(exit.TargetSpawn) ? "Spawn" : exit.TargetSpawn;

                    if (!string.IsNullOrEmpty(exit.TargetLevel))
                    {
                        LoadLevel(exit.TargetLevel, nextSpawn);
                        UpdateCamera();
                    }
                    break;
                }
            }

            if (_isAttacking)
            {
                _currentRow = 8;
                _framesInRow = 3;
                if (_currentFrame >= _framesInRow) 
                    _currentFrame = 0;
            }

            if (_isOnWall)
            {
                _velocity.Y -= _velocity.Y * 0.3f;
                _currentRow = 3;
                _currentFrame = 0;
                _framesInRow = 1;

                if (_wallDirection == -1)
                    _facing = SpriteEffects.FlipHorizontally;
                else
                    _facing = SpriteEffects.None;
            }


            else if (!_onGround)
            {
                _currentRow = 2;
                _framesInRow = 3;
            }

            else if (Math.Abs(_velocity.X) > 10f)
            {
                _currentRow = 4;
                _framesInRow = 8;
            }

            else
            {
                _currentRow = 0;
                _framesInRow = 7;
            }

            if (!_isOnWall && !_isAttacking)
            {
                _animTimer += dt;
                if (_animTimer > 0.1f)
                {
                    _currentFrame = (_currentFrame + 1) % _framesInRow;
                    _animTimer = 0;
                }
            }

            if (_currentFrame >= _framesInRow)
                _currentFrame = 0;
        }

        for (int i = _damageTexts.Count - 1; i >= 0; i--)
        {
            _damageTexts[i].Update(gameTime);
            if (_damageTexts[i].IsDead) _damageTexts.RemoveAt(i);
        }

        _previousRow = _currentRow;
        UpdateCamera();

        _mapRenderer.Update(gameTime);
        if (string.Equals(_currentLevelName, "level1", StringComparison.OrdinalIgnoreCase))
            _dummy.Update(gameTime);
        for (int i = _skeletons.Count - 1; i >= 0; i--)
        {
            _skeletons[i].Update(gameTime, _pos, _collisionObjects, _stats);
            if (_skeletons[i].IsDead())
                _skeletons.RemoveAt(i);
        }

        for (int i = _fireWisps.Count - 1; i >= 0; i--)
        {
            _fireWisps[i].Update(gameTime, _pos, _collisionObjects, _stats, _damageTexts);
            if (_fireWisps[i].IsDead())
                _fireWisps.RemoveAt(i);
        }

        if (_stats != null && _stats.Health <= 0f)
        {
            _isRespawning = true;
            _respawnTimer = 1.25f;
            _gameOverScreen.Show(_respawnTimer);
            _velocity = Vector2.Zero;
            _isAttacking = false;
            _attackLock = false;
            _pendingAttackBounce = false;
            _attackDir = Vector2.Zero;
            _attackDistanceLeft = 0f;
        }
        _previousState = currentState;
        _previousMouseState = currentMouseState;
        base.Update(gameTime);
    }


    private void UpdateCamera()
    {
        float screenWidth = GraphicsDevice.Viewport.Width / zoom;
        float screenHeight = GraphicsDevice.Viewport.Height / zoom;
        _cameraPos.X = _pos.X - (screenWidth / 2);
        _cameraPos.Y = _pos.Y - (screenHeight / 2);

        if (_cameraPos.X < 0) _cameraPos.X = 0;
        if (_cameraPos.Y < 0) _cameraPos.Y = 0;
        if (_cameraPos.Y > _map.HeightInPixels - screenHeight)
            _cameraPos.Y = _map.HeightInPixels - screenHeight;
        if (_cameraPos.X > _map.WidthInPixels - screenWidth)
            _cameraPos.X = _map.WidthInPixels - screenWidth;
    }


    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var cameraMatrix = Matrix.CreateTranslation(-_cameraPos.X, -_cameraPos.Y, 0) * Matrix.CreateScale(zoom);
        Rectangle sourceRect = new Rectangle(_currentFrame * _frameWidth, _currentRow * _frameHeight, _frameWidth, _frameHeight);
        _mapRenderer.Draw(cameraMatrix);

        SpriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
        if (string.Equals(_currentLevelName, "level1", StringComparison.OrdinalIgnoreCase))
            _dummy.Draw(SpriteBatch);

        foreach (var skeleton in _skeletons)
            skeleton.Draw(SpriteBatch);

        foreach (var fireWisp in _fireWisps)
            fireWisp.Draw(SpriteBatch, _whitePixel);

        Rectangle characterSourceRect = sourceRect;
        if (_specialAttack.IsActive)
        {
            characterSourceRect = new Rectangle(_specialAttack.CurrentFrame * _frameWidth, _specialAttack.CurrentRow * _frameHeight, _frameWidth, _frameHeight);
        }

        Color characterTint = Color.White;
        if (_specialAttack.IsCharging && !_specialAttack.IsActive)
        {
            bool blinkOn = ((int)(gameTime.TotalGameTime.TotalSeconds * 12f) % 2) == 0;
            characterTint = blinkOn ? Color.White : Color.LightGray;
        }

        SpriteBatch.Draw(_character, _pos, characterSourceRect, characterTint, 0f, Vector2.Zero, 1f, _facing, 0f);
        foreach (var text in _damageTexts)
        {
            text.Draw(SpriteBatch, _damageFont);
        }
        SpriteBatch.End();

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_stats != null)
            _stats.DrawHealthBar(SpriteBatch, _whitePixel, gameTime);
        _specialAttackHud.Draw(SpriteBatch, _whitePixel, _specialAttack.CooldownRemaining);
        SpriteBatch.End();

        if (_darkScreen > 0f)
        {
            float darkness = 0.35f;
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            SpriteBatch.Draw(_whitePixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * darkness);
            SpriteBatch.End();
        }

        if (_gameOverScreen != null && _gameOverScreen.IsVisible)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameOverScreen.Draw(SpriteBatch, _whitePixel, _damageFont, GraphicsDevice.Viewport);
            SpriteBatch.End();
        }

        base.Draw(gameTime);
    }

    
}

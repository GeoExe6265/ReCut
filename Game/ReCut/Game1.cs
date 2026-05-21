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
    private Skeleton _skeleton;
    private Texture2D _skeletonIdle, _skeletonWalk, _skeletonAttack1, _skeletonAttack2, _skeletonHurt, _skeletonDie;
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
    private bool _isOnCooldown;
    private bool _isOnWall;
    private float zoom = 2.5f;
    private float _attackAnimTimer = 0f;
    private float _attackCooldown = 0f;
    private float _wallJumpTimer = 0f;
    private float _hitStopTimer = 0f;
    private float _spikeDamageImmunityTimer = 0f;
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
    private const float Gravity = 1600f;
    private const float JumpForce = -600f;
    private const float Speed = 350f;
    private const float AttackCooldownTime = 0.75f;
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

    // Я ЭТУ ПРОГРУЗКУ ДЕЛАЛ 2 С ПОЛОВИНОЙ ЧАСА. МОЖНО МНЕ ОТДОХНУТЬ? ;-;
    private void LoadLevel(string levelName, string spawnName)
    {
        _currentLevelName = levelName;
        _collisionObjects.Clear();
        _levelExits.Clear();
        _map = GetCachedLevel(levelName);
        _mapRenderer = new TiledMapRenderer(GraphicsDevice, _map);
        var objects = _map.GetLayer<TiledMapObjectLayer>("Collision").Objects;

        _pos = ResolveSpawnPosition(objects, spawnName);

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

        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
        _stats = new PlayerStats();

        for (int i = 1; i <= 4; i++) 
            _dummyIdleFrames.Add(Content.Load<Texture2D>("enemies/dummy/dummy_idle"));

        for (int i = 1; i <= 5; i++) 
            _dummyHitFrames.Add(Content.Load<Texture2D>($"enemies/dummy/dummy_hit{i}"));

        _dummy = new Target(_dummyIdleFrames, _dummyHitFrames, new Vector2(500, 497));
        _skeleton = new Skeleton(_skeletonIdle, _skeletonWalk, _skeletonAttack1, _skeletonAttack2, _skeletonHurt, _skeletonDie, new Vector2(1200, 450));

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

        if (_spikeDamageImmunityTimer > 0f)
        {
            _spikeDamageImmunityTimer -= dt;
            if (_spikeDamageImmunityTimer < 0f)
                _spikeDamageImmunityTimer = 0f;
        }

        if (_stats != null)
            _stats.Update(gameTime);
        bool _jumpPressed = (currentState.IsKeyDown(Keys.Space) && _previousState.IsKeyUp(Keys.Space)) || (currentState.IsKeyDown(Keys.W) && _previousState.IsKeyUp(Keys.W));

        if (_hitStopTimer > 0)
        {
            _hitStopTimer -= dt;
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
    
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && !_isAttacking && _attackCooldown <= 0) 
        {
            _isAttacking = true;
            _attackCooldown = AttackCooldownTime;
            _attackAnimState = 0;
            _currentRow = 8;
            _currentFrame = 0;
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

                if (attackHitbox.Intersects(_dummy.Hitbox))
                {
                    _dummy.IsHit = true; 
                    hitWall = true;
                    _attackCooldown = 0;
                    _hitStopTimer = 0.2f;
                    _damageTexts.Add(new DamageText(_dummy.Position + new Vector2(16, -10), 5));
                }

                if (_skeleton != null && attackHitbox.Intersects(_skeleton.Hitbox))
                {
                    hitWall = true;
                    _attackCooldown = 0;
                    _hitStopTimer = 0.2f;
                    bool died = _skeleton.TakeDamage(1);
                    _damageTexts.Add(new DamageText(_skeleton.Position + new Vector2(16, -10), died ? 12 : 8));
                    if (died)
                    {
                        _skeleton = null;
                    }
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
                    }
                }
            }
        }

        else 
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

                    if (_spikeDamageImmunityTimer > 0f)
                        continue;

                    if (!playerRect.Intersects(col.Bounds))
                        continue;

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
                    _spikeDamageImmunityTimer = SpikeDamageImmunityTime;
                    playerRect = new Rectangle((int)_pos.X + 24, (int)_pos.Y + 14, 16, 18);

                    break;
                }
            }

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
        _dummy.Update(gameTime);
        if (_skeleton != null)
            _skeleton.Update(gameTime, _pos, _collisionObjects, _stats);
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
            if (_currentLevelName == "level1")
        {
            _dummy.Draw(SpriteBatch);
            if (_skeleton != null)
                _skeleton.Draw(SpriteBatch);
        }

        SpriteBatch.Draw(_character, _pos, sourceRect, Color.White, 0f, Vector2.Zero, 1f, _facing, 0f);
        foreach (var text in _damageTexts)
        {
            text.Draw(SpriteBatch, _damageFont);
        }
        SpriteBatch.End();

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_stats != null)
            _stats.DrawHealthBar(SpriteBatch, _whitePixel, gameTime);
        SpriteBatch.End();

        if (_hitStopTimer > 0f)
        {
            float darkness = 0.35f;
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            SpriteBatch.Draw(_whitePixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * darkness);
            SpriteBatch.End();
        }

        base.Draw(gameTime);
    }

    
}

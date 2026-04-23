using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.Collections.Generic;

namespace ReCut;

public class Game1 : Core
{
    private KeyboardState _previousState;
    private MouseState _previousMouseState;
    private Texture2D _character;
    private Target _dummy;
    private TiledMap _map;
    private TiledMapRenderer _mapRenderer;
    private List<Texture2D> _dummyIdleFrames = new List<Texture2D>();
    private List<Texture2D> _dummyHitFrames = new List<Texture2D>();
    private List<Rectangle> _collisionRectangles = new List<Rectangle>();
    private Vector2 _pos;
    private Vector2 _cameraPos;
    private Vector2 _velocity;
    private Vector2 _attackDir;
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
    private float _animTimer;
    private float _attackDistanceLeft;
    private int _frameWidth = 64;
    private int _frameHeight = 32;
    private int _framesInRow = 7;
    private int _previousRow = -1;
    private int _attackAnimState = 0;
    private int _wallDirection = 0;
    private int _currentFrame, _currentRow;
    private const float Gravity = 1600f;
    private const float JumpForce = -600f;
    private const float Speed = 350f;
    private const float AttackCooldownTime = 0.75f;
    public Game1() : base("ReCut", 1280, 720, false)
    {
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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

    // ТЕХ ЧАСТЬ НЕ ТРОГАТЬ СВОИМИ РУЧКАМИ!
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        // TODO: use this.Content to load your game content here
        _character = Content.Load<Texture2D>("images/character");
        _map = Content.Load<TiledMap>("maps/level1");
        _mapRenderer = new TiledMapRenderer(GraphicsDevice, _map);

        for (int i = 1; i <= 4; i++) 
        _dummyIdleFrames.Add(Content.Load<Texture2D>("enemies/dummy/dummy_idle"));

        for (int i = 1; i <= 5; i++) 
            _dummyHitFrames.Add(Content.Load<Texture2D>($"enemies/dummy/dummy_hit{i}"));

        _dummy = new Target(_dummyIdleFrames, _dummyHitFrames, new Vector2(500, 497));

        var collisionLayer = _map.GetLayer<TiledMapObjectLayer>("Collision");
        foreach (var obj in collisionLayer.Objects)
        {
            if (obj.Name == "Spawn")
            {
                _pos = obj.Position;   
            }

            else
            {
                _collisionRectangles.Add(new Rectangle((int)obj.Position.X, (int)obj.Position.Y, (int)obj.Size.Width, (int)obj.Size.Height));
            }

        }
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentState = Keyboard.GetState();
        MouseState currentMouseState = Mouse.GetState();
        Vector2 mouseWorldPos = (currentMouseState.Position.ToVector2() / zoom) + _cameraPos;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
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
                foreach (var rect in _collisionRectangles)
                    if (attackHitbox.Intersects(rect))
                    {
                        hitWall = true;
                        break;
                    }

                if (attackHitbox.Intersects(_dummy.Hitbox))
                {
                    _dummy.IsHit = true; 
                    hitWall = true;
                    _attackCooldown = 0;
                    _hitStopTimer = 0.2f;
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

                foreach (var rect in _collisionRectangles)
                {
                    if (wallHitbox.Intersects(rect))
                    {
                        canMoveX = false;
                        if (_velocity.X > 0)
                            _pos.X = rect.Left - 40; 
                        else if (_velocity.X < 0)
                            _pos.X = rect.Right - 24;
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

                foreach (var rect in _collisionRectangles)
                {
                    if (_velocity.Y < 0 && nextHeadHitbox.Intersects(rect))
                    {
                        _pos.Y = rect.Bottom - 12;
                        _velocity.Y = 0;
                        nextY = _pos.Y;
                    }

                    if (_velocity.Y >= 0 && nextFeetHitbox.Intersects(rect))
                    {
                        _pos.Y = rect.Top - 31;
                        _velocity.Y = 0;
                        foundGround = true;
                        nextY = _pos.Y;
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
                    Rectangle wallCheckLeft = new Rectangle((int)_pos.X + 21, (int)_pos.Y + 10, 3, 16);
                    Rectangle wallCheckRight = new Rectangle((int)_pos.X + 40, (int)_pos.Y + 10, 3, 16);

                    foreach (var rect in _collisionRectangles)
                    {
                        if (wallCheckLeft.Intersects(rect))
                        {
                            _isOnWall = true;
                            _wallDirection = -1;
                            break;
                        }

                        if (wallCheckRight.Intersects(rect))
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

        _previousRow = _currentRow;
        UpdateCamera();

        _mapRenderer.Update(gameTime);
        _dummy.Update(gameTime);
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
        _dummy.Draw(SpriteBatch);

        SpriteBatch.Draw(_character, _pos, sourceRect, Color.White, 0f, Vector2.Zero, 1f, _facing, 0f);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

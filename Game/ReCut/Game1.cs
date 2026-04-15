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
    private TiledMap _map;
    private TiledMapRenderer _mapRenderer;
    private List<Rectangle> _collisionRectangles = new List<Rectangle>();
    private Vector2 _pos;
    private Vector2 _cameraPos;
    private Vector2 _velocity;
    private Vector2 _attackDir;
    private SpriteEffects _facing = SpriteEffects.None;
    private bool _onGround;
    private bool _isAttacking = false;
    private bool _isOnCooldown;
    private float zoom = 2.5f;
    private float _attackAnimTimer = 0f;
    private float _attackCooldown = 0f;
    private float _animTimer;
    private float _attackDistanceLeft;
    private int _frameWidth = 64;
    private int _frameHeight = 32;
    private int _framesInRow = 7;
    private int _previousRow = -1;
    private int _attackAnimState = 0;
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
                if (_attackAnimTimer > 0.06f)
                {
                    _currentFrame++;
                    _attackAnimTimer = 0;
                    if (_currentFrame > 2)
                    {
                        _isAttacking = false;
                    }
                }
            }
        }

        else 
        {
            _velocity.X = 0;
            _velocity.Y += Gravity * dt;

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

            if ((currentState.IsKeyDown(Keys.Space) || currentState.IsKeyDown(Keys.W)) && _onGround)
            {
                _velocity.Y = JumpForce;
                _onGround = false;
            }

            float nextX = _pos.X + _velocity.X * dt;
            Rectangle nextHitboxX = new Rectangle((int)nextX + 24, (int)_pos.Y + 14, 16, 14); 
            bool canMove = true;
            
            foreach (var rect in _collisionRectangles)
                if (nextHitboxX.Intersects(rect))
                {
                    canMove = false;
                    _velocity.X = 0;
                    break;
                }
            
            if (canMove)
                _pos.X = nextX;

            _pos.Y += _velocity.Y * dt;
            bool foundGround = false; 
            Rectangle feetHitbox = new Rectangle((int)_pos.X + 24, (int)_pos.Y + 16, 16, 16);

            foreach (var rect in _collisionRectangles)
            {
                if (feetHitbox.Intersects(rect))
                {
                    if (_velocity.Y >= 0)
                    {
                        _pos.Y = rect.Top - 31;
                        _velocity.Y = 0;
                        foundGround = true;
                    }
                    
                    else
                    {
                        _pos.Y = rect.Bottom - 14;
                        _velocity.Y = 0;
                    }
                }
            }
            _onGround = foundGround;

            if (!_onGround)
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

            _animTimer += dt;
            if (_animTimer > 0.1f)
            {
                _currentFrame = (_currentFrame + 1) % _framesInRow;
                _animTimer = 0;
            }
        }

        if (_previousRow != _currentRow)
        {
            _currentFrame = 0;
            _animTimer = 0;
        }
        _previousRow = _currentRow;
        UpdateCamera();

        _mapRenderer.Update(gameTime);
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
        
        // TODO: Add your drawing code here
        _mapRenderer.Draw(cameraMatrix);

        SpriteBatch.Begin(transformMatrix: cameraMatrix, samplerState: SamplerState.PointClamp);
        SpriteBatch.Draw(_character, _pos, sourceRect, Color.White, 0f, Vector2.Zero, 1f, _facing, 0f);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

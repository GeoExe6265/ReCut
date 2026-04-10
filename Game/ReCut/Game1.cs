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
    private Texture2D _character;
    private TiledMap _map;
    private TiledMapRenderer _mapRenderer;
    private List<Rectangle> _collisionRectangles = new List<Rectangle>();
    private Vector2 _pos;
    private Vector2 _cameraPos;
    private Vector2 _velocity;
    private SpriteEffects _facing = SpriteEffects.None;
    private bool _onGround;
    private float zoom = 2.5f;
    private float _animTimer;
    private int _frameWidth = 64;
    private int _frameHeight = 32;
    private int _framesInRow = 7;
    private int _previousRow = -1;
    private int _currentFrame, _currentRow;
    private const float Gravity = 1600f;
    private const float JumpForce = -600f;
    private const float Speed = 350f;
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
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (currentState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (currentState.IsKeyDown(Keys.F11) && _previousState.IsKeyUp(Keys.F11))
        {
            if (Graphics.IsFullScreen == false)
                ToggleFullScreen();
            else
                ToggleWindowedMode();
        }

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
        {
            if (nextHitboxX.Intersects(rect))
            {
                canMove = false;
                _velocity.X = 0; 
                break;
            }
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
                if (_velocity.Y > 0)
                {
                    _pos.Y = rect.Top - 31;
                    _velocity.Y = 0;
                    foundGround = true;
                }
                else if (_velocity.Y < 0)
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

        if (_previousRow != _currentRow)
        {
            _currentFrame = 0;
            _animTimer = 0;
        }
        _previousRow = _currentRow;

        _animTimer += dt;
        if (_animTimer > 0.1f)
        {
            _currentFrame++;
            if (_currentFrame >= _framesInRow)
                _currentFrame = 0;
            _animTimer = 0;
        }
        UpdateCamera();

        _mapRenderer.Update(gameTime);
        _previousState = currentState;
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

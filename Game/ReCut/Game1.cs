using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
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
    private Vector2 _velocity;
    private bool _onGround;
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

        // TODO: Add your update logic here
        if (currentState.IsKeyDown(Keys.Escape))
        Exit();

        if (currentState.IsKeyDown(Keys.F11) && _previousState.IsKeyUp(Keys.F11))
        {
            if (Graphics.IsFullScreen == false)
                ToggleFullScreen();
            else
                ToggleWindowedMode();
        }
        
        _previousState = currentState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _mapRenderer.Draw();

        SpriteBatch.Begin();
        SpriteBatch.Draw(_character, _pos, Color.White);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

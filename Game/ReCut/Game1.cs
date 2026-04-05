using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ReCut;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private KeyboardState _previousState;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.IsFullScreen = false;
    }

    public void ToggleFullScreen()
    {
        _graphics.IsFullScreen = true;
        _graphics.HardwareModeSwitch = false;
        _graphics.ApplyChanges();
    }

    public void ToggleWindowedMode()
    {
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }

    // ТЕХ ЧАСТЬ НЕ ТРОГАТЬ СВОИМИ РУЧКАМИ!
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentState = Keyboard.GetState();

        // TODO: Add your update logic here
        if (currentState.IsKeyDown(Keys.Escape))
        Exit();

        if (currentState.IsKeyDown(Keys.F11) && _previousState.IsKeyUp(Keys.F11))
        {
            if (_graphics.IsFullScreen == false)
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

        base.Draw(gameTime);
    }
}

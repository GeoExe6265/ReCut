using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace ReCut;

public class Game1 : Core
{
    private KeyboardState _previousState;
    private Texture2D _character;
    public Game1() : base("ReCut", 1280, 720, false)
    {
        Content.RootDirectory = "Content/images";
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

        // TODO: use this.Content to load your game content here
        _character = Content.Load<Texture2D>("character");
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
        SpriteBatch.Begin();
        SpriteBatch.Draw(
        _character,
        new Vector2(
            (Window.ClientBounds.Width * 0.5f) - (_character.Width * 0.5f),
            (Window.ClientBounds.Height * 0.5f) - (_character.Height * 0.5f)),
        Color.White
        );
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

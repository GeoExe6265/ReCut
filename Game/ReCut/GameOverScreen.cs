using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReCut;

public class GameOverScreen
{
    private float _timer = 0f;

    public bool IsVisible => _timer > 0f;

    public void Show(float durationSeconds)
    {
        _timer = durationSeconds;
    }

    public void Update(GameTime gameTime)
    {
        if (_timer <= 0f)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer -= dt;
        if (_timer < 0f)
            _timer = 0f;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D whitePixel, SpriteFont font, Viewport viewport)
    {
        if (_timer <= 0f || spriteBatch == null || whitePixel == null || font == null)
            return;

        float pulse = 0.5f + (float)System.Math.Sin(_timer * 8f) * 0.15f;
        spriteBatch.Draw(whitePixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.72f);

        string title = "GAME OVER";
        string subtitle = "Respawning...";

        Vector2 titleSize = font.MeasureString(title);
        Vector2 subtitleSize = font.MeasureString(subtitle);
        Vector2 titlePos = new Vector2((viewport.Width - titleSize.X) * 0.5f, viewport.Height * 0.38f);
        Vector2 subtitlePos = new Vector2((viewport.Width - subtitleSize.X) * 0.5f, viewport.Height * 0.5f);

        spriteBatch.DrawString(font, title, titlePos + new Vector2(2f, 2f), Color.Black * 0.8f, 0f, Vector2.Zero, 1.35f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, title, titlePos, Color.White * pulse, 0f, Vector2.Zero, 1.35f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, subtitle, subtitlePos, Color.Gainsboro, 0f, Vector2.Zero, 0.95f, SpriteEffects.None, 0f);
    }
}
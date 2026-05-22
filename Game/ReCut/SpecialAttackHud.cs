using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReCut;

public class SpecialAttackHud
{
    private const int IconSize = 40;
    private const float CooldownTime = 25f;

    public Vector2 Position { get; set; }

    public SpecialAttackHud(Vector2 position)
    {
        Position = position;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D whitePixel, float cooldownRemaining)
    {
        if (spriteBatch == null || whitePixel == null)
            return;

        int iconX = (int)Position.X;
        int iconY = (int)Position.Y;

        Rectangle iconRect = new Rectangle(iconX, iconY, IconSize, IconSize);
        spriteBatch.Draw(whitePixel, iconRect, Color.Black);

        float clampedCooldown = MathHelper.Clamp(cooldownRemaining, 0f, CooldownTime);
        float fillRatio = 1f - (clampedCooldown / CooldownTime);

        if (clampedCooldown > 0f)
        {
            int innerPadding = 4;
            int fillHeight = (int)((IconSize - innerPadding) * fillRatio);
            if (fillHeight > 0)
            {
                Rectangle fillRect = new Rectangle(iconX + innerPadding / 2, iconY + IconSize - innerPadding - fillHeight, IconSize - innerPadding, fillHeight);
                spriteBatch.Draw(whitePixel, fillRect, Color.DarkGray);
            }
        }

        Color slashColor = clampedCooldown > 0f ? Color.Gray : Color.White;
        Vector2 center = new Vector2(iconX + IconSize / 2f, iconY + IconSize / 2f);
        float thickness = 4f;
        float length = IconSize * 1.1f;
        float rotation = MathHelper.ToRadians(-45f);
        spriteBatch.Draw(whitePixel, center, null, slashColor, rotation, new Vector2(0.5f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
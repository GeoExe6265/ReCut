using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ReCut;

public class Target
{
    public Vector2 Position;
    public Rectangle Hitbox;
    
    private List<Texture2D> _idleFrames;
    private List<Texture2D> _hitFrames;
    
    private int _currentFrame = 0;
    private float _animTimer;
    public bool IsHit = false;
    private float _hitResetTimer = 0f;

    public Target(List<Texture2D> idle, List<Texture2D> hit, Vector2 pos)
    {
        _idleFrames = idle;
        _hitFrames = hit;
        Position = pos;
        Hitbox = new Rectangle((int)pos.X, (int)pos.Y, _idleFrames[0].Width, _idleFrames[0].Height);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (IsHit)
        {
            _hitResetTimer += dt;
            _animTimer += dt;
            if (_animTimer > 0.07f)
            {
                _currentFrame++;
                if (_currentFrame >= _hitFrames.Count) _currentFrame = _hitFrames.Count - 1;
                _animTimer = 0;
            }
            if (_hitResetTimer > 0.4f) { IsHit = false; _hitResetTimer = 0f; _currentFrame = 0; }
        }
        else
        {
            _animTimer += dt;
            if (_animTimer > 0.15f)
            {
                _currentFrame = (_currentFrame + 1) % _idleFrames.Count;
                _animTimer = 0;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var frames = IsHit ? _hitFrames : _idleFrames;
        spriteBatch.Draw(frames[_currentFrame], Position, Color.White);
    }
}
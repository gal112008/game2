using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class Enemy
    {
        public Texture2D Texture;
        public Vector2 Position;
        public float Health;
        public float Speed;
        public bool IsActive = true;

        protected List<Vector2> _waypoints;
        protected int _currentWaypointIndex = 0;

        public Enemy(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float health, float speed)
        {
            Texture = texture;
            Position = startPosition;
            _waypoints = waypoints;
            Health = health;
            Speed = speed;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            if (_currentWaypointIndex < _waypoints.Count)
            {
                Vector2 target = _waypoints[_currentWaypointIndex];
                Vector2 direction = target - Position;

                if (direction.Length() < Speed)
                {
                    Position = target;
                    _currentWaypointIndex++;
                }
                else
                {
                    direction.Normalize();
                    Position += direction * Speed;
                }
            }
            else { IsActive = false; }

            if (Health <= 0) IsActive = false;
        }

        public virtual void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (IsActive)
            {
                // Draw the sprite
                Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
                spriteBatch.Draw(Texture, Position, null, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0f);

                // Draw HP and Speed text above the head
                // We cast to (int) to keep the UI clean
                string statusText = $"HP: {(int)Health} | SPD: {Speed:0.0}";

                // Calculate position (30 pixels above the sprite)
                Vector2 textPos = new Vector2(Position.X - 30, Position.Y - (Texture.Height / 2) - 20);

                // Draw the text (Black color for visibility)
                spriteBatch.DrawString(font, statusText, textPos, Color.Black);
            }
        } 
    }
}
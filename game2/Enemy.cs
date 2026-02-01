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
        public int CurrentWaypointIndex = 0; // Public for inheritance
        protected int _size;

        public Enemy(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float health, float speed, int size)
        {
            Texture = texture;
            Position = startPosition;
            _waypoints = waypoints;
            Health = health;
            Speed = speed;
            _size = size;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            if (Health <= 0)
            {
                IsActive = false;
                return;
            }

            if (CurrentWaypointIndex < _waypoints.Count)
            {
                Vector2 target = _waypoints[CurrentWaypointIndex];
                Vector2 direction = target - Position;
                if (direction.Length() < Speed)
                {
                    Position = target;
                    CurrentWaypointIndex++;
                }
                else
                {
                    direction.Normalize();
                    Position += direction * Speed;
                }
            }
            else { IsActive = false; }
        }

        public virtual void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (IsActive)
            {
                Rectangle destRect = new Rectangle(
                    (int)Position.X - (_size / 2),
                    (int)Position.Y - (_size / 2),
                    _size,
                    _size
                );
                spriteBatch.Draw(Texture, destRect, Color.White);
                string statusText = $"HP: {(int)Health}";
                Vector2 textPos = new Vector2(Position.X - 20, Position.Y - (_size / 2) - 15);
                spriteBatch.DrawString(font, statusText, textPos, Color.Black);
            }
        }
    }
}
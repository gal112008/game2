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
        protected int _size; // Added to store the TileSize

        public Enemy(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float health, float speed, int size)
        {
            Texture = texture;
            Position = startPosition;
            _waypoints = waypoints;
            Health = health;
            Speed = speed;
            _size = size; // Initialize size
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            if (_currentWaypointIndex < _waypoints.Count)
            {
                Vector2 target = _waypoints[_currentWaypointIndex];
                Vector2 direction = target - Position;
                if (direction.Length() < Speed) { Position = target; _currentWaypointIndex++; }
                else { direction.Normalize(); Position += direction * Speed; }
            }
            else { IsActive = false; }
            if (Health <= 0) IsActive = false;
        }

        public virtual void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (IsActive)
            {
                // Calculate the rectangle so the center of the sprite 
                // matches the center-path coordinate
                Rectangle destRect = new Rectangle(
                    (int)Position.X - (_size / 2),
                    (int)Position.Y - (_size / 2),
                    _size,
                    _size
                );

                spriteBatch.Draw(Texture, destRect, Color.White);

                // Update health text to also be centered above the head
                string statusText = $"HP: {(int)Health}";
                Vector2 textPos = new Vector2(Position.X - 20, Position.Y - (_size / 2) - 15);
                spriteBatch.DrawString(font, statusText, textPos, Color.Black);
            }
        }
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using static game2.TowerManager;

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
        public int CurrentWaypointIndex = 0;
        protected int _size;
        public int GoldReward;
        public DamageType ResistType = DamageType.Physical;

        public Enemy(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float health, float speed, int size, int goldReward)
        {
            Texture = texture;
            Position = startPosition;
            _waypoints = waypoints;
            Health = health;
            Speed = speed;
            _size = size;
            GoldReward = goldReward;
        }

        public void TakeDamage(float amount, DamageType type)
        {
            float multiplier = DamageChart.GetMultiplier(type, this.ResistType);
            Health -= (amount * multiplier);
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            if (Health <= 0) { IsActive = false; return; }

            if (CurrentWaypointIndex < _waypoints.Count)
            {
                Vector2 target = _waypoints[CurrentWaypointIndex];
                Vector2 direction = target - Position;
                if (direction.Length() < Speed) { Position = target; CurrentWaypointIndex++; }
                else { direction.Normalize(); Position += direction * Speed; }
            }
            else { IsActive = false; }
        }

        // Updated Draw: Now draws the resistance icon above the HP
        public virtual void Draw(SpriteBatch spriteBatch, SpriteFont font, Dictionary<DamageType, Texture2D> typeIcons)
        {
            if (!IsActive) return;

            Rectangle destRect = new Rectangle((int)Position.X - (_size / 2), (int)Position.Y - (_size / 2), _size, _size);
            spriteBatch.Draw(Texture, destRect, Color.White);

            // Draw element icon above head
            if (typeIcons.ContainsKey(ResistType))
            {
                spriteBatch.Draw(typeIcons[ResistType], new Rectangle((int)Position.X - 12, (int)Position.Y - (_size / 2) - 35, 24, 24), Color.White);
            }

            string statusText = $"HP: {(int)Health}";
            spriteBatch.DrawString(font, statusText, new Vector2(Position.X - 20, Position.Y - (_size / 2) - 15), Color.Black);
        }
    }
}
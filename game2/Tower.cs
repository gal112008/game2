using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using static game2.TowerManager;

namespace game2
{
    public class Tower
    {
        public Texture2D Texture;
        public Vector2 Position;
        public float Range = 200f;
        public float Cooldown = 0.5f;
        private float _timer = 0f;
        private int _size;
        private Texture2D _bulletTexture;
        public DamageType DamageType = DamageType.Physical;

        public Tower(Texture2D texture, Texture2D bulletTexture, Vector2 position, int size)
        {
            Texture = texture;
            _bulletTexture = bulletTexture;
            Position = position;
            _size = size;
        }

        public void Update(GameTime gameTime, QuadTree spatialIndex, List<Bullet> bullets)
        {
            _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer <= 0)
            {
                Rectangle searchArea = new Rectangle((int)(Position.X - Range), (int)(Position.Y - Range), (int)(Range * 2), (int)(Range * 2));
                List<Enemy> nearbyEnemies = new List<Enemy>();
                spatialIndex.Query(searchArea, nearbyEnemies);

                Enemy target = null;
                float closestDist = Range;
                foreach (var enemy in nearbyEnemies)
                {
                    float dist = Vector2.Distance(Position, enemy.Position);
                    if (dist < closestDist) { closestDist = dist; target = enemy; }
                }

                if (target != null)
                {
                    Bullet b = new Bullet(_bulletTexture, Position, target);
                    b.Type = this.DamageType;
                    bullets.Add(b);
                    _timer = Cooldown;
                }
            }
        }

        // Updated Draw: Now draws the element icon in the corner
        public void Draw(SpriteBatch spriteBatch, Dictionary<DamageType, Texture2D> typeIcons)
        {
            Rectangle destRect = new Rectangle((int)Position.X - (_size / 2), (int)Position.Y - (_size / 2), _size, _size);
            spriteBatch.Draw(Texture, destRect, Color.White);

            if (typeIcons.ContainsKey(DamageType) && DamageType != DamageType.Physical)
            {
                // Draw small icon in bottom right corner
                Rectangle iconRect = new Rectangle(destRect.Right - 18, destRect.Bottom - 18, 16, 16);
                spriteBatch.Draw(typeIcons[DamageType], iconRect, Color.White);
            }
        }
    }
}
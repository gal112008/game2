using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

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

        public Tower(Texture2D texture, Texture2D bulletTexture, Vector2 position, int size)
        {
            Texture = texture;
            _bulletTexture = bulletTexture;
            Position = position;
            _size = size; // Store the TileSize
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
                    bullets.Add(new Bullet(_bulletTexture, Position, target));
                    _timer = Cooldown;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Use a rectangle to scale the texture to TileSize
            Rectangle destRect = new Rectangle(
                (int)Position.X - (_size / 2),
                (int)Position.Y - (_size / 2),
                _size,
                _size
            );

            spriteBatch.Draw(Texture, destRect, Color.White);
        }
    }
}
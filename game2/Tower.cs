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
        public float Cooldown = 0.5f; // Seconds between shots
        private float _timer = 0f;

        // Reference to the bullet texture (we will pass a small part of a texture or a pixel)
        private Texture2D _bulletTexture;

        public Tower(Texture2D texture, Texture2D bulletTexture, Vector2 position)
        {
            Texture = texture;
            _bulletTexture = bulletTexture;
            Position = position;
        }

        public void Update(GameTime gameTime, QuadTree spatialIndex, List<Bullet> bullets)
        {
            _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_timer <= 0)
            {
                // Define a search box around the tower based on its range
                Rectangle searchArea = new Rectangle(
                    (int)(Position.X - Range), (int)(Position.Y - Range),
                    (int)(Range * 2), (int)(Range * 2)
                );

                List<Enemy> nearbyEnemies = new List<Enemy>();
                spatialIndex.Query(searchArea, nearbyEnemies);

                Enemy target = null;
                float closestDist = Range;

                foreach (var enemy in nearbyEnemies)
                {
                    float dist = Vector2.Distance(Position, enemy.Position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        target = enemy;
                    }
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
            Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
            // Draw the Tree Sentinel
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0f);
        }
    }
}
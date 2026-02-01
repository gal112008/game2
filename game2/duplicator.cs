using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class duplicator : Enemy
    {
        private bool _isSpawning = false;
        private float _initialBaseHP;
        private float _spawnTimer = 0f;
        private const float SpawnDelay = 0.5f; // 0.5 second delay
        private int _remainingChildrenToSpawn = 0;

        public duplicator(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float hp, float speed, int size)
            : base(texture, startPosition, waypoints, hp * 10, speed*0.2f, size)
        {
            _initialBaseHP = hp;
        }

        public override void Update(GameTime gameTime)
        {
            // If the enemy has health, move normally
            if (Health > 0)
            {
                base.Update(gameTime);
                return;
            }

            // If health is 0, start the spawning sequence if not already started
            if (!_isSpawning)
            {
                _isSpawning = true;
                _remainingChildrenToSpawn = RandomHelper.GetInt(2, 6);
                _spawnTimer = 0f; // Spawn the first one immediately
            }

            // Manage the timer for children
            if (_remainingChildrenToSpawn <= 0)
            {
                IsActive = false; // Finally kill the parent when done spawning
            }
            else
            {
                _spawnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        public List<Enemy> GetPendingChildren()
        {
            List<Enemy> children = new List<Enemy>();

            // Only spawn if we are in spawning mode and the timer hit zero
            if (_isSpawning && _remainingChildrenToSpawn > 0 && _spawnTimer <= 0)
            {
                Enemy child = new Enemy(
                    Texture,
                    Position,
                    _waypoints,
                    _initialBaseHP / 1.2f,
                    Speed * 2f,
                    _size / 2
                );

                child.CurrentWaypointIndex = this.CurrentWaypointIndex;
                children.Add(child);

                _remainingChildrenToSpawn--;
                _spawnTimer = SpawnDelay; // Reset timer for next child
            }

            return children;
        }

        public override void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            // Only draw the parent if it's still alive (not in spawning mode)
            if (Health > 0)
            {
                base.Draw(spriteBatch, font);
            }
        }
    }
}
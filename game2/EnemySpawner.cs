using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace game2
{
    public class EnemySpawner
    {
        private float _spawnTimer = 0f;
        private float _spawnInterval = 2f;
        private ContentManager _content;
        private int _boss_to_spawn = 0;
        private int _tileSize; // Added to track global size

        public EnemySpawner(ContentManager content, int tileSize)
        {
            _content = content;
            _tileSize = tileSize;
        }

        public void Update(GameTime gameTime, double currentWave, List<Vector2> path, List<Enemy> enemies)
        {
            _spawnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_spawnTimer <= 0)
            {
                float hp = 100f * (float)Math.Pow(1.1, currentWave);
                float spd = 3f;
                int roll = RandomHelper.GetInt(1, 101);

                Enemy e;
                // You must update Tank, fast, and BOSS classes to accept 'int size' as the last parameter
                if (roll <= 30)
                    e = new Tank(_content.Load<Texture2D>("radhanpixel"), path[0], path, hp, spd, _tileSize);
                else if (roll <= 80)
                    e = new fast(_content.Load<Texture2D>("malikethpixel"), path[0], path, hp, spd, _tileSize);
                else
                    e = new Enemy(_content.Load<Texture2D>("treesenpixel"), path[0], path, hp, spd, _tileSize);

                enemies.Add(e);

                if (currentWave % 5 == 0 && currentWave != 0 && _boss_to_spawn == 0)
                {
                    enemies.Add(new BOSS(_content.Load<Texture2D>("maleniapixel"), path[0], path, hp, spd, _tileSize));
                    _boss_to_spawn++;
                }
                _spawnTimer = _spawnInterval;
            }
        }
    }
}
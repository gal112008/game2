using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using static game2.TowerManager;

namespace game2
{
    public class EnemySpawner
    {
        private ContentManager _content;
        private int _tileSize;

        public EnemySpawner(ContentManager content, int tileSize)
        {
            _content = content;
            _tileSize = tileSize;
        }

        // NEW SPAWN METHOD using WaveDirector
        public void SpawnWave(WaveDirector director, List<List<Vector2>> paths, List<Tower> towers, List<Enemy> enemies, double currentWave)
        {
            // 1. GET BEST STRATEGY (AI Choice)
            var strategy = director.GetBestStrategy(paths, towers);
            List<Vector2> chosenPath = paths[strategy.pathIdx];

            float spent = 0;
            // Scale HP slightly with waves
            float baseHp = 100f * (float)Math.Pow(1.1, currentWave);
            float baseSpeed = 3f;

            // 2. SPAWN LOOP (Tanks first 50%, then Fast)
            while (spent < director.TotalBudget)
            {
                Enemy e;
                bool isTankPhase = spent < (director.TotalBudget * 0.5f);

                if (isTankPhase)
                {
                    // --- TANK ---
                    // Slower (0.7x speed), High HP (2x HP), Costs 25
                    e = new Tank(_content.Load<Texture2D>("radhanpixel"), chosenPath[0], chosenPath, baseHp * 2f, baseSpeed * 0.7f, _tileSize);
                    spent += 25;
                }
                else
                {
                    // --- FAST / NORMAL ---
                    int roll = RandomHelper.GetInt(0, 100);
                    if (roll < 30)
                    {
                        // Fast Enemy: Low HP (0.7x), Fast (2x speed), Costs 15
                        e = new fast(_content.Load<Texture2D>("malikethpixel"), chosenPath[0], chosenPath, baseHp * 0.7f, baseSpeed * 2f, _tileSize);
                        spent += 15;
                    }
                    else
                    {
                        // Normal/Duplicator
                        e = new duplicator(_content.Load<Texture2D>("spawner"), chosenPath[0], chosenPath, baseHp, baseSpeed, _tileSize);
                        spent += 20;
                    }
                }

                // Apply the AI's chosen element
                e.ResistType = strategy.element;
                enemies.Add(e);
            }

            // 3. BOSS LOGIC (Every 5 Waves)
            if (currentWave % 5 == 0 && currentWave != 0)
            {
                // Find the SAFEST path (Lowest Risk)
                int safestPathIdx = 0;
                float lowestRisk = float.MaxValue;

                for (int i = 0; i < paths.Count; i++)
                {
                    float r = director.CalculatePathRisk(paths[i], towers);
                    if (r < lowestRisk) { lowestRisk = r; safestPathIdx = i; }
                }

                List<Vector2> bossPath = paths[safestPathIdx];

                // Create Boss
                BOSS boss = new BOSS(_content.Load<Texture2D>("maleniapixel"), bossPath[0], bossPath, baseHp * 1.5f, baseSpeed, _tileSize);

                // --- 2 ELEMENTS LOGIC ---
                // 1. Primary from Strategy
                boss.ResistType = strategy.element;
                // 2. Secondary Random Element
                Array types = Enum.GetValues(typeof(DamageType));
                DamageType randomType = (DamageType)types.GetValue(RandomHelper.GetInt(1, types.Length));
                boss.ExtraResistances.Add(randomType);

                enemies.Add(boss);
            }
        }

        // Empty Update because we spawn everything at start of wave now
        public void Update(GameTime gameTime) { }
    }
}
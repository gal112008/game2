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
            // 1. GET RANKED PATHS by risk level
            var rankedPaths = director.GetRankedPaths(paths, towers);

            // 2. DECIDE HOW MANY PATHS TO USE /curently i set only 2 patsh so i set it too two
            int pathsToUse = Math.Min(2, rankedPaths.Count);
            float budgetPerPath = director.TotalBudget / pathsToUse;

            // Scale HP and Speed based on wave number
            float baseHp = 100f * (float)Math.Pow(1.1, currentWave);
            float baseSpeed = 3f;

            // 3. LOOP THROUGH EACH CHOSEN PATH
            for (int i = 0; i < pathsToUse; i++)
            {
                int pathIdx = rankedPaths[i].Index;//which path you are spawning on rn
                List<Vector2> chosenPath = paths[pathIdx];
                float spentOnThisPath = 0;

                director.ApplyPathFatigue(pathIdx);

                // FIX: Get a fresh strategy specifically for this path list to avoid defaulting to Physical
                var pathStrategy = director.GetBestStrategy(new List<List<Vector2>> { chosenPath }, towers);
                DamageType primaryElement = pathStrategy.element;
                // 4. SPAWN LOOP FOR THIS SPECIFIC PATH
                while (spentOnThisPath < budgetPerPath)
                {
                    Enemy e;
                    bool isTankPhase = spentOnThisPath < (budgetPerPath * 0.2f);

                    if (isTankPhase)
                    {
                        e = new Tank(_content.Load<Texture2D>("radhanpixel"), chosenPath[0], chosenPath, baseHp * 2f, baseSpeed * 0.7f, _tileSize);
                        spentOnThisPath += 25;
                    }
                    else
                    {
                        int roll = RandomHelper.GetInt(0, 100);
                        if (roll < 30)
                        {
                            e = new fast(_content.Load<Texture2D>("malikethpixel"), chosenPath[0], chosenPath, baseHp * 0.7f, baseSpeed * 2f, _tileSize);
                            spentOnThisPath += 15;
                        }
                        else if (roll < 70)
                        {
                            e = new NormalEnemy(_content.Load<Texture2D>("treesenpixel"), chosenPath[0], chosenPath, baseHp, baseSpeed, _tileSize);
                            spentOnThisPath += 15;
                        }
                        else
                        {
                            e = new duplicator(_content.Load<Texture2D>("spawner"), chosenPath[0], chosenPath, baseHp, baseSpeed, _tileSize);
                            spentOnThisPath += 20;
                        }
                    }
                    if (spentOnThisPath < (budgetPerPath * 0.5f))
                    {
                        e.ResistType = primaryElement;
                    }
                    else
                    {
                        // Switches to the counter element for the second half of the wave
                        e.ResistType = GetCounterElement(primaryElement);
                    }

                    enemies.Add(e);
                }
            }

            // 5. BOSS LOGIC (Every 5 Waves)
            if (currentWave % 5 == 0 && currentWave != 0)
            {
                int bossPathIdx = rankedPaths[0].Index;
                List<Vector2> bossPath = paths[bossPathIdx];

                BOSS boss = new BOSS(_content.Load<Texture2D>("maleniapixel"), bossPath[0], bossPath, baseHp * 1.5f, baseSpeed, _tileSize);

                // Use a fresh strategy for the boss path
                var bossStrategy = director.GetBestStrategy(new List<List<Vector2>> { bossPath }, towers);
                boss.ResistType = bossStrategy.element;

                Array types = Enum.GetValues(typeof(DamageType));
       
                boss.ExtraResistances.Add(GetCounterElement(bossStrategy.element));

                enemies.Add(boss);
            }
        }
        public void Update(GameTime gameTime) { }
        public static DamageType GetCounterElement(DamageType enemyResist)
        {
            return enemyResist switch
            {
                DamageType.Ice => DamageType.Fire,     // Fire beats Ice (2.0x)
                DamageType.Fire => DamageType.Water,    // Water beats Fire (2.0x)
                DamageType.Earth => DamageType.Ice,      // Ice beats Earth (2.0x)
                DamageType.Dark => DamageType.Holy,     // Holy beats Dark (2.0x)
                DamageType.Holy => DamageType.Dark,     // Dark beats Holy (2.0x)
                DamageType.Knight => DamageType.Magic,    // Magic beats Knight (1.5x)
                DamageType.Physical => DamageType.Bleed,    // Bleed beats Physical (1.5x)
                _ => DamageType.Physical  // Default fallback
            };
        }
    }
}
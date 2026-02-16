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

        // Per-path tracking: stores every enemy spawned this wave alongside its
        // template kind so we can measure how far each got at wave-end.
        private Dictionary<int, List<(Enemy enemy, SurvivorTemplate.EnemyKind kind)>> _spawnedByPath
            = new Dictionary<int, List<(Enemy, SurvivorTemplate.EnemyKind)>>();

        public EnemySpawner(ContentManager content, int tileSize)
        {
            _content = content;
            _tileSize = tileSize;
        }

        // ------------------------------------------------------------------
        // Call at wave-end (after _enemies.Count == 0 but before Evolve).
        //
        // For each path we collect ALL enemies that were spawned there and
        // build SurvivorTemplate candidates:
        //   - Enemies that reached the base  → WaypointReached = int.MaxValue
        //   - Enemies killed on the path     → WaypointReached = their last waypoint index
        //
        // WaveDirector.RecordSurvivors then keeps the top 3 (furthest first).
        // This means even if NOBODY got through, we still remember who came closest.
        // ------------------------------------------------------------------
        public void RecordWaveSurvivors(WaveDirector director, List<List<Vector2>> paths)
        {
            foreach (var kv in _spawnedByPath)
            {
                int pathIdx = kv.Key;
                int totalWaypoints = (pathIdx < paths.Count) ? paths[pathIdx].Count : 1;

                var candidates = new List<SurvivorTemplate>();

                foreach (var (enemy, kind) in kv.Value)
                {
                    // Reached the base = IsActive false AND health was still > 0 when deactivated
                    // (health <= 0 means it was killed). We can't check health here after death,
                    // so we use waypoint index: if it reached the last waypoint it "escaped".
                    bool reachedBase = enemy.CurrentWaypointIndex >= totalWaypoints;

                    candidates.Add(new SurvivorTemplate
                    {
                        Kind = kind,
                        ResistType = enemy.ResistType,
                        HpMultiplier = 1f,
                        SpeedMultiplier = 1f,
                        BudgetCost = GetBaseCost(kind),
                        // Enemies that escaped get int.MaxValue so they always sort to top
                        WaypointReached = reachedBase ? int.MaxValue : enemy.CurrentWaypointIndex
                    });
                }

                director.RecordSurvivors(pathIdx, candidates);
            }

            _spawnedByPath.Clear();
        }

        // ------------------------------------------------------------------
        // MAIN SPAWN: reads each path's PathIntelligence and spawns a
        // composition tailored to that path's tower layout.
        // ------------------------------------------------------------------
        public void SpawnWave(
            WaveDirector director,
            List<List<Vector2>> paths,
            List<Tower> towers,
            List<Enemy> enemies,
            double currentWave)
        {
            // 1. Rebuild all element score arrays + tower profiles + strategies
            director.AnalyzeAllPaths(paths, towers);

            // 2. Rank paths (preference - risk)
            var rankedPaths = director.GetRankedPaths(paths, towers);
            int pathsToUse = Math.Min(paths.Count, rankedPaths.Count);
            float budgetPerPath = director.TotalBudget / pathsToUse;

            float baseHp = 100f * (float)Math.Pow(1.1, currentWave);
            float baseSpeed = 3f;

            // 3. Per-path spawn loop
            for (int i = 0; i < pathsToUse; i++)
            {
                int pathIdx = rankedPaths[i].Index;
                List<Vector2> path = paths[pathIdx];
                PathIntelligence intel = director.GetPathIntel(pathIdx);

                director.ApplyPathFatigue(pathIdx);

                // Init tracking list
                _spawnedByPath[pathIdx] = new List<(Enemy, SurvivorTemplate.EnemyKind)>();

                // --- Read per-path data ---
                List<DamageType> topResistances = intel.GetTopResistances(3);
                SpawnStrategy strategy = intel.Strategy;

                float spentOnPath = 0f;
                int resistCycle = 0; // rotates through topResistances

                // --- Phase A: Spawn survivors from last wave (upgraded, capped at 40% budget) ---
                spentOnPath = SpawnSurvivors(intel, path, enemies, pathIdx,
                                             baseHp, baseSpeed, budgetPerPath, spentOnPath);

                // --- Phase B: Strategy-driven fill ---
                while (spentOnPath < budgetPerPath)
                {
                    DamageType resist = topResistances[resistCycle % topResistances.Count];
                    resistCycle++;

                    spentOnPath += SpawnByStrategy(
                        strategy, path, enemies, pathIdx,
                        baseHp, baseSpeed, resist, spentOnPath, budgetPerPath);
                }
            }

            // 4. Boss every 5 waves
            if (currentWave % 5 == 0 && currentWave != 0)
            {
                int bossPathIdx = rankedPaths[0].Index;
                List<Vector2> bossPath = paths[bossPathIdx];
                PathIntelligence bi = director.GetPathIntel(bossPathIdx);

                BOSS boss = new BOSS(
                    _content.Load<Texture2D>("maleniapixel"),
                    bossPath[0], bossPath,
                    baseHp * 1.5f, baseSpeed, _tileSize);

                var bossTop = bi.GetTopResistances(2);
                boss.ResistType = bossTop[0];
                if (bossTop.Count > 1) boss.ExtraResistances.Add(bossTop[1]);

                enemies.Add(boss);
            }
        }

        // ------------------------------------------------------------------
        // Per-strategy spawn function.
        // Returns budget SPENT this call so the caller can accumulate.
        //
        // Each strategy has a distinct spawn pattern:
        //
        //  TankWall       — pure tanks, nothing else
        //  FastSwarm      — clusters of 3 fast enemies spawned together
        //  DuplicatorPush — duplicator + 2 normals behind it as chaff
        //  NormalWithTank — one tank then two normals (tank acts as shield)
        //  NormalWithFast — one fast then two normals (fast pulls aggro)
        // ------------------------------------------------------------------
        private float SpawnByStrategy(
            SpawnStrategy strategy,
            List<Vector2> path,
            List<Enemy> enemies,
            int pathIdx,
            float baseHp, float baseSpeed,
            DamageType resist,
            float spentSoFar, float budgetMax)
        {
            float spent = 0f;

            switch (strategy)
            {
                // ── TankWall ────────────────────────────────────────────────
                // Pure tanks. Slow but absorbs huge chunks of damage.
                // Randomly sprinkle one fast every 4th slot for variety.
                case SpawnStrategy.TankWall:
                    {
                        if (RandomHelper.Chance(0.25f) && CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Fast), budgetMax))
                        {
                            spent += Spawn(new fast(Tex("malikethpixel"), path[0], path, baseHp * 0.7f, baseSpeed * 2f, _tileSize),
                                           SurvivorTemplate.EnemyKind.Fast, resist, enemies, pathIdx);
                        }
                        else if (CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Tank), budgetMax))
                        {
                            spent += Spawn(new Tank(Tex("radhanpixel"), path[0], path, baseHp * 2f, baseSpeed * 0.7f, _tileSize),
                                           SurvivorTemplate.EnemyKind.Tank, resist, enemies, pathIdx);
                        }
                        break;
                    }

                // ── FastSwarm ────────────────────────────────────────────────
                // Spawn 3 fast enemies at once. They spread tower aggro and
                // individually die fast but flood through.
                case SpawnStrategy.FastSwarm:
                    {
                        int clusterSize = 3;
                        for (int c = 0; c < clusterSize; c++)
                        {
                            if (!CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Fast), budgetMax)) break;
                            spent += Spawn(new fast(Tex("malikethpixel"), path[0], path, baseHp * 0.7f, baseSpeed * 2f, _tileSize),
                                           SurvivorTemplate.EnemyKind.Fast, resist, enemies, pathIdx);
                        }
                        break;
                    }

                // ── DuplicatorPush ───────────────────────────────────────────
                // One duplicator flanked by 2 normals.
                // The normals eat early tower fire; duplicator multiplies at the back.
                case SpawnStrategy.DuplicatorPush:
                    {
                        // Two normals first (chaff)
                        for (int c = 0; c < 2; c++)
                        {
                            if (!CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Normal), budgetMax)) break;
                            spent += Spawn(new NormalEnemy(Tex("treesenpixel"), path[0], path, baseHp, baseSpeed, _tileSize),
                                           SurvivorTemplate.EnemyKind.Normal, resist, enemies, pathIdx);
                        }
                        // Duplicator behind them
                        if (CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Duplicator), budgetMax))
                            spent += Spawn(new duplicator(Tex("spawner"), path[0], path, baseHp, baseSpeed, _tileSize),
                                           SurvivorTemplate.EnemyKind.Duplicator, resist, enemies, pathIdx);
                        break;
                    }

                // ── NormalWithTank ───────────────────────────────────────────
                // 1 tank leads, 2 normals draft behind it.
                // Tank soaks the burst; normals slip through.
                case SpawnStrategy.NormalWithTank:
                    {
                        if (CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Tank), budgetMax))
                            spent += Spawn(new Tank(Tex("radhanpixel"), path[0], path, baseHp * 2f, baseSpeed * 0.7f, _tileSize),
                                           SurvivorTemplate.EnemyKind.Tank, resist, enemies, pathIdx);

                        for (int c = 0; c < 2; c++)
                        {
                            if (!CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Normal), budgetMax)) break;
                            spent += Spawn(new NormalEnemy(Tex("treesenpixel"), path[0], path, baseHp, baseSpeed, _tileSize),
                                           SurvivorTemplate.EnemyKind.Normal, resist, enemies, pathIdx);
                        }
                        break;
                    }

                // ── NormalWithFast ───────────────────────────────────────────
                // 1 fast enemy leads (draws tower fire / tests reload timing),
                // 2 normals rush in while towers reload.
                case SpawnStrategy.NormalWithFast:
                    {
                        if (CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Fast), budgetMax))
                            spent += Spawn(new fast(Tex("malikethpixel"), path[0], path, baseHp * 0.7f, baseSpeed * 2f, _tileSize),
                                           SurvivorTemplate.EnemyKind.Fast, resist, enemies, pathIdx);

                        for (int c = 0; c < 2; c++)
                        {
                            if (!CanAfford(spentSoFar + spent, GetBaseCost(SurvivorTemplate.EnemyKind.Normal), budgetMax)) break;
                            spent += Spawn(new NormalEnemy(Tex("treesenpixel"), path[0], path, baseHp, baseSpeed, _tileSize),
                                           SurvivorTemplate.EnemyKind.Normal, resist, enemies, pathIdx);
                        }
                        break;
                    }
            }

            // Safety: always spend at least 1 budget unit to prevent infinite loops
            return spent > 0 ? spent : GetBaseCost(SurvivorTemplate.EnemyKind.Normal);
        }

        // ------------------------------------------------------------------
        // Spawns survivors with a 15% HP and 5% speed upgrade.
        // Capped at 40% of path budget so survivors don't crowd out new enemies.
        // ------------------------------------------------------------------
        private float SpawnSurvivors(
            PathIntelligence intel, List<Vector2> path,
            List<Enemy> enemies, int pathIdx,
            float baseHp, float baseSpeed,
            float budgetMax, float spentSoFar)
        {
            if (intel.Survivors.Count == 0) return spentSoFar;

            const float hpBoost = 1.15f;
            const float speedBoost = 1.05f;
            float survivorBudget = budgetMax * 0.4f;

            foreach (var t in intel.Survivors)
            {
                if (spentSoFar >= survivorBudget) break;
                if (spentSoFar + t.BudgetCost > survivorBudget) break;

                float hp = baseHp * t.HpMultiplier * hpBoost;
                float speed = baseSpeed * t.SpeedMultiplier * speedBoost;

                Enemy e = t.Kind switch
                {
                    SurvivorTemplate.EnemyKind.Fast =>
                        new fast(Tex("malikethpixel"), path[0], path, hp * 0.7f, speed * 2f, _tileSize),
                    SurvivorTemplate.EnemyKind.Tank =>
                        new Tank(Tex("radhanpixel"), path[0], path, hp * 2f, speed * 0.7f, _tileSize),
                    SurvivorTemplate.EnemyKind.Duplicator =>
                        new duplicator(Tex("spawner"), path[0], path, hp, speed, _tileSize),
                    _ =>
                        new NormalEnemy(Tex("treesenpixel"), path[0], path, hp, speed, _tileSize),
                };

                e.ResistType = t.ResistType; // Keep the resistance that worked

                enemies.Add(e);
                _spawnedByPath[pathIdx].Add((e, t.Kind));
                spentSoFar += t.BudgetCost;
            }

            return spentSoFar;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        // Spawn one enemy, register it for tracking, and return its budget cost.
        private float Spawn(Enemy e, SurvivorTemplate.EnemyKind kind, DamageType resist,
                            List<Enemy> enemies, int pathIdx)
        {
            e.ResistType = resist;
            enemies.Add(e);
            _spawnedByPath[pathIdx].Add((e, kind));
            return GetBaseCost(kind);
        }

        private bool CanAfford(float spent, float cost, float max) => spent + cost <= max;

        private Texture2D Tex(string name) => _content.Load<Texture2D>(name);

        private static int GetBaseCost(SurvivorTemplate.EnemyKind kind) => kind switch
        {
            SurvivorTemplate.EnemyKind.Tank => 25,
            SurvivorTemplate.EnemyKind.Duplicator => 20,
            SurvivorTemplate.EnemyKind.Fast => 15,
            _ => 15  // Normal
        };

        public void Update(GameTime gameTime) { }

        public static DamageType GetCounterElement(DamageType enemyResist)
        {
            return enemyResist switch
            {
                DamageType.Ice => DamageType.Fire,
                DamageType.Fire => DamageType.Water,
                DamageType.Earth => DamageType.Ice,
                DamageType.Dark => DamageType.Holy,
                DamageType.Holy => DamageType.Dark,
                DamageType.Knight => DamageType.Magic,
                DamageType.Physical => DamageType.Bleed,
                _ => DamageType.Physical
            };
        }
    }
}
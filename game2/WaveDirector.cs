using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static game2.TowerManager;

namespace game2
{
    public struct PathRank
    {
        public int Index;
        public float Fitness;
    }

    // -------------------------------------------------------------------------
    // What composition strategy to use on a specific path this wave.
    // Derived from the tower makeup covering that path.
    // -------------------------------------------------------------------------
    public enum SpawnStrategy
    {
        TankWall,       // Slow-fire towers  -> tanks absorb the wide gaps between shots
        FastSwarm,      // Short-range fast  -> fast enemies sprint past the kill zone
        DuplicatorPush, // Balanced coverage -> duplicators multiply past the supply
        NormalWithTank, // Tank up front + normals drafting behind its HP
        NormalWithFast, // Fast scouts pull tower aggro, normals rush through reloads
    }

    // -------------------------------------------------------------------------
    // Snapshot of the tower makeup covering one path.
    // Built alongside ElementScores in RebuildScores().
    // -------------------------------------------------------------------------
    public class TowerProfile
    {
        public float AverageCooldown = 1f;  // High = slow firing = tanks survive longer
        public float TotalDps = 0f;  // Sum of (1/cooldown) = raw lethality
        public float AverageRange = 0f;  // High = snipers = fast enemies die early
        public int TowerCount = 0;

        // Helpers used by DeriveStrategy
        public bool IsSlowFire => AverageCooldown > 0.8f;
        public bool IsShortRange => AverageRange < 150f;
    }

    // -------------------------------------------------------------------------
    // Snapshot of one enemy worth re-using next wave.
    // -------------------------------------------------------------------------
    public class SurvivorTemplate
    {
        public enum EnemyKind { Normal, Fast, Tank, Duplicator }

        public EnemyKind Kind;
        public DamageType ResistType;
        public float HpMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public int BudgetCost;

        // How far did this enemy get before the wave ended?
        // Enemies that reached the base are given int.MaxValue so they sort to the top.
        public int WaypointReached;
    }

    // -------------------------------------------------------------------------
    // All per-path intelligence lives here.
    // One instance per path, owned by WaveDirector.
    // -------------------------------------------------------------------------
    public class PathIntelligence
    {
        public int PathIndex;

        // ── Element Score Array ──────────────────────────────────────────────
        // Key   = DamageType
        // Value = accumulated threat: sum over all covering towers of
        //         (1/cooldown) * (range/100) * DamageChart.GetMultiplier(tower.type, element)
        //
        // HIGH score  = towers hit this resistance type very hard
        //             = great resistance for enemies to have (GetTopResistances)
        // LOW  score  = towers barely threaten this type
        public Dictionary<DamageType, float> ElementScores = new Dictionary<DamageType, float>();

        // Sorted snapshot rebuilt each wave (ascending score, so index 0 = weakest)
        public List<(DamageType Element, float Score)> SortedScores = new List<(DamageType, float)>();

        // ── Tower makeup on this path ────────────────────────────────────────
        public TowerProfile TowerProfile = new TowerProfile();

        // ── Derived spawn strategy for the coming wave ───────────────────────
        public SpawnStrategy Strategy = SpawnStrategy.NormalWithTank;

        // ── Survivor memory (up to 3 templates carried from last wave) ───────
        public List<SurvivorTemplate> Survivors = new List<SurvivorTemplate>();

        // ── Fatigue weight (path preference) ────────────────────────────────
        public float Preference = 100f;

        public PathIntelligence(int index)
        {
            PathIndex = index;
            foreach (DamageType t in Enum.GetValues(typeof(DamageType)))
                ElementScores[t] = 0f;
        }

        // ── Main rebuild: element scores + tower profile in one tower loop ───
        public void RebuildScores(List<Vector2> path, List<Tower> towers)
        {
            // Reset element scores
            foreach (DamageType t in Enum.GetValues(typeof(DamageType)))
                ElementScores[t] = 0f;

            // Reset profile
            TowerProfile = new TowerProfile();
            float totalCooldown = 0f;
            float totalRange = 0f;

            foreach (var tower in towers)
            {
                // Does this tower cover any point on the path?
                float rangeSq = tower.Range * tower.Range;
                bool canHit = false;
                for (int i = 0; i < path.Count; i += 5)
                {
                    if (Vector2.DistanceSquared(path[i], tower.Position) <= rangeSq)
                    { canHit = true; break; }
                }
                if (!canHit) continue;

                // Accumulate profile data
                TowerProfile.TowerCount++;
                totalCooldown += tower.Cooldown;
                totalRange += tower.Range;
                TowerProfile.TotalDps += 1.0f / tower.Cooldown;

                // Accumulate element scores
                // baseRisk = DPS weight × reach weight (same formula for all tower types)
                float baseRisk = (1.0f / tower.Cooldown) * (tower.Range / 100f);
                foreach (DamageType element in Enum.GetValues(typeof(DamageType)))
                {
                    float mult = DamageChart.GetMultiplier(tower.DamageType, element);
                    ElementScores[element] += baseRisk * mult;
                }
            }

            // Finalize averages
            if (TowerProfile.TowerCount > 0)
            {
                TowerProfile.AverageCooldown = totalCooldown / TowerProfile.TowerCount;
                TowerProfile.AverageRange = totalRange / TowerProfile.TowerCount;
            }

            // Sort element scores ascending (cheapest resistance at [0])
            SortedScores = ElementScores
                .OrderBy(kv => kv.Value)
                .Select(kv => (kv.Key, kv.Value))
                .ToList();

            // Derive strategy from profile
            Strategy = DeriveStrategy();
        }

        // ── Picks composition strategy from tower profile ────────────────────
        //
        // Grid:
        //   SlowFire + LongRange  → TankWall    (wide shot gaps, big HP soaks shots)
        //   SlowFire + ShortRange → FastSwarm   (sprint past the short kill zone)
        //   FastFire + ShortRange → DuplicatorPush (overwhelm the fast cluster)
        //   FastFire + LongRange  → NormalWithFast (spread aggro, normals draft behind)
        //   No towers             → NormalWithTank (safe default)
        //
        // A random nudge (1-in-5) swaps to an adjacent strategy so the same
        // tower layout doesn't always produce identical waves.
        private SpawnStrategy DeriveStrategy()
        {
            if (TowerProfile.TowerCount == 0) return SpawnStrategy.NormalWithTank;

            bool slow = TowerProfile.IsSlowFire;
            bool close = TowerProfile.IsShortRange;
            bool nudge = RandomHelper.Chance(0.2f); // 20% deviation

            if (slow && !close)
                return nudge ? SpawnStrategy.NormalWithFast : SpawnStrategy.TankWall;

            if (slow && close)
                return nudge ? SpawnStrategy.DuplicatorPush : SpawnStrategy.FastSwarm;

            if (!slow && close)
                return nudge ? SpawnStrategy.FastSwarm : SpawnStrategy.DuplicatorPush;

            // FastFire + LongRange — toughest for enemies, go mixed
            return nudge ? SpawnStrategy.NormalWithTank : SpawnStrategy.NormalWithFast;
        }

        // Returns the 'count' best resistances (highest element scores = most dangerous towers)
        public List<DamageType> GetTopResistances(int count)
        {
            var top = SortedScores.TakeLast(count).Select(p => p.Element).ToList();
            if (top.Count == 0) top.Add(DamageType.Physical);
            return top;
        }
    }

    // -------------------------------------------------------------------------
    // WaveDirector: owns one PathIntelligence per path
    // -------------------------------------------------------------------------
    public class WaveDirector
    {
        public float TotalBudget = 60f;

        private Dictionary<int, PathIntelligence> _pathData = new Dictionary<int, PathIntelligence>();

        public WaveDirector(int pathCount)
        {
            for (int i = 0; i < pathCount; i++)
                _pathData[i] = new PathIntelligence(i);
        }

        public void AnalyzeAllPaths(List<List<Vector2>> paths, List<Tower> towers)
        {
            for (int i = 0; i < paths.Count; i++)
                if (_pathData.ContainsKey(i))
                    _pathData[i].RebuildScores(paths[i], towers);
        }

        public PathIntelligence GetPathIntel(int pathIndex)
            => _pathData.ContainsKey(pathIndex) ? _pathData[pathIndex] : null;

        public float CalculatePathRisk(List<Vector2> path, List<Tower> towers)
        {
            float risk = 0;
            foreach (var tower in towers)
            {
                float rangeSq = tower.Range * tower.Range;
                bool canHit = false;
                for (int i = 0; i < path.Count; i += 5)
                    if (Vector2.DistanceSquared(path[i], tower.Position) <= rangeSq)
                    { canHit = true; break; }

                if (canHit)
                    risk += (1.0f / tower.Cooldown) * (tower.Range / 100f);
            }
            return risk;
        }

        public List<PathRank> GetRankedPaths(List<List<Vector2>> paths, List<Tower> towers)
        {
            var list = new List<PathRank>();
            for (int i = 0; i < paths.Count; i++)
            {
                float fitness = _pathData[i].Preference - CalculatePathRisk(paths[i], towers);
                list.Add(new PathRank { Index = i, Fitness = fitness });
            }
            list.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
            return list;
        }

        // ------------------------------------------------------------------
        // Record up to 3 survivor templates for a path.
        //
        // Sorting priority:
        //   1. Enemies that reached the base (WaypointReached == int.MaxValue) — first
        //   2. Among the rest: highest WaypointReached (went furthest)
        //   3. Tiebreak: cheapest budget cost (most efficient enemy)
        //
        // This means: if NOBODY reached the base, we still keep the 3 enemies
        // that got closest — they are the most promising templates.
        // ------------------------------------------------------------------
        public void RecordSurvivors(int pathIndex, List<SurvivorTemplate> candidates)
        {
            if (!_pathData.ContainsKey(pathIndex)) return;

            candidates.Sort((a, b) =>
            {
                int cmp = b.WaypointReached.CompareTo(a.WaypointReached); // furthest first
                if (cmp != 0) return cmp;
                return a.BudgetCost.CompareTo(b.BudgetCost);              // cheapest tiebreak
            });

            _pathData[pathIndex].Survivors = candidates.Take(3).ToList();
        }

        public void ClearSurvivors(int pathIndex)
        {
            if (_pathData.ContainsKey(pathIndex))
                _pathData[pathIndex].Survivors.Clear();
        }

        public void ApplyPathFatigue(int pathIdx)
        {
            if (_pathData.ContainsKey(pathIdx))
                _pathData[pathIdx].Preference -= 5f;
        }

        public void Evolve(bool enemyReachedBase)
        {
            TotalBudget *= enemyReachedBase ? 1.05f : 1.15f;
        }

        // Legacy wrapper for BOSS logic in EnemySpawner
        public (int pathIdx, DamageType element) GetBestStrategy(List<List<Vector2>> paths, List<Tower> towers)
        {
            var ranked = GetRankedPaths(paths, towers);
            int bestPath = ranked[0].Index;
            var intel = GetPathIntel(bestPath);
            var topRes = intel?.GetTopResistances(1);
            DamageType elem = (topRes != null && topRes.Count > 0) ? topRes[0] : DamageType.Physical;
            ApplyPathFatigue(bestPath);
            return (bestPath, elem);
        }
    }
}
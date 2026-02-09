using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using static game2.TowerManager;

namespace game2
{
    public struct PathRank
    {
        public int Index;
        public float Fitness;
    }

    public class WaveDirector
    {
        public float TotalBudget = 60f; // Starting budget (Wave 1)

        // The "Notebook" of preferences
        private Dictionary<int, float> _pathPreference = new Dictionary<int, float>();
        private Dictionary<DamageType, float> _elementPreference = new Dictionary<DamageType, float>();

        public WaveDirector(int pathCount)
        {
            // Initialize preferences to 100
            for (int i = 0; i < pathCount; i++) _pathPreference[i] = 100f;
            foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
                _elementPreference[type] = 100f;
        }

        // RISK ASSESSMENT: Calculates how dangerous a path is
        public float CalculatePathRisk(List<Vector2> path, List<Tower> towers)
        {
            float risk = 0;
            foreach (var tower in towers)
            {
                float rangeSq = tower.Range * tower.Range;
                bool canHit = false;

                // Optimization: Check every 5th point to save CPU time
                for (int i = 0; i < path.Count; i += 5)
                {
                    if (Vector2.DistanceSquared(path[i], tower.Position) <= rangeSq)
                    {
                        canHit = true;
                        break;
                    }
                }

                if (canHit)
                {
                    // High DPS (low cooldown) adds more risk
                    risk += (1.0f / tower.Cooldown);
                    // Long range adds risk
                    risk += (tower.Range / 100f);
                }
            }
            return risk;
        }

        public List<PathRank> GetRankedPaths(List<List<Vector2>> paths, List<Tower> towers)
        {
            List<PathRank> rankings = new List<PathRank>();

            for (int i = 0; i < paths.Count; i++)
            {
                float risk = CalculatePathRisk(paths[i], towers);

                // Fitness = Preference - Risk
                float fitness = _pathPreference[i] - risk;

                rankings.Add(new PathRank { Index = i, Fitness = fitness });
            }

            // Sort by Fitness descending (Highest score at index 0)
            rankings.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));

            return rankings;
        }

        // NEW METHOD: Calculates the best element SPECIFICALLY for a single path
        public DamageType GetBestElementForPath(List<Vector2> path, List<Tower> towers)
        {
            DamageType bestElement = DamageType.Physical;
            float highestScore = float.MinValue;

            foreach (DamageType element in Enum.GetValues(typeof(DamageType)))
            {
                if (element == DamageType.Physical) continue;

                // Calculate Elemental Advantage (Countering towers)
                float counterBonus = 0;
                foreach (var t in towers)
                {
                    // If this element resists the tower (multiplier < 1), it is a good choice
                    if (DamageChart.GetMultiplier(t.DamageType, element) < 1.0f)
                        counterBonus += 2f;
                }

                // Score = Preference + Bonus
                // (We don't subtract Risk here because Risk is identical for all elements on the same path)
                float score = _elementPreference[element] + counterBonus;

                if (score > highestScore)
                {
                    highestScore = score;
                    bestElement = element;
                }
            }

            // FATIGUE: Reduce preference so we don't spam the same element every time
            _elementPreference[bestElement] -= 2f;

            return bestElement;
        }

        // EVOLUTION: Adjust budget based on player performance
        public void Evolve(bool enemyReachedBase)
        {
            if (!enemyReachedBase)
            {
                // Player won easily -> Increase difficulty by 15%
                TotalBudget *= 1.15f;
            }
            else
            {
                // Player struggled -> Increase difficulty by only 5%
                TotalBudget *= 1.05f;
            }
        }

        public void ApplyPathFatigue(int pathIdx)
        {
            if (_pathPreference.ContainsKey(pathIdx))
                _pathPreference[pathIdx] -= 5f;
        }

        // Legacy support: Wraps the new methods to provide a single strategy
        public (int pathIdx, DamageType element) GetBestStrategy(List<List<Vector2>> paths, List<Tower> towers)
        {
            var ranked = GetRankedPaths(paths, towers);
            int bestPath = ranked[0].Index;

            // Use the new helper to find the element for this specific path
            DamageType bestElement = GetBestElementForPath(paths[bestPath], towers);

            // Apply fatigue manually since we selected this path
            ApplyPathFatigue(bestPath);

            return (bestPath, bestElement);
        }
    }
}
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using static game2.TowerManager;

namespace game2
{
    public class WaveDirector
    {
        public float TotalBudget = 100f; // Starting budget (Wave 1)

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
        // We use DistanceSquared to make it run faster (optimization)
        public float CalculatePathRisk(List<Vector2> path, List<Tower> towers)
        {
            float risk = 0;
            foreach (var tower in towers)
            {
                float rangeSq = tower.Range * tower.Range;
                bool canHit = false;

                // Check every 5th point to save CPU time
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

        // SELECTION: Pick the best Strategy (Path + Element)
        public (int pathIdx, DamageType element) GetBestStrategy(List<List<Vector2>> paths, List<Tower> towers)
        {
            int bestPath = 0;
            DamageType bestElement = DamageType.Physical;
            float highestFitness = float.MinValue;

            for (int i = 0; i < paths.Count; i++)
            {
                float risk = CalculatePathRisk(paths[i], towers);

                foreach (DamageType element in Enum.GetValues(typeof(DamageType)))
                {
                    if (element == DamageType.Physical) continue;

                    // Calculate Elemental Advantage (Countering towers)
                    float counterBonus = 0;
                    foreach (var t in towers)
                    {
                        // If this element resists the tower, add bonus
                        if (DamageChart.GetMultiplier(t.DamageType, element) < 1.0f)
                            counterBonus += 2f;
                    }

                    // FITNESS FORMULA: Preference + Counter Bonus - Risk
                    float fitness = _pathPreference[i] + _elementPreference[element] + counterBonus - risk;

                    if (fitness > highestFitness)
                    {
                        highestFitness = fitness;
                        bestPath = i;
                        bestElement = element;
                    }
                }
            }

            // FATIGUE: Lower the score for next time so AI varies its attacks
            _pathPreference[bestPath] -= 5f;
            _elementPreference[bestElement] -= 5f;

            return (bestPath, bestElement);
        }
    }
}
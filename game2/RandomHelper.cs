using System;

namespace game2
{
    public static class RandomHelper
    {
        private static Random _random = new Random();

        public static int GetInt(int min, int max) => _random.Next(min, max);
        public static float GetFloat(float min, float max) => (float)(_random.NextDouble() * (max - min) + min);
        public static bool Chance(float probability) => _random.NextDouble() < probability;
    }
}
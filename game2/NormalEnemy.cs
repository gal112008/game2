using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class NormalEnemy : Enemy
    {
        public NormalEnemy(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float health, float speed, int size)
            : base(texture, startPosition, waypoints, health, speed, size, 15)
        {
        }
    }
}
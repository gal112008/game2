using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class Tank : Enemy
    {
        public Tank(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float hp, float speed)
            : base(texture, startPosition, waypoints, hp *RandomHelper.GetFloat(3f,6f), speed * RandomHelper.GetFloat(0.3f,0.7f))
        {
            // Radahn has 5x Health but moves at half speed
        }
    }
}
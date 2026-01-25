using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class Tank : Enemy
    {
        public Tank(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float hp, float speed, int size)
            : base(texture, startPosition, waypoints, hp * RandomHelper.GetFloat(3f, 6f), speed * RandomHelper.GetFloat(0.3f, 0.7f), size)
        {
            // Radahn has massive Health but moves slow. 
            // We pass 'size' into the base() call so the Enemy class knows how big to draw him.
        }
    }
}
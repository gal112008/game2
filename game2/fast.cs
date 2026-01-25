using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class fast : Enemy
    {
        public fast(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float hp, float speed)
            : base(texture, startPosition, waypoints, hp * RandomHelper.GetFloat(0.4f,0.8f), speed * RandomHelper.GetFloat(1.5f, 2.7f))
        {
            // Maliketh is very fragile but extremely fast
        }
    }
}
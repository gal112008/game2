using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class BOSS : Enemy
    {
        private float _teleportTimer = 0f;
        private float _didTeleport = 0f;

        public BOSS(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float hp, float speed, int size)
            : base(texture, startPosition, waypoints, hp * 10f, speed * 1.1f, size,100)
        {
        }

        public override void Update(GameTime gameTime)
        {
            _teleportTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_teleportTimer <= 0 && RandomHelper.Chance(0.01f) && _didTeleport == 0f)
            {
                if (CurrentWaypointIndex < _waypoints.Count - 1)
                {
                    CurrentWaypointIndex++;
                    Position = _waypoints[CurrentWaypointIndex];
                    _teleportTimer = 2.0f;
                    _didTeleport = 1f;
                }
            }

            base.Update(gameTime);
        }
    }
}
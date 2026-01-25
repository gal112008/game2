using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace game2
{
    public class BOSS : Enemy
    {
        private float _teleportTimer = 0f;
        private float _didTeleport = 0f;
        public BOSS(Texture2D texture, Vector2 startPosition, List<Vector2> waypoints, float hp, float speed)
            : base(texture, startPosition, waypoints, hp * 10f, speed * 1.1f)
        {
            // Malenia has massive HP
        }

        public override void Update(GameTime gameTime)
        {
            _teleportTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Use the random helper for a 1% chance to teleport every frame if cooldown is over
            if (_teleportTimer <= 0 && RandomHelper.Chance(0.01f)&&_didTeleport==0f)
            {
                if (_currentWaypointIndex < _waypoints.Count - 1)
                {
                    _currentWaypointIndex++;
                    Position = _waypoints[_currentWaypointIndex];
                    _teleportTimer = 2.0f; // Cooldown of 2 seconds
                    _didTeleport = 1f;
                }
            }

            base.Update(gameTime);
        }
    }
}
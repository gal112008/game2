using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace game2
{
    public class Bullet
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Enemy Target;
        public float Speed = 12f;
        public int Damage = 25;
        public bool IsActive = true;

        public TowerManager.DamageType Type;

        public Bullet(Texture2D texture, Vector2 position, Enemy target)
        {
            Texture = texture;
            Position = position;
            Target = target;
        }

        public void Update()
        {
            // If target is gone or dead, deactivate the bullet
            if (Target == null || !Target.IsActive)
            {
                IsActive = false;
                return;
            }

            Vector2 dir = Target.Position - Position;

            // Check if bullet reached the target
            if (dir.Length() < Speed)
            {
                // Apply damage using the elemental system in Enemy.cs
                Target.TakeDamage(Damage, Type);
                IsActive = false;
            }
            else
            {
                dir.Normalize();
                Position += dir * Speed;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            // Draw bullet centered on its position
            sb.Draw(Texture, Position, null, Color.Yellow, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }
}
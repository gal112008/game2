using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static game2.TowerManager;

namespace game2
{
    public class Bullet
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Enemy Target;
        public float Speed = 10f;
        public int Damage = 25;
        public bool IsActive = true;
        public DamageType Type;

        public Bullet(Texture2D texture, Vector2 position, Enemy target)
        {
            Texture = texture;
            Position = position;
            Target = target;
        }

        public void Update()
        {
            if (Target == null || !Target.IsActive)
            {
                IsActive = false;
                return;
            }

            Vector2 direction = Target.Position - Position;
            float distance = direction.Length();

            if (distance < Speed)
            {
                // Use the new TakeDamage method instead of subtracting health directly
                Target.TakeDamage(Damage, Type);
                IsActive = false;
            }
            else
            {
                direction.Normalize();
                Position += direction * Speed;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive)
            {
                // Use a small scale or color for the bullet since we don't have a specific bullet sprite
                // We will use a 1x1 white pixel created in Game1 for this usually, or one of the assets scaled down
                spriteBatch.Draw(Texture, Position, null, Color.Yellow, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            }
        }
    }
}
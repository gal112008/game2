using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System;

namespace game2
{
    public enum TowerType { Basic, Sniper, FastFire }

    public class TowerManager
    {
        private List<Tower> _towers;
        private Dictionary<TowerType, Texture2D> _towerTextures;
        private Texture2D _bulletTexture;
        private int _tileSize;
        public Dictionary<DamageType, Texture2D> TypeIcons = new Dictionary<DamageType, Texture2D>();

        public TowerManager(int tileSize)
        {
            _towers = new List<Tower>();
            _towerTextures = new Dictionary<TowerType, Texture2D>();
            _tileSize = tileSize;
        }

        // The full list of elements
        public enum DamageType { Physical, Fire, Ice, Water, Earth, Rot, Dark, Holy, Bleed, Magic, Knight }

        public int GetCost(TowerType type)
        {
            return type switch { TowerType.Sniper => 150, TowerType.FastFire => 75, _ => 50 };
        }

        public void LoadContent(ContentManager content, Texture2D pixel)
        {
            _towerTextures[TowerType.Basic] = content.Load<Texture2D>("itai");
            _towerTextures[TowerType.Sniper] = content.Load<Texture2D>("ado_normal");
            _towerTextures[TowerType.FastFire] = content.Load<Texture2D>("ado_burn");

            // Loading your custom icons
            foreach (DamageType t in Enum.GetValues(typeof(DamageType)))
            {
                if (t == DamageType.Physical) continue;
                TypeIcons[t] = content.Load<Texture2D>("type_" + t.ToString().ToLower());
            }

            _bulletTexture = pixel;
        }

        public static class DamageChart
        {
            public static float GetMultiplier(DamageType atk, DamageType res)
            {
                if (atk == res) return 0.5f; // Resistant to own type

                return (atk, res) switch
                {
                    (DamageType.Fire, DamageType.Ice) => 2.0f,
                    (DamageType.Water, DamageType.Fire) => 2.0f,
                    (DamageType.Ice, DamageType.Earth) => 2.0f,
                    (DamageType.Holy, DamageType.Dark) => 2.0f,
                    (DamageType.Dark, DamageType.Holy) => 2.0f,
                    (DamageType.Magic, DamageType.Knight) => 1.5f,
                    (DamageType.Bleed, DamageType.Physical) => 1.5f,
                    _ => 1.0f
                };
            }
        }

        public bool CreateTower(TowerType type, Vector2 position, ref int currentGold)
        {
            int cost = GetCost(type);
            if (currentGold >= cost)
            {
                Tower newTower = new Tower(_towerTextures[type], _bulletTexture, position, _tileSize);

                // RANDOMIZE TOWER ELEMENT ON PLACEMENT
                Array types = Enum.GetValues(typeof(DamageType));
                newTower.DamageType = (DamageType)types.GetValue(RandomHelper.GetInt(1, types.Length));

                // Specific stats based on tower model
                if (type == TowerType.Sniper) { newTower.Range = 400f; newTower.Cooldown = 1.5f; }
                if (type == TowerType.FastFire) { newTower.Range = 120f; newTower.Cooldown = 0.15f; }

                _towers.Add(newTower);
                currentGold -= cost;
                return true;
            }
            return false;
        }

        public void Update(GameTime gameTime, QuadTree spatialIndex, List<Bullet> bullets)
        {
            foreach (var tower in _towers) tower.Update(gameTime, spatialIndex, bullets);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var tower in _towers) tower.Draw(spriteBatch, TypeIcons);
        }
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace game2
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private MapGenerator _mapGen;
        private EnemySpawner _spawner;
        private Texture2D _towerTexture, _pixel;
        private SpriteFont _gameFont;
        private List<Enemy> _enemies = new List<Enemy>();
        private List<Tower> _towers = new List<Tower>();
        private List<Bullet> _bullets = new List<Bullet>();
        private double _currentWave = 0, _waveEndCount = 0;
        private MouseState _previousMouseState;
        private const int TileSize = 48;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
        }

        protected override void Initialize()
        {
            _mapGen = new MapGenerator(1024, 768, TileSize);
            _mapGen.Generate();

            // Offset waypoints for ALL paths
            foreach (var path in _mapGen.Paths)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    path[i] = new Vector2(path[i].X + (TileSize / 2), path[i].Y + (TileSize / 2));
                }
            }

            _spawner = new EnemySpawner(Content, TileSize);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _towerTexture = Content.Load<Texture2D>("itai");
            _gameFont = Content.Load<SpriteFont>("File");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
            HandleInput();
            _spawner.Update(gameTime, _currentWave, _mapGen.Paths, _enemies);
            UpdateEntities(gameTime);
            base.Update(gameTime);
        }

        private void HandleInput()
        {
            MouseState currentMouse = Mouse.GetState();
            if (currentMouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                int gX = currentMouse.X / TileSize;
                int gY = currentMouse.Y / TileSize;
                if (gX >= 0 && gX < _mapGen.GridMap.GetLength(0) && gY >= 0 && gY < _mapGen.GridMap.GetLength(1))
                {
                    if (_mapGen.GridMap[gX, gY] == 0)
                    {
                        Vector2 snapPos = new Vector2(gX * TileSize + (TileSize / 2), gY * TileSize + (TileSize / 2));
                        _towers.Add(new Tower(_towerTexture, _pixel, snapPos, TileSize));
                        _mapGen.GridMap[gX, gY] = 2;
                    }
                }
            }
            _previousMouseState = currentMouse;
        }

        private void UpdateEntities(GameTime gameTime)
        {
            QuadTree spatialIndex = new QuadTree(new Rectangle(0, 0, 1024, 768));
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                _enemies[i].Update(gameTime);
                if (!_enemies[i].IsActive) { _enemies.RemoveAt(i); _waveEndCount++; }
                else spatialIndex.Insert(_enemies[i]);
            }
            foreach (var tower in _towers) tower.Update(gameTime, spatialIndex, _bullets);
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update();
                if (!_bullets[i].IsActive) _bullets.RemoveAt(i);
            }
            if (_waveEndCount >= 5) { _currentWave++; _waveEndCount = 0; }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            for (int x = 0; x < _mapGen.GridMap.GetLength(0); x++)
            {
                for (int y = 0; y < _mapGen.GridMap.GetLength(1); y++)
                {
                    Rectangle rect = new Rectangle(x * TileSize, y * TileSize, TileSize - 1, TileSize - 1);
                    Color tileColor = Color.ForestGreen;

                    // Different colors for the two paths
                    if (_mapGen.GridMap[x, y] == 1) tileColor = Color.Gray;
                    else if (_mapGen.GridMap[x, y] == 3) tileColor = Color.SlateGray;

                    _spriteBatch.Draw(_pixel, rect, tileColor * 0.5f);
                }
            }

            foreach (var t in _towers) t.Draw(_spriteBatch);
            foreach (var e in _enemies) e.Draw(_spriteBatch, _gameFont);
            foreach (var b in _bullets) b.Draw(_spriteBatch);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
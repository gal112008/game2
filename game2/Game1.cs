using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace game2
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Grid Logic
        private int[,] _gridMap;
        private int _cols, _rows;
        private const int TileSize = 64;
        // 0 = Empty, 1 = Path, 2 = Tower

        // Textures & Assets
        private Texture2D _towerTexture, _pixel;
        private SpriteFont _gameFont;
        private List<Enemy> _enemies = new List<Enemy>();
        private List<Tower> _towers = new List<Tower>();
        private List<Bullet> _bullets = new List<Bullet>();
        private List<Vector2> _path = new List<Vector2>();

        // Spawning & Waves
        private float _spawnTimer = 0f;
        private float _spawnInterval = 2f;
        private double currentwave = 0, waveend = 0;

        private MouseState _previousMouseState;

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
            _cols = _graphics.PreferredBackBufferWidth / TileSize;
            _rows = _graphics.PreferredBackBufferHeight / TileSize;
            _gridMap = new int[_cols, _rows];

            // --- RANDOM MAP GENERATION ---
            GenerateRandomPath();
            MarkGridPath();

            base.Initialize();
        }


        private void GenerateRandomPath()
        {
            _path.Clear();
            Random rand = new Random();

            // 1. Randomize complexity
            int totalTurns = rand.Next(12, 25);

            // Start at a random row on the left edge
            int currentX = 0;
            int currentY = rand.Next(4, _rows - 4);
            _path.Add(new Vector2(currentX * TileSize, currentY * TileSize));

            // --- REQUIREMENT: First 2 tiles must go Right ---
            currentX += 2;
            _path.Add(new Vector2(currentX * TileSize, currentY * TileSize));

            int lastDir = 0; // 0:R, 1:U, 2:D, 3:L

            for (int i = 0; i < totalTurns; i++)
            {
                int dir = rand.Next(0, 4);

                // --- PREVENT REPEATING THE SAME TRACK ---
                if ((lastDir == 0 && dir == 3) || (lastDir == 3 && dir == 0) ||
                    (lastDir == 1 && dir == 2) || (lastDir == 2 && dir == 1))
                {
                    dir = 0;
                }

                // --- REQUIREMENT: SPACING GAP (Left, Right, Up, Down) ---
                // We force a minimum length of 3 for EVERY direction.
                // This ensures no path is ever directly touching another row or column.
                int length = rand.Next(3, 7);

                for (int j = 0; j < length; j++)
                {
                    if (dir == 0) currentX++;
                    else if (dir == 1) currentY--;
                    else if (dir == 2) currentY++;
                    else if (dir == 3) currentX--;

                    // Stay within screen boundaries with a 3-tile safety buffer
                    currentX = Math.Clamp(currentX, 0, _cols - 1);
                    currentY = Math.Clamp(currentY, 3, _rows - 4);
                }

                _path.Add(new Vector2(currentX * TileSize, currentY * TileSize));
                lastDir = dir;
            }

            // 5. Final Step: Clear exit to the right
            _path.Add(new Vector2(_cols * TileSize, currentY * TileSize));
        }


        private void MarkGridPath()
        {
            for (int i = 0; i < _path.Count - 1; i++)
            {
                int startX = (int)_path[i].X / TileSize;
                int startY = (int)_path[i].Y / TileSize;
                int endX = (int)_path[i + 1].X / TileSize;
                int endY = (int)_path[i + 1].Y / TileSize;

                for (int x = Math.Min(startX, endX); x <= Math.Max(startX, endX); x++)
                    for (int y = Math.Min(startY, endY); y <= Math.Max(startY, endY); y++)
                        if (x >= 0 && x < _cols && y >= 0 && y < _rows)
                            _gridMap[x, y] = 1;
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Note: Ensure these assets exist in your Content Pipeline
            _towerTexture = Content.Load<Texture2D>("itai");
            _gameFont = Content.Load<SpriteFont>("File");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            MouseState currentMouse = Mouse.GetState();

            // 1. Tower Placement Logic
            if (currentMouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                int gX = currentMouse.X / TileSize;
                int gY = currentMouse.Y / TileSize;

                if (gX >= 0 && gX < _cols && gY >= 0 && gY < _rows && _gridMap[gX, gY] == 0)
                {
                    Vector2 snapPos = new Vector2(gX * TileSize + 32, gY * TileSize + 32);
                    _towers.Add(new Tower(_towerTexture, _pixel, snapPos));
                    _gridMap[gX, gY] = 2; // Mark as Tower
                }
            }
            _previousMouseState = currentMouse;

            // 2. Enemy Spawning Logic
            UpdateSpawning(gameTime);

            // 3. QuadTree Rebuild & Enemy Updates
            QuadTree spatialIndex = new QuadTree(new Rectangle(0, 0, 1024, 768));
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                _enemies[i].Update(gameTime);
                if (!_enemies[i].IsActive)
                {
                    _enemies.RemoveAt(i);
                    waveend++;
                }
                else
                {
                    spatialIndex.Insert(_enemies[i]);
                }
            }

            // 4. Tower & Bullet Updates
            foreach (var tower in _towers) tower.Update(gameTime, spatialIndex, _bullets);

            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update();
                if (!_bullets[i].IsActive) _bullets.RemoveAt(i);
            }

            if (waveend >= 5) { currentwave++; waveend = 0; }

            base.Update(gameTime);
        }

        private void UpdateSpawning(GameTime gameTime)
        {
            _spawnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_spawnTimer <= 0)
            {
                float hp = 100f * (float)Math.Pow(1.1, currentwave);
                float spd = 3f;
                int roll = RandomHelper.GetInt(1, 101);

                // Use inheritance to spawn different types
                Enemy e = roll <= 10 ? new BOSS(Content.Load<Texture2D>("maleniapixel"), _path[0], _path, hp, spd) :
                          roll <= 30 ? new Tank(Content.Load<Texture2D>("radhanpixel"), _path[0], _path, hp, spd) :
                          new Enemy(Content.Load<Texture2D>("treesenpixel"), _path[0], _path, hp, spd);

                _enemies.Add(e);
                _spawnTimer = _spawnInterval;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);
            _spriteBatch.Begin();

            // Draw Background Grid
            for (int x = 0; x < _cols; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    Rectangle rect = new Rectangle(x * TileSize, y * TileSize, TileSize - 1, TileSize - 1);
                    Color tileColor = _gridMap[x, y] == 1 ? Color.Gray : Color.ForestGreen;
                    _spriteBatch.Draw(_pixel, rect, tileColor * 0.5f);
                }
            }

            // Draw All Entities
            foreach (var t in _towers) t.Draw(_spriteBatch);
            foreach (var e in _enemies) e.Draw(_spriteBatch, _gameFont);
            foreach (var b in _bullets) b.Draw(_spriteBatch);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
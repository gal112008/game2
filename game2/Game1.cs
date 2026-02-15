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

        // --- NEW VARIABLES ---
        private WaveDirector _waveDirector;// Manages wave strategies and evolution
        private bool _enemiesReachedBaseThisWave = false;

        private MapGenerator _mapGen;
        private EnemySpawner _spawner;
        private Texture2D _towerTexture, _pixel;
        private SpriteFont _gameFont;
        private List<Enemy> _enemies = new List<Enemy>();
        private TowerManager _towerManager;
        private List<Bullet> _bullets = new List<Bullet>();

        private double _currentWave = 1;
        private float _waveDelayTimer = 0; // Delay between waves

        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;
        private const int TileSize = 48;
        int gold = 400;
        private TowerType _selectedType = TowerType.Basic;
        private int hp = 5;

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
            _towerManager = new TowerManager(TileSize);

            foreach (var path in _mapGen.Paths)
            {
                for (int i = 0; i < path.Count; i++)
                    path[i] = new Vector2(path[i].X + (TileSize / 2), path[i].Y + (TileSize / 2));
            }

            // --- INIT WAVE DIRECTOR ---
            _waveDirector = new WaveDirector(_mapGen.Paths.Count);
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
            _towerManager.LoadContent(Content, _pixel);

            // Spawn first wave immediately
            _spawner.SpawnWave(_waveDirector, _mapGen.Paths, _towerManager.GetTowers(), _enemies, _currentWave);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
            HandleInput();

            // Check for game over
            if (hp <= 0) return;

            UpdateEntities(gameTime);

            // --- WAVE MANAGEMENT ---
            if (_enemies.Count == 0)
            {
                _waveDelayTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_waveDelayTimer > 2.0f) // 2 second break between waves
                {
                    // 1. Evolve AI based on performance
                    _waveDirector.Evolve(_enemiesReachedBaseThisWave);
                    _enemiesReachedBaseThisWave = false;

                    // 2. Next Wave
                    _currentWave++;

                    // 3. Spawn New Wave
                    _spawner.SpawnWave(_waveDirector, _mapGen.Paths, _towerManager.GetTowers(), _enemies, _currentWave);

                    _waveDelayTimer = 0;
                }
            }

            base.Update(gameTime);
            _previousKeyboardState = Keyboard.GetState();
        }

        private void HandleInput()
        {
            KeyboardState kState = Keyboard.GetState();
            MouseState currentMouse = Mouse.GetState();

            if (kState.IsKeyDown(Keys.D1)) _selectedType = TowerType.Basic;
            if (kState.IsKeyDown(Keys.D2)) _selectedType = TowerType.Sniper;
            if (kState.IsKeyDown(Keys.D3)) _selectedType = TowerType.FastFire;

            if (currentMouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                int gX = currentMouse.X / TileSize;
                int gY = currentMouse.Y / TileSize;

                if (gX >= 0 && gX < _mapGen.GridMap.GetLength(0) && gY >= 0 && gY < _mapGen.GridMap.GetLength(1))
                {
                    if (_mapGen.GridMap[gX, gY] == 0)
                    {
                        Vector2 snapPos = new Vector2(gX * TileSize + (TileSize / 2), gY * TileSize + (TileSize / 2));
                        if (_towerManager.CreateTower(_selectedType, snapPos, ref gold))
                        {
                            _mapGen.GridMap[gX, gY] = 2;
                        }
                    }
                }
            }
            _previousMouseState = currentMouse;
            _previousKeyboardState = kState;
        }

        private void UpdateEntities(GameTime gameTime)
        {
            QuadTree spatialIndex = new QuadTree(new Rectangle(0, 0, 1024, 768));
            List<Enemy> newChildren = new List<Enemy>();

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                _enemies[i].Update(gameTime);

                if (_enemies[i] is duplicator dup)
                {
                    newChildren.AddRange(dup.GetPendingChildren());
                }

                if (!_enemies[i].IsActive)
                {
                    if (_enemies[i].Health > 0)
                    {
                        hp--;
                        _enemiesReachedBaseThisWave = true; // Mark that player took damage
                    }
                    if (_enemies[i].Health <= 0)
                    {
                        gold += _enemies[i].GoldReward;
                    }
                    _enemies.RemoveAt(i);
                }
                else
                {
                    if (_enemies[i].Health > 0)
                        spatialIndex.Insert(_enemies[i]);
                }
            }

            _enemies.AddRange(newChildren);
            _towerManager.Update(gameTime, spatialIndex, _bullets);

            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update();
                if (!_bullets[i].IsActive) _bullets.RemoveAt(i);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Inside Game1.cs Draw method
            for (int x = 0; x < _mapGen.GridMap.GetLength(0); x++)
            {
                for (int y = 0; y < _mapGen.GridMap.GetLength(1); y++)
                {
                    Rectangle rect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize); // Removed -1 for solid grid
                    Color tileColor;
                    int tileType = _mapGen.GridMap[x, y];

                    // Use more distinct colors for debugging
                    tileColor = tileType switch
                    {
                        1 => Color.Gray,         // Path 1
                        3 => Color.SlateGray,    // Path 2
                        4 => Color.Red,          // Intersection
                        2 => Color.DarkSlateGray,// Tower occupied
                        _ => Color.ForestGreen   // Empty
                    };

                    _spriteBatch.Draw(_pixel, rect, tileColor); // Use full opacity to test
                }
            }

            _towerManager.Draw(_spriteBatch);
            foreach (var e in _enemies) e.Draw(_spriteBatch, _gameFont, _towerManager.TypeIcons);
            foreach (var b in _bullets) b.Draw(_spriteBatch);

            // Draw UI
            _spriteBatch.DrawString(_gameFont, $"Selected: {_selectedType}", new Vector2(20, 50), Color.White);
            _spriteBatch.DrawString(_gameFont, $"Gold: {gold}", new Vector2(400, 20), Color.Yellow);
            _spriteBatch.DrawString(_gameFont, $"HP: {hp}", new Vector2(20, 20), Color.Red);
            _spriteBatch.DrawString(_gameFont, $"Wave: {_currentWave}", new Vector2(120, 20), Color.Yellow);
            _spriteBatch.DrawString(_gameFont, $"Enemies: {_enemies.Count}", new Vector2(240, 20), Color.Yellow);

            if (hp <= 0)
            {
                string message = "GAME OVER";
                Vector2 fontSize = _gameFont.MeasureString(message);
                Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                Vector2 origin = fontSize / 2;
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, 1024, 768), Color.Black * 0.5f);
                _spriteBatch.DrawString(_gameFont, message, screenCenter, Color.Red, 0, origin, 3f, SpriteEffects.None, 0);
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
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
        private TowerManager _towerManager;
        private List<Bullet> _bullets = new List<Bullet>();
        private double _currentWave = 1, _waveEndCount = 0;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState; // Add this line
        private const int TileSize = 48;
        int gold=400;
        // Inside Game1.cs
        private bool _isShopOpen = false;
        private TowerType _selectedType = TowerType.Basic; // Default tower

        private int hp = 5; // Player life resource

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
            _towerManager.LoadContent(Content, _pixel);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
            HandleInput();
            _spawner.Update(gameTime, _currentWave, _mapGen.Paths, _enemies);
            UpdateEntities(gameTime);
            base.Update(gameTime);
            _previousKeyboardState = Keyboard.GetState();
        }


        private void HandleInput()
        {
            KeyboardState kState = Keyboard.GetState();
            MouseState currentMouse = Mouse.GetState();

            // 1. Select Tower Type with 0, 1, 2 keys
            // Mapping: 0 = Basic, 1 = Sniper, 2 = FastFire
            if (kState.IsKeyDown(Keys.D1)) _selectedType = TowerType.Basic;
            if (kState.IsKeyDown(Keys.D2)) _selectedType = TowerType.Sniper;
            if (kState.IsKeyDown(Keys.D3)) _selectedType = TowerType.FastFire;

            // 2. Place Tower on Left Click
            if (currentMouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                int gX = currentMouse.X / TileSize;
                int gY = currentMouse.Y / TileSize;

                // Check if mouse is within map bounds
                if (gX >= 0 && gX < _mapGen.GridMap.GetLength(0) && gY >= 0 && gY < _mapGen.GridMap.GetLength(1))
                {
                    // Only allow placement on Grass (tileType 0)
                    if (_mapGen.GridMap[gX, gY] == 0)
                    {
                        Vector2 snapPos = new Vector2(gX * TileSize + (TileSize / 2), gY * TileSize + (TileSize / 2));

                        // TowerManager handles the gold check and deduction via 'ref gold'
                        if (_towerManager.CreateTower(_selectedType, snapPos, ref gold))
                        {
                            // Success! Mark tile as occupied (type 2) so we can't stack towers
                            _mapGen.GridMap[gX, gY] = 2;
                        }
                    }
                }
            }

            // Update states for next frame
            _previousMouseState = currentMouse;
            _previousKeyboardState = kState;
        }
        private void UpdateEntities(GameTime gameTime)
        {
            if (hp <= 0) return;
            QuadTree spatialIndex = new QuadTree(new Rectangle(0, 0, 1024, 768));
            List<Enemy> newChildren = new List<Enemy>();

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                _enemies[i].Update(gameTime); //

                // Specifically check if this is a duplicator to grab its delayed children
                if (_enemies[i] is duplicator dup)
                {
                    newChildren.AddRange(dup.GetPendingChildren());
                }

                if (!_enemies[i].IsActive)
                {
                    // If they reached the end (Health > 0), player loses hp
                    if (_enemies[i].Health > 0)
                    {
                        hp--;
                    }
                    if (_enemies[i].Health <= 0)
                    {
                        gold += _enemies[i].GoldReward; 
                    }

                    _enemies.RemoveAt(i); //
                    _waveEndCount++;
                }
                else
                {
                    // Only add to the spatial index for towers to target if they are alive
                    if (_enemies[i].Health > 0)
                        spatialIndex.Insert(_enemies[i]); //
                }
            }

            // Add any children spwaned this frame to the main list
            _enemies.AddRange(newChildren);

            _towerManager.Update(gameTime, spatialIndex, _bullets);
            
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

                    Color tileColor;
                    int tileType = _mapGen.GridMap[x, y];

                    if (tileType == 1)
                    {
                        tileColor = Color.Blue; // Color for Path 1
                    }
                    else if (tileType == 3)
                    {
                        tileColor = Color.Azure; // Distinct color for Path 2
                    }
                    else if (tileType==4)
                    {
                        tileColor=Color.Aquamarine;
                    }
                    else
                    {
                        tileColor = Color.ForestGreen; // Grass/Background
                    }

                    _spriteBatch.Draw(_pixel, rect, tileColor * 0.5f);
                }
            }

            _towerManager.Draw(_spriteBatch);
            _towerManager.Draw(_spriteBatch);

            foreach (var e in _enemies)
            {
                // Pass the Dictionary from TowerManager as the third argument
                e.Draw(_spriteBatch, _gameFont, _towerManager.TypeIcons);
            }
            foreach (var b in _bullets) b.Draw(_spriteBatch);

            // Draw  UI
            _spriteBatch.DrawString(_gameFont, $"Selected: {_selectedType}", new Vector2(20, 50), Color.White);// selected tower
            _spriteBatch.DrawString(_gameFont, $"balance: {gold}", new Vector2(400, 20), Color.Yellow);//gold balance
            _spriteBatch.DrawString(_gameFont, $"hp left: {hp}", new Vector2(20, 20), Color.Yellow);// draw hp left
            _spriteBatch.DrawString(_gameFont, $"wave: {_currentWave}", new Vector2(120, 20), Color.Yellow);// draw current wave
            _spriteBatch.DrawString(_gameFont, $"enemies alive: {_enemies.Count}", new Vector2(220, 20), Color.Yellow);//draw enemies alive

            if (hp <= 0)
            {
                string message = "GAME OVER";

                // Calculate the center position based on font size
                Vector2 fontSize = _gameFont.MeasureString(message);
                Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                Vector2 origin = fontSize / 2;

                // Draw a dark overlay to make text pop
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, 1024, 768), Color.Black * 0.5f);

                // Draw the text (using scale 3f to make it "Big")
                _spriteBatch.DrawString(_gameFont, message, screenCenter, Color.Red, 0, origin, 3f, SpriteEffects.None, 0);
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
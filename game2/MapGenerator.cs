using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace game2
{
    public class MapGenerator
    {
        public int[,] GridMap { get; private set; }
        public List<Vector2> Path { get; private set; }
        private int _cols, _rows, _tileSize;

        public MapGenerator(int width, int height, int tileSize)
        {
            _tileSize = tileSize;
            _cols = width / tileSize;
            _rows = height / tileSize;
            GridMap = new int[_cols, _rows];
            Path = new List<Vector2>();
        }

        public void Generate()
        {
            Path.Clear();
            Array.Clear(GridMap, 0, GridMap.Length);
            Random rand = new Random();

            int currentX = 0;
            int currentY = rand.Next(5, _rows - 5);
            int lastTurn = 0; // 0:R, 1:U, 2:D, 3:L

            // Start the path
            Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
            currentX++;
            Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            int iterations = 0;
            while (currentX < _cols - 2 && iterations < 20)
            {
                int action = rand.Next(0, 3); // 0: Move Forward, 1: Loop, 2: Random Turn

                if (action == 0)
                {
                    currentX += 2; // Move right to ensure progress
                    Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
                    lastTurn = 0; // Moving Right
                }
                else if (action == 1 && CanYouLoopThatWay(currentX, currentY, 0))
                {
                    LoopUpStart(ref currentX, ref currentY, 0);
                    currentX += 2;
                    Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
                    lastTurn = 0; // Ends moving Right
                }
                else if (action == 2)
                {
                    // FIX: We now pass lastTurn as a ref so it updates for the next loop
                    TryApplyRandomTurn(ref currentX, ref currentY, ref lastTurn);
                }

                iterations++;
            }

            // Ensure it reaches the end of the screen
            Path.Add(new Vector2(_cols * _tileSize, currentY * _tileSize));
            MarkGrid();
        }

        private Point TryApplyRandomTurn(ref int x, ref int y, ref int lastTurn)
        {
            Random rand = new Random();
            int choice = rand.Next(0, 2);

            int checkDir;
            // Determine the new direction based on where we just came from
            if (lastTurn == 0 || lastTurn == 3) // If was horizontal, turn vertical
                checkDir = (choice == 0) ? 1 : 2; // 1: Up, 2: Down
            else // If was vertical, turn horizontal
                checkDir = (choice == 0) ? 0 : 3; // 0: Right, 3: Left

            bool canGo = CanYouGoThatWay(checkDir, x, y);
            int moveAmount = canGo ? 2 : 0;

            int deltaX = 0;
            int deltaY = 0;

            if (canGo)
            {
                if (checkDir == 0) deltaX = moveAmount;
                else if (checkDir == 3) deltaX = -moveAmount;
                else if (checkDir == 1) deltaY = -moveAmount;
                else if (checkDir == 2) deltaY = moveAmount;

                x += deltaX;
                y += deltaY;
                lastTurn = checkDir; // UPDATE the direction so the next turn is relative to this one
                Path.Add(new Vector2(x * _tileSize, y * _tileSize));
            }

            return new Point(deltaX, deltaY);
        }
        public bool CanYouLoopThatWay(int currentX, int currentY, int iswhat)
        {
            switch (iswhat)
            {
                case 0: // Checking RIGHT
                        // Needs 2 tiles right, and 2 tiles both up and down
                    return (currentX + 2 < _cols) && (currentY - 2 >= 0) && (currentY + 2 < _rows);

                case 3: // Checking LEFT
                        // Needs 2 tiles left, and 2 tiles both up and down
                    return (currentX - 2 >= 0) && (currentY - 2 >= 0) && (currentY + 2 < _rows);

                case 1: // Checking UP
                        // Needs 2 tiles up, and 2 tiles both left and right
                    return (currentY - 2 >= 0) && (currentX - 2 >= 0) && (currentX + 2 < _cols);

                case 2: // Checking DOWN
                        // Needs 2 tiles down, and 2 tiles both left and right
                    return (currentY + 2 < _rows) && (currentX - 2 >= 0) && (currentX + 2 < _cols);

                default:
                    return false;
            }
        }

        public bool CanYouGoThatWay(int direction, int currentX, int currentY)
        {
            int targetX = currentX;
            int targetY = currentY;

            switch (direction)
            {
                case 0: targetY--; break;
                case 1: targetX++; break;
                case 2: targetY++; break;
                case 3: targetX--; break;
                default: return false;
            }

            if (targetX < 0 || targetX >= _cols || targetY < 0 || targetY >= _rows)
            {
                return false;
            }

            return GridMap[targetX, targetY] == 0;
        }
        public void LoopUpStart(ref int currentX, ref int currentY, int iswhat)
        {
            Random rand = new Random();
            int upordown = rand.Next(0, 2);

            if (iswhat == 0) currentX += 2;
            else if (iswhat == 3) currentX -= 2;
            else if (iswhat == 1) currentY -= 2;
            else if (iswhat == 2) currentY += 2;
            Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            if (iswhat == 0 || iswhat == 3)
            {
                if (upordown == 0) currentY += 2; else currentY -= 2;
            }
            else
            {
                if (upordown == 0) currentX += 2; else currentX -= 2;
            }
            Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            if (iswhat == 0) currentX -= 2;
            else if (iswhat == 3) currentX += 2;
            else if (iswhat == 1) currentY += 2;
            else if (iswhat == 2) currentY -= 2;
            Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            if (iswhat == 0 || iswhat == 3)
            {
                if (upordown == 0) currentY -= 3; else currentY += 3;
            }
            else
            {
                if (upordown == 0) currentX -= 3; else currentX += 3;
            }
            Path.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
        }
        private void MarkGrid()
        {
            for (int i = 0; i < Path.Count - 1; i++)
            {
                int startX = (int)Path[i].X / _tileSize;
                int startY = (int)Path[i].Y / _tileSize;
                int endX = (int)Path[i + 1].X / _tileSize;
                int endY = (int)Path[i + 1].Y / _tileSize;

                for (int x = Math.Min(startX, endX); x <= Math.Max(startX, endX); x++)
                    for (int y = Math.Min(startY, endY); y <= Math.Max(startY, endY); y++)
                        if (x >= 0 && x < _cols && y >= 0 && y < _rows)
                            GridMap[x, y] = 1;
            }
        }
    }
}
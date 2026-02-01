using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace game2
{
    public class MapGenerator
    {
        public int[,] GridMap { get; private set; }
        // Changed to a list of lists to store multiple paths
        public List<List<Vector2>> Paths { get; private set; }
        private int _cols, _rows, _tileSize;

        public MapGenerator(int width, int height, int tileSize)
        {
            _tileSize = tileSize;
            _cols = width / tileSize;
            _rows = height / tileSize;
            GridMap = new int[_cols, _rows];
            Paths = new List<List<Vector2>>();
        }

        public void Generate()
        {
            Paths.Clear();
            Array.Clear(GridMap, 0, GridMap.Length);

            int mid = _rows / 2;
            // Path 1 starts 2 tiles above middle, Path 2 starts 2 tiles below (4 tile diff)
            Paths.Add(GenerateSinglePath(mid - 2));
            Paths.Add(GenerateSinglePath(mid + 2));

            // Mark grid: Path 1 as 1 (Gray), Path 2 as 3 (Slate)
            MarkGrid(Paths[0], 1);
            MarkGrid(Paths[1], 3);
        }

        private List<Vector2> GenerateSinglePath(int startY)
        {
            List<Vector2> newPath = new List<Vector2>();
            Random rand = new Random();
            int currentX = 0;
            int currentY = startY;
            int last = rand.Next(0, 2);
            int didyounotloop = 0;

            newPath.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
            currentX++;
            newPath.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            int iterations = 0;
            while (currentX < _cols - 2 && iterations < 20)
            {
                int action = rand.Next(0, 2);
                if (action == 0)
                {
                    currentX += 2;
                    newPath.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
                    didyounotloop++;
                }
                else if (action == 1 && CanYouLoopThatWay(currentX, currentY, 0) && didyounotloop != 0)
                {
                    LoopUpStart(ref currentX, ref currentY, 0, ref last, newPath);
                    currentX += 2;
                    newPath.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
                    didyounotloop = 0;
                }
                iterations++;
            }

            newPath.Add(new Vector2(_cols * _tileSize, currentY * _tileSize));
            return newPath;
        }

        public bool CanYouLoopThatWay(int currentX, int currentY, int iswhat)
        {
            switch (iswhat)
            {
                case 0: return (currentX + 3 < _cols) && (currentY - 2 >= 0) && (currentY + 2 < _rows);
                case 3: return (currentX - 3 >= 0) && (currentY - 2 >= 0) && (currentY + 2 < _rows);
                case 1: return (currentY - 2 >= 0) && (currentX - 2 >= 0) && (currentX + 2 < _cols);
                case 2: return (currentY + 2 < _rows) && (currentX - 2 >= 0) && (currentX + 2 < _cols);
                default: return false;
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
            return (targetX >= 0 && targetX < _cols && targetY >= 0 && targetY < _rows) && GridMap[targetX, targetY] == 0;
        }

        public void LoopUpStart(ref int currentX, ref int currentY, int iswhat, ref int last, List<Vector2> p)
        {
            Random rand = new Random();
            int upordown = rand.Next(0, 2);

            if (last != 1 && CanYouGoThatWay(1, currentX, currentY - 3)) { currentY -= 3; p.Add(new Vector2(currentX * _tileSize, currentY * _tileSize)); last = 1; }
            else if (last != 0 && CanYouGoThatWay(2, currentX, currentY + 3)) { currentY += 3; p.Add(new Vector2(currentX * _tileSize, currentY * _tileSize)); last = 0; }

            if (iswhat == 0) currentX += 2;
            else if (iswhat == 3) currentX -= 2;
            else if (iswhat == 1) currentY -= 2;
            else if (iswhat == 2) currentY += 2;
            p.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            if (iswhat == 0 || iswhat == 3) { if (upordown == 0) currentY += 2; else currentY -= 2; }
            else { if (upordown == 0) currentX += 2; else currentX -= 2; }
            p.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            if (iswhat == 0) currentX -= 2;
            else if (iswhat == 3) currentX += 2;
            else if (iswhat == 1) currentY += 2;
            else if (iswhat == 2) currentY -= 2;
            p.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));

            if (iswhat == 0 || iswhat == 3) { if (upordown == 0) currentY -= 3; else currentY += 3; }
            else { if (upordown == 0) currentX -= 3; else currentX += 3; }
            p.Add(new Vector2(currentX * _tileSize, currentY * _tileSize));
        }

        private void MarkGrid(List<Vector2> path, int value)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                int sX = (int)path[i].X / _tileSize;
                int sY = (int)path[i].Y / _tileSize;
                int eX = (int)path[i + 1].X / _tileSize;
                int eY = (int)path[i + 1].Y / _tileSize;

                for (int x = Math.Min(sX, eX); x <= Math.Max(sX, eX); x++)
                    for (int y = Math.Min(sY, eY); y <= Math.Max(sY, eY); y++)
                        if (x >= 0 && x < _cols && y >= 0 && y < _rows)
                        {
                            // LOGIC CHANGE HERE:
                            // If the grid is already Path 1 (1) and we are trying to write Path 2 (3),
                            // mark it as Intersection (4).
                            if (GridMap[x, y] == 1 && value == 3)
                            {
                                GridMap[x, y] = 4;
                            }
                            // Otherwise, only overwrite if it's not already an intersection
                            else if (GridMap[x, y] != 4)
                            {
                                GridMap[x, y] = value;
                            }
                        }
            }
        }
    }
}
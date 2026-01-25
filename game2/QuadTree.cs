using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace game2
{
    public class QuadTree
    {
        private int MAX_OBJECTS = 4; // How many enemies before a node splits
        private Rectangle _boundary;
        private List<Enemy> _enemies;
        private QuadTree[] _children;
        private bool _isDivided = false;

        public QuadTree(Rectangle boundary)
        {
            _boundary = boundary;
            _enemies = new List<Enemy>();
        }

        private void Split()
        {
            int subWidth = _boundary.Width / 2;
            int subHeight = _boundary.Height / 2;
            int x = _boundary.X;
            int y = _boundary.Y;

            _children = new QuadTree[4];
            _children[0] = new QuadTree(new Rectangle(x + subWidth, y, subWidth, subHeight)); // NE
            _children[1] = new QuadTree(new Rectangle(x, y, subWidth, subHeight));            // NW
            _children[2] = new QuadTree(new Rectangle(x, y + subHeight, subWidth, subHeight)); // SW
            _children[3] = new QuadTree(new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight)); // SE

            _isDivided = true;
        }

        public void Insert(Enemy enemy)
        {
            if (!_boundary.Contains(enemy.Position)) return;

            if (_enemies.Count < MAX_OBJECTS && !_isDivided)
            {
                _enemies.Add(enemy);
            }
            else
            {
                if (!_isDivided) Split();
                foreach (var child in _children) child.Insert(enemy);
            }
        }

        public void Query(Rectangle range, List<Enemy> found)
        {
            if (!_boundary.Intersects(range)) return;

            foreach (var enemy in _enemies)
            {
                if (range.Contains(enemy.Position) && enemy.IsActive)
                    found.Add(enemy);
            }

            if (_isDivided)
            {
                foreach (var child in _children) child.Query(range, found);
            }
        }
    }
}
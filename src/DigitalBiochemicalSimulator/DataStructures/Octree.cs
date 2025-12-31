using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.DataStructures
{
    /// <summary>
    /// Octree for efficient 3D spatial indexing and queries.
    /// Provides O(log n) average-case performance for range queries and nearest neighbor searches.
    /// </summary>
    public class Octree
    {
        private OctreeNode _root;
        private readonly int _maxDepth;
        private readonly int _maxTokensPerNode;

        public int TotalTokens { get; private set; }
        public BoundingBox Bounds { get; private set; }

        /// <summary>
        /// Creates a new Octree with specified bounds and parameters
        /// </summary>
        public Octree(BoundingBox bounds, int maxDepth = 8, int maxTokensPerNode = 8)
        {
            Bounds = bounds;
            _maxDepth = maxDepth;
            _maxTokensPerNode = maxTokensPerNode;
            _root = new OctreeNode(bounds, 0, maxTokensPerNode);
            TotalTokens = 0;
        }

        /// <summary>
        /// Creates an Octree from grid dimensions
        /// </summary>
        public static Octree FromGrid(int width, int height, int depth, int maxDepth = 8, int maxTokensPerNode = 8)
        {
            var bounds = new BoundingBox(
                new Vector3Int(0, 0, 0),
                new Vector3Int(width, height, depth)
            );
            return new Octree(bounds, maxDepth, maxTokensPerNode);
        }

        /// <summary>
        /// Inserts a token into the octree
        /// </summary>
        public bool Insert(Token token)
        {
            if (token == null || !Bounds.Contains(token.Position))
                return false;

            bool inserted = _root.Insert(token, _maxDepth);
            if (inserted)
                TotalTokens++;

            return inserted;
        }

        /// <summary>
        /// Removes a token from the octree
        /// </summary>
        public bool Remove(Token token)
        {
            if (token == null)
                return false;

            bool removed = _root.Remove(token);
            if (removed)
                TotalTokens--;

            return removed;
        }

        /// <summary>
        /// Updates a token's position in the octree
        /// </summary>
        public bool Update(Token token, Vector3Int oldPosition)
        {
            // Remove from old position and insert at new position
            var tempPosition = token.Position;
            token.Position = oldPosition;
            bool removed = Remove(token);
            token.Position = tempPosition;

            if (removed)
            {
                return Insert(token);
            }

            return false;
        }

        /// <summary>
        /// Finds all tokens within a spherical range
        /// </summary>
        public List<Token> QueryRange(Vector3Int center, float radius)
        {
            var results = new List<Token>();
            _root.QueryRange(center, radius, results);
            return results;
        }

        /// <summary>
        /// Finds all tokens within a bounding box
        /// </summary>
        public List<Token> QueryBox(BoundingBox box)
        {
            var results = new List<Token>();
            _root.QueryBox(box, results);
            return results;
        }

        /// <summary>
        /// Finds the k nearest neighbors to a position
        /// </summary>
        public List<Token> FindNearestNeighbors(Vector3Int position, int k)
        {
            var candidates = new List<TokenDistance>();
            _root.FindNearestNeighbors(position, candidates);

            return candidates
                .OrderBy(td => td.Distance)
                .Take(k)
                .Select(td => td.Token)
                .ToList();
        }

        /// <summary>
        /// Finds the nearest token to a position
        /// </summary>
        public Token FindNearest(Vector3Int position)
        {
            var neighbors = FindNearestNeighbors(position, 1);
            return neighbors.FirstOrDefault();
        }

        /// <summary>
        /// Clears all tokens from the octree
        /// </summary>
        public void Clear()
        {
            _root.Clear();
            TotalTokens = 0;
        }

        /// <summary>
        /// Rebuilds the octree with current tokens (useful after many updates)
        /// </summary>
        public void Rebuild()
        {
            var allTokens = new List<Token>();
            _root.GetAllTokens(allTokens);
            Clear();

            foreach (var token in allTokens)
            {
                Insert(token);
            }
        }

        /// <summary>
        /// Gets statistics about the octree structure
        /// </summary>
        public OctreeStatistics GetStatistics()
        {
            var stats = new OctreeStatistics();
            _root.CollectStatistics(stats);
            stats.TotalTokens = TotalTokens;
            return stats;
        }

        public override string ToString()
        {
            return $"Octree(Tokens:{TotalTokens}, Bounds:{Bounds})";
        }
    }

    /// <summary>
    /// A node in the octree structure
    /// </summary>
    internal class OctreeNode
    {
        private BoundingBox _bounds;
        private List<Token> _tokens;
        private OctreeNode[] _children;
        private int _depth;
        private int _maxTokensPerNode;

        public bool IsLeaf => _children == null;
        public int TokenCount => _tokens?.Count ?? 0;

        public OctreeNode(BoundingBox bounds, int depth, int maxTokensPerNode)
        {
            _bounds = bounds;
            _depth = depth;
            _maxTokensPerNode = maxTokensPerNode;
            _tokens = new List<Token>();
            _children = null;
        }

        public bool Insert(Token token, int maxDepth)
        {
            if (!_bounds.Contains(token.Position))
                return false;

            // If this is a leaf and not full, add here
            if (IsLeaf)
            {
                if (_tokens.Count < _maxTokensPerNode || _depth >= maxDepth)
                {
                    _tokens.Add(token);
                    return true;
                }
                else
                {
                    // Subdivide and redistribute
                    Subdivide();
                    RedistributeTokens();
                }
            }

            // Insert into appropriate child
            foreach (var child in _children)
            {
                if (child.Insert(token, maxDepth))
                    return true;
            }

            return false;
        }

        public bool Remove(Token token)
        {
            if (!_bounds.Contains(token.Position))
                return false;

            if (IsLeaf)
            {
                return _tokens.Remove(token);
            }

            // Try to remove from children
            foreach (var child in _children)
            {
                if (child.Remove(token))
                    return true;
            }

            return false;
        }

        public void QueryRange(Vector3Int center, float radius, List<Token> results)
        {
            // Check if this node's bounds intersect with the query sphere
            if (!_bounds.IntersectsSphere(center, radius))
                return;

            if (IsLeaf)
            {
                // Check each token
                float radiusSquared = radius * radius;
                foreach (var token in _tokens)
                {
                    float distSquared = DistanceSquared(token.Position, center);
                    if (distSquared <= radiusSquared)
                    {
                        results.Add(token);
                    }
                }
            }
            else
            {
                // Recurse into children
                foreach (var child in _children)
                {
                    child.QueryRange(center, radius, results);
                }
            }
        }

        public void QueryBox(BoundingBox box, List<Token> results)
        {
            if (!_bounds.Intersects(box))
                return;

            if (IsLeaf)
            {
                foreach (var token in _tokens)
                {
                    if (box.Contains(token.Position))
                    {
                        results.Add(token);
                    }
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    child.QueryBox(box, results);
                }
            }
        }

        public void FindNearestNeighbors(Vector3Int position, List<TokenDistance> candidates)
        {
            if (IsLeaf)
            {
                foreach (var token in _tokens)
                {
                    float distance = Distance(token.Position, position);
                    candidates.Add(new TokenDistance { Token = token, Distance = distance });
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    child.FindNearestNeighbors(position, candidates);
                }
            }
        }

        public void GetAllTokens(List<Token> tokens)
        {
            if (IsLeaf)
            {
                tokens.AddRange(_tokens);
            }
            else
            {
                foreach (var child in _children)
                {
                    child.GetAllTokens(tokens);
                }
            }
        }

        public void Clear()
        {
            _tokens?.Clear();
            _children = null;
        }

        public void CollectStatistics(OctreeStatistics stats)
        {
            stats.TotalNodes++;

            if (IsLeaf)
            {
                stats.LeafNodes++;
                stats.MaxDepth = Math.Max(stats.MaxDepth, _depth);
                if (_tokens.Count > 0)
                {
                    stats.OccupiedLeaves++;
                }
                stats.TotalTokensInLeaves += _tokens.Count;
            }
            else
            {
                foreach (var child in _children)
                {
                    child.CollectStatistics(stats);
                }
            }
        }

        private void Subdivide()
        {
            var center = _bounds.Center;
            _children = new OctreeNode[8];

            // Create 8 child octants
            for (int i = 0; i < 8; i++)
            {
                var childBounds = GetChildBounds(i);
                _children[i] = new OctreeNode(childBounds, _depth + 1, _maxTokensPerNode);
            }
        }

        private void RedistributeTokens()
        {
            var tokensToRedistribute = new List<Token>(_tokens);
            _tokens.Clear();

            foreach (var token in tokensToRedistribute)
            {
                bool inserted = false;
                foreach (var child in _children)
                {
                    if (child._bounds.Contains(token.Position))
                    {
                        child._tokens.Add(token);
                        inserted = true;
                        break;
                    }
                }

                // If token couldn't be inserted into child (edge case), keep in parent
                if (!inserted)
                {
                    _tokens.Add(token);
                }
            }
        }

        private BoundingBox GetChildBounds(int octant)
        {
            var center = _bounds.Center;
            var min = _bounds.Min;
            var max = _bounds.Max;

            var childMin = new Vector3Int(
                (octant & 1) == 0 ? min.X : center.X,
                (octant & 2) == 0 ? min.Y : center.Y,
                (octant & 4) == 0 ? min.Z : center.Z
            );

            var childMax = new Vector3Int(
                (octant & 1) == 0 ? center.X : max.X,
                (octant & 2) == 0 ? center.Y : max.Y,
                (octant & 4) == 0 ? center.Z : max.Z
            );

            return new BoundingBox(childMin, childMax);
        }

        private float Distance(Vector3Int a, Vector3Int b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            int dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private float DistanceSquared(Vector3Int a, Vector3Int b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            int dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }
    }

    /// <summary>
    /// Represents a 3D axis-aligned bounding box
    /// </summary>
    public class BoundingBox
    {
        public Vector3Int Min { get; set; }
        public Vector3Int Max { get; set; }

        public Vector3Int Center => new Vector3Int(
            (Min.X + Max.X) / 2,
            (Min.Y + Max.Y) / 2,
            (Min.Z + Max.Z) / 2
        );

        public BoundingBox(Vector3Int min, Vector3Int max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(Vector3Int point)
        {
            return point.X >= Min.X && point.X < Max.X &&
                   point.Y >= Min.Y && point.Y < Max.Y &&
                   point.Z >= Min.Z && point.Z < Max.Z;
        }

        public bool Intersects(BoundingBox other)
        {
            return Min.X < other.Max.X && Max.X > other.Min.X &&
                   Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
                   Min.Z < other.Max.Z && Max.Z > other.Min.Z;
        }

        public bool IntersectsSphere(Vector3Int center, float radius)
        {
            // Find closest point on box to sphere center
            int closestX = Math.Max(Min.X, Math.Min(center.X, Max.X));
            int closestY = Math.Max(Min.Y, Math.Min(center.Y, Max.Y));
            int closestZ = Math.Max(Min.Z, Math.Min(center.Z, Max.Z));

            // Calculate distance from closest point to center
            int dx = closestX - center.X;
            int dy = closestY - center.Y;
            int dz = closestZ - center.Z;
            float distanceSquared = dx * dx + dy * dy + dz * dz;

            return distanceSquared <= radius * radius;
        }

        public override string ToString()
        {
            return $"BoundingBox(Min:{Min}, Max:{Max})";
        }
    }

    /// <summary>
    /// Helper class for nearest neighbor queries
    /// </summary>
    internal class TokenDistance
    {
        public Token Token { get; set; }
        public float Distance { get; set; }
    }

    /// <summary>
    /// Statistics about octree structure
    /// </summary>
    public class OctreeStatistics
    {
        public int TotalNodes { get; set; }
        public int LeafNodes { get; set; }
        public int OccupiedLeaves { get; set; }
        public int MaxDepth { get; set; }
        public int TotalTokens { get; set; }
        public int TotalTokensInLeaves { get; set; }

        public float AverageTokensPerLeaf => OccupiedLeaves > 0
            ? (float)TotalTokensInLeaves / OccupiedLeaves
            : 0;

        public override string ToString()
        {
            return $"Octree Stats: {TotalNodes} nodes ({LeafNodes} leaves), " +
                   $"{TotalTokens} tokens, MaxDepth: {MaxDepth}, " +
                   $"Avg tokens/leaf: {AverageTokensPerLeaf:F2}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.DataStructures
{
    /// <summary>
    /// High-level spatial indexing system for efficient token queries.
    /// Wraps Octree and provides integration with simulation systems.
    /// </summary>
    public class SpatialIndex
    {
        private Octree _octree;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int _gridDepth;
        private bool _isDirty;
        private int _rebuildThreshold;
        private int _updatesSinceRebuild;

        public int TotalTokens => _octree.TotalTokens;
        public bool AutoRebuild { get; set; }

        /// <summary>
        /// Creates a new spatial index for the grid
        /// </summary>
        public SpatialIndex(int gridWidth, int gridHeight, int gridDepth,
            int maxDepth = 8, int maxTokensPerNode = 8, int rebuildThreshold = 1000)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _gridDepth = gridDepth;
            _rebuildThreshold = rebuildThreshold;
            _updatesSinceRebuild = 0;
            AutoRebuild = true;

            _octree = Octree.FromGrid(gridWidth, gridHeight, gridDepth, maxDepth, maxTokensPerNode);
            _isDirty = false;
        }

        /// <summary>
        /// Indexes a collection of tokens
        /// </summary>
        public void IndexTokens(IEnumerable<Token> tokens)
        {
            _octree.Clear();
            _updatesSinceRebuild = 0;

            foreach (var token in tokens)
            {
                if (token.IsActive)
                {
                    _octree.Insert(token);
                }
            }

            _isDirty = false;
        }

        /// <summary>
        /// Adds a token to the index
        /// </summary>
        public bool AddToken(Token token)
        {
            if (!token.IsActive)
                return false;

            bool added = _octree.Insert(token);
            if (added)
            {
                _isDirty = true;
                CheckRebuild();
            }

            return added;
        }

        /// <summary>
        /// Removes a token from the index
        /// </summary>
        public bool RemoveToken(Token token)
        {
            bool removed = _octree.Remove(token);
            if (removed)
            {
                _isDirty = true;
                CheckRebuild();
            }

            return removed;
        }

        /// <summary>
        /// Updates a token's position in the index
        /// </summary>
        public bool UpdateTokenPosition(Token token, Vector3Int oldPosition)
        {
            _updatesSinceRebuild++;
            bool updated = _octree.Update(token, oldPosition);

            if (updated)
            {
                _isDirty = true;
                CheckRebuild();
            }

            return updated;
        }

        /// <summary>
        /// Finds all tokens within a spherical range
        /// </summary>
        public List<Token> FindTokensInRange(Vector3Int center, float radius)
        {
            return _octree.QueryRange(center, radius);
        }

        /// <summary>
        /// Finds all tokens within a box region
        /// </summary>
        public List<Token> FindTokensInBox(Vector3Int min, Vector3Int max)
        {
            var box = new BoundingBox(min, max);
            return _octree.QueryBox(box);
        }

        /// <summary>
        /// Finds all tokens in a specific cell
        /// </summary>
        public List<Token> FindTokensInCell(Vector3Int cellPosition)
        {
            return FindTokensInBox(cellPosition, cellPosition + new Vector3Int(1, 1, 1));
        }

        /// <summary>
        /// Finds k nearest neighbors to a position
        /// </summary>
        public List<Token> FindNearestNeighbors(Vector3Int position, int k)
        {
            return _octree.FindNearestNeighbors(position, k);
        }

        /// <summary>
        /// Finds the nearest token to a position
        /// </summary>
        public Token FindNearestToken(Vector3Int position)
        {
            return _octree.FindNearest(position);
        }

        /// <summary>
        /// Finds all tokens within bonding range of a token
        /// </summary>
        public List<Token> FindPotentialBondingPartners(Token token, float bondingRange)
        {
            var candidates = FindTokensInRange(token.Position, bondingRange);

            // Exclude the token itself and inactive tokens
            return candidates
                .Where(t => t.Id != token.Id && t.IsActive)
                .ToList();
        }

        /// <summary>
        /// Finds all tokens in neighboring cells (26 neighbors + current)
        /// </summary>
        public List<Token> FindTokensInNeighborhood(Vector3Int cellPosition)
        {
            var tokens = new List<Token>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var neighborPos = new Vector3Int(
                            cellPosition.X + dx,
                            cellPosition.Y + dy,
                            cellPosition.Z + dz
                        );

                        // Check bounds
                        if (IsValidCell(neighborPos))
                        {
                            var cellTokens = FindTokensInCell(neighborPos);
                            tokens.AddRange(cellTokens);
                        }
                    }
                }
            }

            return tokens;
        }

        /// <summary>
        /// Finds tokens in a specific altitude range
        /// </summary>
        public List<Token> FindTokensAtAltitude(int minY, int maxY)
        {
            var box = new BoundingBox(
                new Vector3Int(0, minY, 0),
                new Vector3Int(_gridWidth, maxY, _gridDepth)
            );

            return _octree.QueryBox(box);
        }

        /// <summary>
        /// Performs a ray cast to find tokens along a line
        /// </summary>
        public List<Token> RayCast(Vector3Int start, Vector3Int direction, float maxDistance)
        {
            var tokens = new List<Token>();
            var visited = new HashSet<long>();

            // Sample points along the ray
            int samples = (int)Math.Ceiling(maxDistance);
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                var point = new Vector3Int(
                    start.X + (int)(direction.X * t * maxDistance),
                    start.Y + (int)(direction.Y * t * maxDistance),
                    start.Z + (int)(direction.Z * t * maxDistance)
                );

                var nearby = FindTokensInRange(point, 1.0f);
                foreach (var token in nearby)
                {
                    if (!visited.Contains(token.Id))
                    {
                        tokens.Add(token);
                        visited.Add(token.Id);
                    }
                }
            }

            return tokens;
        }

        /// <summary>
        /// Checks if automatic rebuild is needed
        /// </summary>
        private void CheckRebuild()
        {
            if (AutoRebuild && _updatesSinceRebuild >= _rebuildThreshold)
            {
                Rebuild();
            }
        }

        /// <summary>
        /// Rebuilds the spatial index
        /// </summary>
        public void Rebuild()
        {
            _octree.Rebuild();
            _updatesSinceRebuild = 0;
            _isDirty = false;
        }

        /// <summary>
        /// Clears all tokens from the index
        /// </summary>
        public void Clear()
        {
            _octree.Clear();
            _updatesSinceRebuild = 0;
            _isDirty = false;
        }

        /// <summary>
        /// Gets statistics about the spatial index
        /// </summary>
        public SpatialIndexStatistics GetStatistics()
        {
            var octreeStats = _octree.GetStatistics();

            return new SpatialIndexStatistics
            {
                TotalTokens = TotalTokens,
                IsDirty = _isDirty,
                UpdatesSinceRebuild = _updatesSinceRebuild,
                OctreeStats = octreeStats
            };
        }

        /// <summary>
        /// Validates a cell position is within grid bounds
        /// </summary>
        private bool IsValidCell(Vector3Int cellPosition)
        {
            return cellPosition.X >= 0 && cellPosition.X < _gridWidth &&
                   cellPosition.Y >= 0 && cellPosition.Y < _gridHeight &&
                   cellPosition.Z >= 0 && cellPosition.Z < _gridDepth;
        }

        public override string ToString()
        {
            return $"SpatialIndex(Tokens:{TotalTokens}, Grid:{_gridWidth}x{_gridHeight}x{_gridDepth}, " +
                   $"Updates:{_updatesSinceRebuild}, Dirty:{_isDirty})";
        }
    }

    /// <summary>
    /// Statistics about the spatial index
    /// </summary>
    public class SpatialIndexStatistics
    {
        public int TotalTokens { get; set; }
        public bool IsDirty { get; set; }
        public int UpdatesSinceRebuild { get; set; }
        public OctreeStatistics OctreeStats { get; set; }

        public override string ToString()
        {
            return $"SpatialIndex: {TotalTokens} tokens, " +
                   $"{UpdatesSinceRebuild} updates since rebuild, " +
                   $"Dirty: {IsDirty}\n{OctreeStats}";
        }
    }

    /// <summary>
    /// Extension methods for spatial queries
    /// </summary>
    public static class SpatialIndexExtensions
    {
        /// <summary>
        /// Finds clusters of tokens based on proximity
        /// </summary>
        public static List<List<Token>> FindClusters(this SpatialIndex index,
            List<Token> tokens, float clusterRadius)
        {
            var clusters = new List<List<Token>>();
            var assigned = new HashSet<long>();

            foreach (var token in tokens)
            {
                if (assigned.Contains(token.Id))
                    continue;

                var cluster = new List<Token> { token };
                assigned.Add(token.Id);

                // Find all connected tokens within radius
                var toProcess = new Queue<Token>();
                toProcess.Enqueue(token);

                while (toProcess.Count > 0)
                {
                    var current = toProcess.Dequeue();
                    var nearby = index.FindTokensInRange(current.Position, clusterRadius);

                    foreach (var neighbor in nearby)
                    {
                        if (!assigned.Contains(neighbor.Id))
                        {
                            cluster.Add(neighbor);
                            assigned.Add(neighbor.Id);
                            toProcess.Enqueue(neighbor);
                        }
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        /// <summary>
        /// Calculates density of tokens in a region
        /// </summary>
        public static float CalculateDensity(this SpatialIndex index,
            Vector3Int center, float radius)
        {
            var tokens = index.FindTokensInRange(center, radius);
            float volume = (4.0f / 3.0f) * (float)Math.PI * radius * radius * radius;
            return tokens.Count / volume;
        }

        /// <summary>
        /// Finds regions of high token density
        /// </summary>
        public static List<Vector3Int> FindDenseRegions(this SpatialIndex index,
            int gridWidth, int gridHeight, int gridDepth,
            float sampleRadius, float densityThreshold, int sampleStride = 10)
        {
            var denseRegions = new List<Vector3Int>();

            for (int x = 0; x < gridWidth; x += sampleStride)
            {
                for (int y = 0; y < gridHeight; y += sampleStride)
                {
                    for (int z = 0; z < gridDepth; z += sampleStride)
                    {
                        var position = new Vector3Int(x, y, z);
                        float density = index.CalculateDensity(position, sampleRadius);

                        if (density >= densityThreshold)
                        {
                            denseRegions.Add(position);
                        }
                    }
                }
            }

            return denseRegions;
        }
    }
}

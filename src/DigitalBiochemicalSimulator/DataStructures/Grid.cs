using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.DataStructures
{
    /// <summary>
    /// 3D grid system for spatial partitioning and token management.
    /// Based on section 3.2 of the design specification.
    /// Thread-safe for concurrent access.
    /// </summary>
    public class Grid
    {
        public Vector3Int Dimensions { get; private set; }
        public int Width => Dimensions.X;
        public int Height => Dimensions.Y;
        public int Depth => Dimensions.Z;

        private Cell[,,] _cells;
        private readonly ConcurrentDictionary<Vector3Int, byte> _activeCells;
        private readonly ReaderWriterLockSlim _cellLock;

        public Grid(int width, int height, int depth, int cellCapacity = 1000)
        {
            Dimensions = new Vector3Int(width, height, depth);
            _cells = new Cell[width, height, depth];
            _activeCells = new ConcurrentDictionary<Vector3Int, byte>();
            _cellLock = new ReaderWriterLockSlim();

            InitializeCells(cellCapacity);
        }

        private void InitializeCells(int cellCapacity)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        var position = new Vector3Int(x, y, z);
                        _cells[x, y, z] = new Cell(position, cellCapacity);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a cell at the specified position (thread-safe for reads)
        /// </summary>
        public Cell? GetCell(Vector3Int position)
        {
            if (!IsValidPosition(position))
                return null;

            _cellLock.EnterReadLock();
            try
            {
                return _cells[position.X, position.Y, position.Z];
            }
            finally
            {
                _cellLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a cell at the specified coordinates (thread-safe for reads)
        /// </summary>
        public Cell? GetCell(int x, int y, int z)
        {
            return GetCell(new Vector3Int(x, y, z));
        }

        /// <summary>
        /// Checks if a position is within grid bounds
        /// </summary>
        public bool IsValidPosition(Vector3Int position)
        {
            return position.X >= 0 && position.X < Width &&
                   position.Y >= 0 && position.Y < Height &&
                   position.Z >= 0 && position.Z < Depth;
        }

        /// <summary>
        /// Adds a token to the grid at its current position (thread-safe)
        /// </summary>
        public bool AddToken(Token token)
        {
            if (token == null)
                return false;

            if (!IsValidPosition(token.Position))
                return false;

            _cellLock.EnterWriteLock();
            try
            {
                var cell = _cells[token.Position.X, token.Position.Y, token.Position.Z];
                bool success = cell.AddToken(token);
                if (success)
                {
                    _activeCells.TryAdd(cell.Position, 0);
                }
                return success;
            }
            finally
            {
                _cellLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes a token from the grid (thread-safe)
        /// </summary>
        public bool RemoveToken(Token token)
        {
            if (token == null)
                return false;

            if (!IsValidPosition(token.Position))
                return false;

            _cellLock.EnterWriteLock();
            try
            {
                var cell = _cells[token.Position.X, token.Position.Y, token.Position.Z];
                bool success = cell.RemoveToken(token);
                if (success && cell.IsEmpty)
                {
                    _activeCells.TryRemove(cell.Position, out _);
                }
                return success;
            }
            finally
            {
                _cellLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Moves a token from one position to another (thread-safe)
        /// </summary>
        public bool MoveToken(Token token, Vector3Int newPosition)
        {
            if (token == null)
                return false;

            if (!IsValidPosition(newPosition) || !IsValidPosition(token.Position))
                return false;

            _cellLock.EnterWriteLock();
            try
            {
                var oldCell = _cells[token.Position.X, token.Position.Y, token.Position.Z];
                var newCell = _cells[newPosition.X, newPosition.Y, newPosition.Z];

                // Check if new cell can accept the token
                if (!newCell.CanAcceptToken(token))
                    return false;

                // Move the token
                oldCell.RemoveToken(token);
                newCell.AddToken(token);

                // Update active cells
                if (oldCell.IsEmpty)
                    _activeCells.TryRemove(oldCell.Position, out _);
                _activeCells.TryAdd(newCell.Position, 0);

                return true;
            }
            finally
            {
                _cellLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the 8 horizontal neighbors + 1 below (9 total) for a cell
        /// Based on design spec section 3.2.2
        /// </summary>
        public List<Cell> GetNeighbors(Vector3Int position)
        {
            var neighbors = new List<Cell>();

            // 8 horizontal neighbors (same Z level)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue; // Skip center

                    var neighborPos = new Vector3Int(
                        position.X + dx,
                        position.Y + dy,
                        position.Z
                    );

                    var neighbor = GetCell(neighborPos);
                    if (neighbor != null)
                        neighbors.Add(neighbor);
                }
            }

            // 1 below
            var belowPos = new Vector3Int(position.X, position.Y, position.Z - 1);
            var belowCell = GetCell(belowPos);
            if (belowCell != null)
                neighbors.Add(belowCell);

            return neighbors;
        }

        /// <summary>
        /// Gets the neighbor with the lowest total mass
        /// </summary>
        public Cell? GetLowestMassNeighbor(Vector3Int position)
        {
            var neighbors = GetNeighbors(position);
            if (neighbors.Count == 0)
                return null;

            return neighbors.OrderBy(c => c.TotalMass).First();
        }

        /// <summary>
        /// Gets all tokens in the grid (thread-safe)
        /// </summary>
        public List<Token> GetAllTokens()
        {
            var allTokens = new List<Token>();

            _cellLock.EnterReadLock();
            try
            {
                foreach (var cellPos in _activeCells.Keys)
                {
                    var cell = _cells[cellPos.X, cellPos.Y, cellPos.Z];
                    if (cell != null)
                        allTokens.AddRange(cell.Tokens);
                }
            }
            finally
            {
                _cellLock.ExitReadLock();
            }

            return allTokens;
        }

        /// <summary>
        /// Gets tokens within a radius of a position (thread-safe)
        /// </summary>
        public List<Token> GetTokensInRadius(Vector3Int center, int radius)
        {
            var tokens = new List<Token>();

            _cellLock.EnterReadLock();
            try
            {
                for (int x = Math.Max(0, center.X - radius); x <= Math.Min(Width - 1, center.X + radius); x++)
                {
                    for (int y = Math.Max(0, center.Y - radius); y <= Math.Min(Height - 1, center.Y + radius); y++)
                    {
                        for (int z = Math.Max(0, center.Z - radius); z <= Math.Min(Depth - 1, center.Z + radius); z++)
                        {
                            var pos = new Vector3Int(x, y, z);
                            if (center.ManhattanDistance(pos) <= radius)
                            {
                                var cell = _cells[x, y, z];
                                if (cell != null)
                                    tokens.AddRange(cell.Tokens);
                            }
                        }
                    }
                }
            }
            finally
            {
                _cellLock.ExitReadLock();
            }

            return tokens;
        }

        /// <summary>
        /// Marks cells in mutation zone based on altitude (thread-safe)
        /// </summary>
        public void UpdateMutationZone(int mutationRange)
        {
            int mutationThreshold = Depth - mutationRange;

            _cellLock.EnterWriteLock();
            try
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int z = 0; z < Depth; z++)
                        {
                            _cells[x, y, z].IsInMutationZone = z >= mutationThreshold;
                        }
                    }
                }
            }
            finally
            {
                _cellLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets active cells (thread-safe snapshot)
        /// </summary>
        public HashSet<Vector3Int> ActiveCells
        {
            get
            {
                return new HashSet<Vector3Int>(_activeCells.Keys);
            }
        }

        /// <summary>
        /// Clears all tokens from the grid (thread-safe)
        /// </summary>
        public void Clear()
        {
            _cellLock.EnterWriteLock();
            try
            {
                foreach (var cellPos in _activeCells.Keys)
                {
                    var cell = _cells[cellPos.X, cellPos.Y, cellPos.Z];
                    cell?.Clear();
                }
                _activeCells.Clear();
            }
            finally
            {
                _cellLock.ExitWriteLock();
            }
        }

        public override string ToString()
        {
            int totalTokens = 0;
            int activeCellCount = 0;

            _cellLock.EnterReadLock();
            try
            {
                activeCellCount = _activeCells.Count;
                totalTokens = _activeCells.Keys.Sum(pos => _cells[pos.X, pos.Y, pos.Z]?.Tokens.Count ?? 0);
            }
            finally
            {
                _cellLock.ExitReadLock();
            }

            return $"Grid({Width}x{Height}x{Depth}, ActiveCells:{activeCellCount}, TotalTokens:{totalTokens})";
        }
    }
}

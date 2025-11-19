using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.DataStructures
{
    /// <summary>
    /// 3D grid system for spatial partitioning and token management.
    /// Based on section 3.2 of the design specification.
    /// </summary>
    public class Grid
    {
        public Vector3Int Dimensions { get; private set; }
        public int Width => Dimensions.X;
        public int Height => Dimensions.Y;
        public int Depth => Dimensions.Z;

        private Cell[,,] _cells;
        public HashSet<Vector3Int> ActiveCells { get; private set; }

        public Grid(int width, int height, int depth, int cellCapacity = 1000)
        {
            Dimensions = new Vector3Int(width, height, depth);
            _cells = new Cell[width, height, depth];
            ActiveCells = new HashSet<Vector3Int>();

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
        /// Gets a cell at the specified position
        /// </summary>
        public Cell? GetCell(Vector3Int position)
        {
            if (!IsValidPosition(position))
                return null;

            return _cells[position.X, position.Y, position.Z];
        }

        /// <summary>
        /// Gets a cell at the specified coordinates
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
        /// Adds a token to the grid at its current position
        /// </summary>
        public bool AddToken(Token token)
        {
            var cell = GetCell(token.Position);
            if (cell == null)
                return false;

            bool success = cell.AddToken(token);
            if (success && !ActiveCells.Contains(cell.Position))
            {
                ActiveCells.Add(cell.Position);
            }

            return success;
        }

        /// <summary>
        /// Removes a token from the grid
        /// </summary>
        public bool RemoveToken(Token token)
        {
            var cell = GetCell(token.Position);
            if (cell == null)
                return false;

            bool success = cell.RemoveToken(token);
            if (success && cell.IsEmpty)
            {
                ActiveCells.Remove(cell.Position);
            }

            return success;
        }

        /// <summary>
        /// Moves a token from one position to another
        /// </summary>
        public bool MoveToken(Token token, Vector3Int newPosition)
        {
            if (!IsValidPosition(newPosition))
                return false;

            var oldCell = GetCell(token.Position);
            var newCell = GetCell(newPosition);

            if (oldCell == null || newCell == null)
                return false;

            // Check if new cell can accept the token
            if (!newCell.CanAcceptToken(token))
                return false;

            // Move the token
            oldCell.RemoveToken(token);
            newCell.AddToken(token);

            // Update active cells
            if (oldCell.IsEmpty)
                ActiveCells.Remove(oldCell.Position);
            if (!ActiveCells.Contains(newCell.Position))
                ActiveCells.Add(newCell.Position);

            return true;
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
        /// Gets all tokens in the grid
        /// </summary>
        public List<Token> GetAllTokens()
        {
            var allTokens = new List<Token>();
            foreach (var cellPos in ActiveCells)
            {
                var cell = GetCell(cellPos);
                if (cell != null)
                    allTokens.AddRange(cell.Tokens);
            }
            return allTokens;
        }

        /// <summary>
        /// Gets tokens within a radius of a position
        /// </summary>
        public List<Token> GetTokensInRadius(Vector3Int center, int radius)
        {
            var tokens = new List<Token>();

            for (int x = Math.Max(0, center.X - radius); x <= Math.Min(Width - 1, center.X + radius); x++)
            {
                for (int y = Math.Max(0, center.Y - radius); y <= Math.Min(Height - 1, center.Y + radius); y++)
                {
                    for (int z = Math.Max(0, center.Z - radius); z <= Math.Min(Depth - 1, center.Z + radius); z++)
                    {
                        var pos = new Vector3Int(x, y, z);
                        if (center.ManhattanDistance(pos) <= radius)
                        {
                            var cell = GetCell(pos);
                            if (cell != null)
                                tokens.AddRange(cell.Tokens);
                        }
                    }
                }
            }

            return tokens;
        }

        /// <summary>
        /// Marks cells in mutation zone based on altitude
        /// </summary>
        public void UpdateMutationZone(int mutationRange)
        {
            int mutationThreshold = Depth - mutationRange;

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

        public override string ToString()
        {
            int totalTokens = ActiveCells.Sum(pos => GetCell(pos)?.Tokens.Count ?? 0);
            return $"Grid({Width}x{Height}x{Depth}, ActiveCells:{ActiveCells.Count}, TotalTokens:{totalTokens})";
        }
    }
}

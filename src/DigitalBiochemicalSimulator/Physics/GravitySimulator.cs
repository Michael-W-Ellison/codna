using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Physics
{
    /// <summary>
    /// Simulates gravity and settling behavior for tokens.
    /// Based on section 3.3.2 of the design specification.
    /// </summary>
    public class GravitySimulator
    {
        private SimulationConfig _config;
        private Grid _grid;
        private readonly Random _random = new Random();

        public GravitySimulator(SimulationConfig config, Grid grid)
        {
            _config = config;
            _grid = grid;
        }

        /// <summary>
        /// Applies gravity to all tokens
        /// Tokens always seek lowest available position
        /// Checks 8 horizontal neighbors + 1 below (9 total)
        /// </summary>
        public void ApplyGravity(List<Token> tokens)
        {
            if (!_config.GravityEnabled)
                return;

            // Process tokens from bottom to top to avoid conflicts
            var sortedTokens = tokens
                .Where(t => t.IsActive && t.IsFalling)
                .OrderBy(t => t.Position.Z)
                .ToList();

            foreach (var token in sortedTokens)
            {
                ApplyGravityToToken(token);
            }
        }

        /// <summary>
        /// Applies gravity to a single token
        /// </summary>
        private void ApplyGravityToToken(Token token)
        {
            // Only falling tokens are affected by gravity settling
            if (!token.IsFalling)
                return;

            // Already at bottom
            if (token.Position.Z <= 0)
                return;

            var currentCell = _grid.GetCell(token.Position);
            if (currentCell == null)
                return;

            // Seek the lowest available position: if the cell below can take the token,
            // it falls straight down (MotionController moves it)
            var below = _grid.GetCell(token.Position + new Vector3Int(0, 0, -1));
            if (below == null || below.CanAcceptToken(token))
                return;

            // Blocked below: slide to a horizontal neighbor that has room and open space beneath it,
            // chosen at random so tokens do not drift in one direction
            var candidates = _grid.GetNeighbors(token.Position)
                .Where(c => c.Position.Z == token.Position.Z && c.CanAcceptToken(token))
                .Where(c =>
                {
                    var beneath = _grid.GetCell(c.Position + new Vector3Int(0, 0, -1));
                    return beneath != null && beneath.CanAcceptToken(token);
                })
                .ToList();

            if (candidates.Count > 0)
            {
                _grid.MoveToken(token, candidates[_random.Next(candidates.Count)].Position);
            }
        }

        /// <summary>
        /// Handles cell overflow by redistributing tokens
        /// Based on section 3.2.2 distributeOverflow algorithm
        /// </summary>
        public void RedistributeOverflow(Cell cell)
        {
            if (!cell.IsOverflowing)
                return;

            var neighbors = _grid.GetNeighbors(cell.Position);
            if (neighbors.Count == 0)
                return;

            // Get tokens to redistribute (prioritize falling tokens)
            var tokensToMove = cell.GetRedistributableTokens();

            // Sort neighbors by lowest mass first (ties in random order, so overflow does not drift one way)
            var sortedNeighbors = neighbors.OrderBy(c => c.TotalMass).ThenBy(_ => _random.Next()).ToList();

            foreach (var token in tokensToMove)
            {
                // Stop if cell is no longer overflowing
                if (!cell.IsOverflowing)
                    break;

                // Find first neighbor that can accept token
                foreach (var neighbor in sortedNeighbors)
                {
                    if (neighbor.CanAcceptToken(token))
                    {
                        _grid.MoveToken(token, neighbor.Position);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles overflow for all overflowing cells
        /// </summary>
        public void HandleOverflow()
        {
            var overflowingCells = new List<Cell>();

            // Find all overflowing cells
            foreach (var cellPos in _grid.ActiveCells)
            {
                var cell = _grid.GetCell(cellPos);
                if (cell != null && cell.IsOverflowing)
                {
                    overflowingCells.Add(cell);
                }
            }

            // Redistribute tokens
            foreach (var cell in overflowingCells)
            {
                RedistributeOverflow(cell);
            }
        }
    }
}

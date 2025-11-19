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

            // Get neighbors (8 horizontal + 1 below)
            var neighbors = _grid.GetNeighbors(token.Position);
            if (neighbors.Count == 0)
                return;

            // Find lowest mass neighbor
            var lowestNeighbor = neighbors.OrderBy(c => c.TotalMass).First();

            // If lowest neighbor has less mass and can accept token, move there
            if (lowestNeighbor.TotalMass < currentCell.TotalMass &&
                lowestNeighbor.CanAcceptToken(token))
            {
                _grid.MoveToken(token, lowestNeighbor.Position);
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

            // Sort neighbors by lowest mass first
            var sortedNeighbors = neighbors.OrderBy(c => c.TotalMass).ToList();

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

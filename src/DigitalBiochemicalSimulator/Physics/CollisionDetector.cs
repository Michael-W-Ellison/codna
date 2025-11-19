using System;
using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Physics
{
    /// <summary>
    /// Detects and handles collisions between tokens.
    /// Based on section 3.3.1 collision rules of the design specification.
    /// </summary>
    public class CollisionDetector
    {
        private SimulationConfig _config;
        private Grid _grid;
        private Random _random;

        public CollisionDetector(SimulationConfig config, Grid grid)
        {
            _config = config;
            _grid = grid;
            _random = new Random();
        }

        /// <summary>
        /// Checks for collisions in all active cells
        /// </summary>
        public void DetectAndHandleCollisions()
        {
            foreach (var cellPos in _grid.ActiveCells)
            {
                var cell = _grid.GetCell(cellPos);
                if (cell == null || cell.Tokens.Count < 2)
                    continue;

                CheckCellCollisions(cell);
            }
        }

        /// <summary>
        /// Checks for collisions within a cell
        /// Rising meets Sinking collision logic from spec 3.3.1
        /// </summary>
        private void CheckCellCollisions(Cell cell)
        {
            var risingTokens = new List<Token>();
            var sinkingTokens = new List<Token>();

            // Separate tokens by movement direction
            foreach (var token in cell.Tokens)
            {
                if (token.IsRising)
                    risingTokens.Add(token);
                else if (token.IsFalling)
                    sinkingTokens.Add(token);
            }

            // Check for rising-sinking collisions
            foreach (var rising in risingTokens)
            {
                foreach (var sinking in sinkingTokens)
                {
                    HandleRisingSinkingCollision(rising, sinking, cell);
                }
            }
        }

        /// <summary>
        /// Handles collision between rising and sinking token
        /// Per spec: Rising loses 1 energy, sinking displaced to random adjacent cell
        /// </summary>
        private void HandleRisingSinkingCollision(Token rising, Token sinking, Cell cell)
        {
            // Rising token loses 1 energy
            if (rising.Energy > 0)
            {
                rising.Energy -= 1;
            }

            // Sinking token displaced to random adjacent cell
            DisplaceSinkingToken(sinking, cell.Position);
        }

        /// <summary>
        /// Displaces a sinking token to a random adjacent cell (1 of 8 horizontal)
        /// </summary>
        private void DisplaceSinkingToken(Token token, Vector3Int currentPos)
        {
            // Get 8 horizontal neighbors (same Z level)
            var neighbors = GetHorizontalNeighbors(currentPos);

            if (neighbors.Count == 0)
                return;

            // Select random neighbor
            var randomNeighbor = neighbors[_random.Next(neighbors.Count)];

            // Try to move token to random neighbor
            if (randomNeighbor.CanAcceptToken(token))
            {
                _grid.MoveToken(token, randomNeighbor.Position);
            }
        }

        /// <summary>
        /// Gets the 8 horizontal neighbors (same Z level)
        /// </summary>
        private List<Cell> GetHorizontalNeighbors(Vector3Int position)
        {
            var neighbors = new List<Cell>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue; // Skip center

                    var neighborPos = new Vector3Int(
                        position.X + dx,
                        position.Y + dy,
                        position.Z  // Same Z level
                    );

                    var neighbor = _grid.GetCell(neighborPos);
                    if (neighbor != null)
                        neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Checks if two tokens are colliding (same position)
        /// </summary>
        public bool AreColliding(Token token1, Token token2)
        {
            return token1.Position == token2.Position;
        }
    }
}

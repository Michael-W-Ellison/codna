using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Physics
{
    /// <summary>
    /// Controls token movement (rising and falling behavior).
    /// Based on section 3.3.3 of the design specification.
    /// </summary>
    public class MotionController
    {
        private SimulationConfig _config;
        private Grid _grid;

        public MotionController(SimulationConfig config, Grid grid)
        {
            _config = config;
            _grid = grid;
        }

        /// <summary>
        /// Updates movement for all tokens
        /// Rising tokens: move up, lose energy
        /// Falling tokens: move down, no energy cost
        /// Based on section 3.3.3 updateTokenMotion algorithm
        /// </summary>
        public void UpdateTokenMotion(List<Token> tokens)
        {
            if (tokens == null)
                return;

            foreach (var token in tokens)
            {
                if (token == null || !token.IsActive)
                    continue;

                // Skip bonded tokens (they move with their chain head)
                if (token.IsInChain && token.ChainHead != token)
                    continue;

                UpdateSingleTokenMotion(token);
            }
        }

        /// <summary>
        /// Updates motion for a single token
        /// </summary>
        private void UpdateSingleTokenMotion(Token token)
        {
            if (token == null)
                return;

            if (token.IsRising)
            {
                // Rising phase: move up
                MoveTokenUp(token);
            }
            else if (token.IsFalling)
            {
                // Falling phase: move down
                MoveTokenDown(token);
            }
        }

        /// <summary>
        /// Moves token upward (costs energy based on viscosity)
        /// </summary>
        private void MoveTokenUp(Token token)
        {
            // Check if enough energy to overcome viscosity
            if (token.Energy < _config.EnvironmentViscosity)
            {
                token.Energy = 0; // Transition to falling
                return;
            }

            var newPosition = token.Position + new Vector3Int(0, 0, _config.RiseRate);

            // Check if new position is valid
            if (!_grid.IsValidPosition(newPosition))
            {
                token.Energy = 0; // Hit top of grid
                return;
            }

            // Attempt to move
            bool moved = _grid.MoveToken(token, newPosition);

            if (moved)
            {
                token.Velocity = Vector3Int.Up;
            }
            else
            {
                // Couldn't move (cell full), lose some energy
                token.Energy -= _config.EnvironmentViscosity;
                token.Velocity = Vector3Int.Zero;
            }
        }

        /// <summary>
        /// Moves token downward (no energy cost)
        /// </summary>
        private void MoveTokenDown(Token token)
        {
            // Check if at bottom
            if (token.Position.Z <= 0)
            {
                token.Velocity = Vector3Int.Zero;
                return;
            }

            var newPosition = token.Position + new Vector3Int(0, 0, -_config.FallRate);

            // Ensure we don't go below 0
            if (newPosition.Z < 0)
                newPosition = new Vector3Int(newPosition.X, newPosition.Y, 0);

            // Attempt to move
            bool moved = _grid.MoveToken(token, newPosition);

            if (moved)
            {
                token.Velocity = Vector3Int.Down;
            }
            else
            {
                // Couldn't move down (resting on something)
                token.Velocity = Vector3Int.Zero;
            }
        }

        /// <summary>
        /// Stops a token's movement
        /// </summary>
        public void StopToken(Token token)
        {
            token.Velocity = Vector3Int.Zero;
        }

        /// <summary>
        /// Sets a token's velocity
        /// </summary>
        public void SetVelocity(Token token, Vector3Int velocity)
        {
            token.Velocity = velocity;
        }
    }
}

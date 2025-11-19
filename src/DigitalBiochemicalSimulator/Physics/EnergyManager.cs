using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Physics
{
    /// <summary>
    /// Manages energy for tokens including depletion and bonding energy.
    /// Based on section 3.3.1 of the design specification.
    /// </summary>
    public class EnergyManager
    {
        private SimulationConfig _config;

        public EnergyManager(SimulationConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Updates energy for all active tokens
        /// Energy decreases by 1 per tick when token is rising
        /// </summary>
        public void UpdateTokenEnergy(List<Token> tokens)
        {
            if (tokens == null)
                return;

            foreach (var token in tokens)
            {
                if (token == null || !token.IsActive)
                    continue;

                // Rising tokens lose energy
                if (token.IsRising)
                {
                    token.Energy -= _config.EnergyPerTick;
                    if (token.Energy < 0)
                        token.Energy = 0;
                }

                // Falling tokens don't lose energy (as per spec)
            }
        }

        /// <summary>
        /// Distributes energy to a token chain after bonding
        /// Energy formula: (numTokens - 1) * energyPerBond
        /// Based on section 4.3 of the design specification
        /// </summary>
        public void DistributeChainEnergy(TokenChain chain)
        {
            if (chain == null || chain.Length < 2)
                return;

            // Calculate bonding energy: (chain.length - 1) * energyPerBond
            int energyGain = (chain.Length - 1) * _config.EnergyPerBond;

            // Distribute evenly across all tokens in chain
            int energyPerToken = energyGain / chain.Length;
            int remainder = energyGain % chain.Length;

            foreach (var token in chain.Tokens)
            {
                token.Energy += energyPerToken;

                // Give head token any remainder
                if (token == chain.Head && remainder > 0)
                {
                    token.Energy += remainder;
                }
            }

            chain.TotalEnergy += energyGain;
        }

        /// <summary>
        /// Distributes energy from one token to another (for bonding)
        /// </summary>
        public void TransferEnergy(Token from, Token to, int amount)
        {
            if (from == null || to == null)
                return;

            if (from.Energy < amount)
                amount = from.Energy;

            from.Energy -= amount;
            to.Energy += amount;
        }

        /// <summary>
        /// Gets the total energy in a list of tokens
        /// </summary>
        public int GetTotalEnergy(List<Token> tokens)
        {
            if (tokens == null)
                return 0;

            int total = 0;
            foreach (var token in tokens)
            {
                if (token != null)
                    total += token.Energy;
            }
            return total;
        }

        /// <summary>
        /// Gets average energy across tokens
        /// </summary>
        public float GetAverageEnergy(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return 0;

            return (float)GetTotalEnergy(tokens) / tokens.Count;
        }
    }
}

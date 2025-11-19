using System;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Grammar;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Calculates chain stability using 5 factors.
    /// Based on section 4.4 of the design specification.
    ///
    /// Stability Factors:
    /// 1. Bond strength (average of all bonds)
    /// 2. Grammar validity (is the chain syntactically correct?)
    /// 3. Age/consistency (how long has chain been stable?)
    /// 4. Damage levels (average damage across tokens)
    /// 5. Energy reserves (total energy vs. maintenance cost)
    /// </summary>
    public class ChainStabilityCalculator
    {
        private readonly BondRulesEngine _rulesEngine;
        private readonly BondStrengthCalculator _strengthCalculator;

        // Stability parameters
        private const float VALID_GRAMMAR_MULTIPLIER = 1.2f;    // 20% bonus for valid grammar
        private const float INVALID_GRAMMAR_MULTIPLIER = 0.5f;  // 50% penalty for invalid grammar
        private const float MAX_AGE_BONUS = 0.5f;               // Max 50% age bonus
        private const long AGE_BONUS_TICKS = 100;               // Ticks to reach max age bonus
        private const int ENERGY_PER_TOKEN_REQUIRED = 10;       // Min energy per token for full stability

        public ChainStabilityCalculator(BondRulesEngine rulesEngine, BondStrengthCalculator strengthCalculator)
        {
            _rulesEngine = rulesEngine;
            _strengthCalculator = strengthCalculator;
        }

        /// <summary>
        /// Calculates the stability score for a token chain
        /// Returns value from 0.0 (completely unstable) to 1.0+ (very stable)
        /// </summary>
        public float CalculateStability(TokenChain chain, long currentTick)
        {
            if (chain == null || chain.Length == 0)
                return 0.0f;

            float stability = 1.0f;

            // Factor 1: Bond strength (base multiplier)
            float bondFactor = CalculateBondFactor(chain);
            stability *= bondFactor;

            // Factor 2: Grammar validity
            float grammarFactor = CalculateGrammarFactor(chain);
            stability *= grammarFactor;

            // Factor 3: Age/consistency bonus (additive)
            float ageFactor = CalculateAgeFactor(chain, currentTick);
            stability += ageFactor;

            // Factor 4: Damage penalty (multiplicative)
            float damageFactor = CalculateDamageFactor(chain);
            stability *= damageFactor;

            // Factor 5: Energy reserves (multiplicative)
            float energyFactor = CalculateEnergyFactor(chain);
            stability *= energyFactor;

            // Clamp to valid range (allow up to 2.0 for extremely stable chains)
            return Math.Clamp(stability, 0.0f, 2.0f);
        }

        /// <summary>
        /// Factor 1: Average bond strength
        /// </summary>
        private float CalculateBondFactor(TokenChain chain)
        {
            if (chain.Length < 2)
                return 1.0f; // Single tokens have full bond factor

            // Use pre-calculated average bond strength if available
            if (chain.AverageBondStrength > 0)
                return chain.AverageBondStrength;

            // Calculate from tokens if needed
            if (_strengthCalculator == null || chain.Tokens == null || chain.Tokens.Count < 2)
                return 0.5f; // Default medium strength

            float totalStrength = 0.0f;
            int bondCount = 0;

            for (int i = 0; i < chain.Tokens.Count - 1; i++)
            {
                var token1 = chain.Tokens[i];
                var token2 = chain.Tokens[i + 1];

                float strength = _strengthCalculator.CalculateBondStrength(token1, token2);
                totalStrength += strength;
                bondCount++;
            }

            return bondCount > 0 ? totalStrength / bondCount : 0.5f;
        }

        /// <summary>
        /// Factor 2: Grammar validity (does chain form valid syntax?)
        /// </summary>
        private float CalculateGrammarFactor(TokenChain chain)
        {
            // Use pre-validated flag if available
            if (chain.IsValid)
                return VALID_GRAMMAR_MULTIPLIER;

            // Validate using grammar engine if available
            if (_rulesEngine != null && chain.Tokens != null && chain.Tokens.Count > 0)
            {
                bool isValid = _rulesEngine.MatchesGrammar(chain.Tokens);
                chain.IsValid = isValid;
                return isValid ? VALID_GRAMMAR_MULTIPLIER : INVALID_GRAMMAR_MULTIPLIER;
            }

            // Default to invalid if we can't check
            return INVALID_GRAMMAR_MULTIPLIER;
        }

        /// <summary>
        /// Factor 3: Age bonus (stable chains get bonus over time)
        /// </summary>
        private float CalculateAgeFactor(TokenChain chain, long currentTick)
        {
            long age = currentTick - chain.LastModifiedTick;
            chain.TicksSinceModified = age;

            // Linear scaling up to MAX_AGE_BONUS
            float ageBonus = Math.Min((float)age / AGE_BONUS_TICKS, 1.0f) * MAX_AGE_BONUS;

            return ageBonus;
        }

        /// <summary>
        /// Factor 4: Damage penalty (damaged tokens reduce stability)
        /// </summary>
        private float CalculateDamageFactor(TokenChain chain)
        {
            if (chain.Tokens == null || chain.Tokens.Count == 0)
                return 1.0f;

            // Calculate average damage across all tokens
            float avgDamage = chain.Tokens.Average(t => t.DamageLevel);

            // Inverse: no damage = 1.0, full damage = 0.0
            return 1.0f - avgDamage;
        }

        /// <summary>
        /// Factor 5: Energy reserves (chain needs energy to maintain bonds)
        /// </summary>
        private float CalculateEnergyFactor(TokenChain chain)
        {
            if (chain.Length == 0)
                return 0.0f;

            int totalEnergy = chain.TotalEnergy;
            int requiredEnergy = chain.Length * ENERGY_PER_TOKEN_REQUIRED;

            // Calculate ratio (0.0 to 1.0+)
            float energyRatio = (float)totalEnergy / requiredEnergy;

            // Clamp to max of 1.0 (having extra energy doesn't add stability)
            return Math.Min(energyRatio, 1.0f);
        }

        /// <summary>
        /// Checks if a chain is stable enough to persist
        /// </summary>
        public bool IsChainStable(TokenChain chain, long currentTick, float minimumStability = 0.3f)
        {
            float stability = CalculateStability(chain, currentTick);
            return stability >= minimumStability;
        }

        /// <summary>
        /// Predicts if a chain will remain stable for N ticks
        /// </summary>
        public bool PredictStability(TokenChain chain, long currentTick, int futureTicks)
        {
            // Guard against invalid chains
            if (chain == null || chain.Length == 0 || chain.Tokens == null || chain.Tokens.Count == 0)
                return false;

            // Calculate current stability
            float currentStability = CalculateStability(chain, currentTick);

            // Estimate energy loss over time
            int energyLossPerTick = chain.Length; // Approximate
            int futureEnergy = Math.Max(0, chain.TotalEnergy - (energyLossPerTick * futureTicks));

            // Estimate damage increase
            float damageIncrease = futureTicks * 0.01f; // 1% per tick at high altitude
            float futureAvgDamage = Math.Min(1.0f, chain.Tokens.Average(t => t.DamageLevel) + damageIncrease);

            // Recalculate stability with predictions
            float futureStability = currentStability;
            futureStability *= (1.0f - futureAvgDamage); // Damage factor

            // Prevent division by zero
            int requiredEnergy = chain.Length * ENERGY_PER_TOKEN_REQUIRED;
            if (requiredEnergy > 0)
            {
                futureStability *= Math.Min(1.0f, (float)futureEnergy / requiredEnergy); // Energy factor
            }

            return futureStability >= 0.3f;
        }

        /// <summary>
        /// Gets a detailed breakdown of stability factors
        /// </summary>
        public StabilityBreakdown GetStabilityBreakdown(TokenChain chain, long currentTick)
        {
            return new StabilityBreakdown
            {
                TotalStability = CalculateStability(chain, currentTick),
                BondStrength = CalculateBondFactor(chain),
                GrammarValidity = CalculateGrammarFactor(chain),
                AgeBonus = CalculateAgeFactor(chain, currentTick),
                DamagePenalty = 1.0f - CalculateDamageFactor(chain),
                EnergyLevel = CalculateEnergyFactor(chain)
            };
        }
    }

    /// <summary>
    /// Detailed breakdown of stability factors
    /// </summary>
    public class StabilityBreakdown
    {
        public float TotalStability { get; set; }
        public float BondStrength { get; set; }
        public float GrammarValidity { get; set; }
        public float AgeBonus { get; set; }
        public float DamagePenalty { get; set; }
        public float EnergyLevel { get; set; }

        public override string ToString()
        {
            return $"Stability: {TotalStability:F2} " +
                   $"(Bonds:{BondStrength:F2}, Grammar:{GrammarValidity:F2}, " +
                   $"Age:+{AgeBonus:F2}, Damage:-{DamagePenalty:F2}, Energy:{EnergyLevel:F2})";
        }
    }
}

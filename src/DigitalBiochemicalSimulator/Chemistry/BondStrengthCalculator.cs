using System;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Grammar;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Calculates bond strength between tokens using electronegativity model.
    /// Based on section 3.6 of the design specification.
    ///
    /// Bond strength is calculated using multiple factors:
    /// 1. Grammar compatibility (from BondRulesEngine)
    /// 2. Electronegativity difference
    /// 3. Token energy levels
    /// 4. Spatial proximity
    /// 5. Existing bonding state
    /// </summary>
    public class BondStrengthCalculator
    {
        private readonly BondRulesEngine _rulesEngine;

        public BondStrengthCalculator(BondRulesEngine rulesEngine)
        {
            _rulesEngine = rulesEngine;
        }

        /// <summary>
        /// Calculates the total bond strength between two tokens
        /// Returns value from 0.0 (no bond) to 1.0 (maximum strength)
        /// </summary>
        public float CalculateBondStrength(Token token1, Token token2)
        {
            if (token1 == null || token2 == null)
                return 0.0f;

            if (!token1.IsActive || !token2.IsActive)
                return 0.0f;

            // Factor 1: Grammar compatibility (0.0 - 1.0)
            float grammarFactor = GetGrammarCompatibility(token1, token2);
            if (grammarFactor < 0.1f)
                return 0.0f; // Not compatible according to grammar

            // Factor 2: Electronegativity-based strength (0.0 - 1.0)
            float electronegativityFactor = GetElectronegativityStrength(token1, token2);

            // Factor 3: Energy factor (both tokens need sufficient energy)
            float energyFactor = GetEnergyFactor(token1, token2);

            // Factor 4: Bonding site availability (0.0 - 1.0)
            float availabilityFactor = GetBondSiteAvailability(token1, token2);

            // Factor 5: Damage penalty (0.0 - 1.0, 1.0 = no damage)
            float damageFactor = GetDamageFactor(token1, token2);

            // Combine factors with weighted average
            // Grammar and electronegativity are most important
            float totalStrength = (
                grammarFactor * 0.40f +           // 40% grammar
                electronegativityFactor * 0.30f +  // 30% electronegativity
                energyFactor * 0.15f +             // 15% energy
                availabilityFactor * 0.10f +       // 10% availability
                damageFactor * 0.05f               // 5% damage
            );

            // Clamp to valid range
            return Math.Clamp(totalStrength, 0.0f, 1.0f);
        }

        /// <summary>
        /// Calculates bond strength from grammar rules
        /// </summary>
        private float GetGrammarCompatibility(Token token1, Token token2)
        {
            if (_rulesEngine == null)
                return 0.5f; // Default medium compatibility

            if (!_rulesEngine.CanBond(token1, token2))
                return 0.0f; // Grammar forbids this bond

            // Get the base strength from matching grammar rule
            return _rulesEngine.GetBaseBondStrength(token1, token2);
        }

        /// <summary>
        /// Calculates bond strength from electronegativity difference
        /// </summary>
        private float GetElectronegativityStrength(Token token1, Token token2)
        {
            float en1 = ElectronegativityTable.GetValue(token1.Type);
            float en2 = ElectronegativityTable.GetValue(token2.Type);
            float difference = Math.Abs(en1 - en2);

            // Small difference = stronger bond (covalent sharing)
            // Large difference = weaker bond (ionic transfer, less stable)
            // Formula: strength = 1.0 - (difference * dampening)
            float strength = 1.0f - (difference * 0.6f);

            return Math.Clamp(strength, 0.0f, 1.0f);
        }

        /// <summary>
        /// Calculates energy factor based on token energy levels
        /// </summary>
        private float GetEnergyFactor(Token token1, Token token2)
        {
            // Both tokens need energy to form bonds
            // At least 10 energy each for stable bonding
            const int MIN_ENERGY_FOR_BONDING = 10;

            int totalEnergy = token1.Energy + token2.Energy;
            int minRequired = MIN_ENERGY_FOR_BONDING * 2;

            if (totalEnergy < minRequired)
            {
                return (float)totalEnergy / minRequired; // Partial factor
            }

            // Full energy factor if both have sufficient energy
            return 1.0f;
        }

        /// <summary>
        /// Checks if tokens have available bond sites
        /// </summary>
        private float GetBondSiteAvailability(Token token1, Token token2)
        {
            // Check if both tokens have bonding capacity
            int capacity1 = token1.Metadata?.BondingCapacity ?? 2;
            int capacity2 = token2.Metadata?.BondingCapacity ?? 2;

            int used1 = token1.BondedTokens.Count;
            int used2 = token2.BondedTokens.Count;

            bool hasSpace1 = used1 < capacity1;
            bool hasSpace2 = used2 < capacity2;

            if (!hasSpace1 || !hasSpace2)
                return 0.0f; // No available sites

            // Calculate how "full" each token is
            float utilization1 = (float)used1 / capacity1;
            float utilization2 = (float)used2 / capacity2;
            float avgUtilization = (utilization1 + utilization2) / 2.0f;

            // Less utilized = better availability
            return 1.0f - avgUtilization;
        }

        /// <summary>
        /// Calculates damage penalty factor
        /// </summary>
        private float GetDamageFactor(Token token1, Token token2)
        {
            float damage1 = token1.DamageLevel;
            float damage2 = token2.DamageLevel;

            // Average damage (0.0 = no damage, 1.0 = fully damaged)
            float avgDamage = (damage1 + damage2) / 2.0f;

            // Return inverse (1.0 = no penalty, 0.0 = full penalty)
            return 1.0f - avgDamage;
        }

        /// <summary>
        /// Determines the type of bond that would form
        /// </summary>
        public BondType DetermineBondType(Token token1, Token token2)
        {
            if (_rulesEngine != null)
            {
                // Prefer grammar-specified bond type
                var grammarBondType = _rulesEngine.GetBondType(token1, token2);
                if (grammarBondType != BondType.VAN_DER_WAALS || _rulesEngine.CanBond(token1, token2))
                {
                    return grammarBondType;
                }
            }

            // Fallback to electronegativity-based determination
            return ElectronegativityTable.DetermineBondType(token1.Type, token2.Type);
        }

        /// <summary>
        /// Calculates the energy cost of forming a bond
        /// </summary>
        public int CalculateBondEnergyCost(Token token1, Token token2)
        {
            // Stronger bonds cost more energy to form
            float strength = CalculateBondStrength(token1, token2);

            // Base cost scales with strength (5-20 energy)
            const int MIN_COST = 5;
            const int MAX_COST = 20;

            return (int)(MIN_COST + (strength * (MAX_COST - MIN_COST)));
        }

        /// <summary>
        /// Calculates the energy released when a bond forms (negative cost)
        /// Some bond formations are exothermic
        /// </summary>
        public int CalculateBondEnergyRelease(Token token1, Token token2)
        {
            var bondType = DetermineBondType(token1, token2);

            // Covalent bonds release more energy when forming
            return bondType switch
            {
                BondType.COVALENT => 10,
                BondType.IONIC => 5,
                BondType.VAN_DER_WAALS => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Checks if a bond can form given current conditions
        /// </summary>
        public bool CanFormBond(Token token1, Token token2)
        {
            float strength = CalculateBondStrength(token1, token2);

            // Minimum threshold for bond formation
            const float MIN_STRENGTH_THRESHOLD = 0.3f;

            return strength >= MIN_STRENGTH_THRESHOLD;
        }

        /// <summary>
        /// Predicts if a bond would be stable
        /// </summary>
        public bool IsBondStable(Token token1, Token token2)
        {
            float strength = CalculateBondStrength(token1, token2);

            // Stable bonds require higher threshold
            const float STABILITY_THRESHOLD = 0.5f;

            return strength >= STABILITY_THRESHOLD;
        }
    }
}

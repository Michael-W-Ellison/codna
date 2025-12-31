using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Damage
{
    /// <summary>
    /// Manages altitude-based damage and metadata corruption for tokens.
    /// Based on section 4.1 of the design specification.
    ///
    /// Damage formula: BASE_DAMAGE * (altitude / MAX_HEIGHT)^EXPONENT
    /// Higher altitude = more damage
    /// </summary>
    public class DamageSystem
    {
        private readonly SimulationConfig _config;
        private readonly BondingManager _bondingManager;
        private readonly Random _random;

        // Damage parameters
        private const float BASE_DAMAGE_RATE = 0.01f;    // 1% base damage per tick at max altitude
        private const float DAMAGE_EXPONENT = 2.0f;       // Quadratic scaling
        private const float CRITICAL_DAMAGE_THRESHOLD = 0.8f; // 80% damage = critical

        // Corruption probabilities
        private const float OBFUSCATION_PROBABILITY = 0.40f;  // 40%
        private const float MUTATION_PROBABILITY = 0.35f;      // 35%
        private const float ERASURE_PROBABILITY = 0.25f;       // 25%

        public DamageSystem(SimulationConfig config, BondingManager bondingManager)
        {
            _config = config;
            _bondingManager = bondingManager;
            _random = new Random();
        }

        /// <summary>
        /// Applies altitude-based damage to a token
        /// </summary>
        public void ApplyDamage(Token token, long currentTick)
        {
            if (token == null || !token.IsActive)
                return;

            // Calculate damage rate based on altitude
            float damageRate = CalculateDamageRate(token.Position.Y);

            // Apply probabilistic damage
            if (_random.NextDouble() < damageRate)
            {
                // Increase damage level
                float damageIncrease = (float)_random.NextDouble() * 0.1f; // 0-10% increase
                token.DamageLevel = Math.Min(1.0f, token.DamageLevel + damageIncrease);
                token.IsDamaged = token.DamageLevel > 0.0f;

                // Apply metadata corruption
                ApplyCorruption(token);

                // Check if damage is critical
                if (token.DamageLevel >= CRITICAL_DAMAGE_THRESHOLD)
                {
                    HandleCriticalDamage(token, currentTick);
                }
            }
        }

        /// <summary>
        /// Calculates the damage rate based on altitude
        /// Formula: BASE_DAMAGE * (altitude / MAX_HEIGHT)^EXPONENT
        /// </summary>
        private float CalculateDamageRate(int altitude)
        {
            if (_config == null || _config.GridHeight == 0)
                return BASE_DAMAGE_RATE;

            // Normalize altitude (0.0 to 1.0)
            float normalizedAltitude = (float)altitude / _config.GridHeight;

            // Apply exponential scaling
            float scaledAltitude = (float)Math.Pow(normalizedAltitude, DAMAGE_EXPONENT);

            return BASE_DAMAGE_RATE * scaledAltitude;
        }

        /// <summary>
        /// Applies metadata corruption to a damaged token
        /// </summary>
        private void ApplyCorruption(Token token)
        {
            if (token.Metadata == null)
                return;

            // Determine corruption type based on probability
            CorruptionType corruptionType = SelectCorruptionType();

            switch (corruptionType)
            {
                case CorruptionType.OBFUSCATION:
                    ApplyObfuscation(token);
                    break;

                case CorruptionType.MUTATION:
                    ApplyMutation(token);
                    break;

                case CorruptionType.ERASURE:
                    ApplyErasure(token);
                    break;
            }
        }

        /// <summary>
        /// Selects a corruption type based on probability distribution
        /// </summary>
        private CorruptionType SelectCorruptionType()
        {
            double roll = _random.NextDouble();

            if (roll < OBFUSCATION_PROBABILITY)
                return CorruptionType.OBFUSCATION;
            else if (roll < OBFUSCATION_PROBABILITY + MUTATION_PROBABILITY)
                return CorruptionType.MUTATION;
            else
                return CorruptionType.ERASURE;
        }

        /// <summary>
        /// Obfuscates token metadata (garbles but preserves structure)
        /// </summary>
        private void ApplyObfuscation(Token token)
        {
            var metadata = token.Metadata;

            // Garble syntax category
            if (_random.NextDouble() < 0.3f)
            {
                metadata.SyntaxCategory = GarbleString(metadata.SyntaxCategory);
            }

            // Garble semantic type
            if (_random.NextDouble() < 0.3f)
            {
                metadata.SemanticType = GarbleString(metadata.SemanticType);
            }

            // Reduce electronegativity slightly
            if (_random.NextDouble() < 0.2f)
            {
                float reduction = (float)_random.NextDouble() * 0.1f;
                metadata.Electronegativity = Math.Max(0.0f, metadata.Electronegativity - reduction);
            }
        }

        /// <summary>
        /// Mutates token to a different type or value
        /// </summary>
        private void ApplyMutation(Token token)
        {
            // Mutate token value
            if (_random.NextDouble() < 0.5f)
            {
                token.Value = MutateValue(token.Type, token.Value);
            }

            // Mutate token type (dangerous!)
            if (_random.NextDouble() < 0.2f)
            {
                token.Type = MutateType(token.Type);
            }

            // Alter electronegativity significantly
            if (_random.NextDouble() < 0.4f)
            {
                float delta = ((float)_random.NextDouble() - 0.5f) * 0.3f; // +/- 15%
                token.Metadata.Electronegativity = Math.Clamp(
                    token.Metadata.Electronegativity + delta, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Erases parts of token metadata
        /// </summary>
        private void ApplyErasure(Token token)
        {
            var metadata = token.Metadata;

            // Erase syntax category
            if (_random.NextDouble() < 0.4f)
            {
                metadata.SyntaxCategory = "";
            }

            // Erase semantic type
            if (_random.NextDouble() < 0.4f)
            {
                metadata.SemanticType = "";
            }

            // Erase grammar role
            if (_random.NextDouble() < 0.4f)
            {
                metadata.GrammarRole = "";
            }

            // Zero out electronegativity
            if (_random.NextDouble() < 0.3f)
            {
                metadata.Electronegativity = 0.0f;
            }

            // Reduce bonding capacity
            if (_random.NextDouble() < 0.3f)
            {
                metadata.BondingCapacity = Math.Max(0, metadata.BondingCapacity - 1);
            }
        }

        /// <summary>
        /// Garbles a string by replacing random characters with special chars
        /// </summary>
        private string GarbleString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            char[] garbled = input.ToCharArray();
            char[] specialChars = { '@', '#', '$', '%', '&', '*', '~', '€', '£' };

            // Replace 1-2 random characters
            int replacements = _random.Next(1, 3);
            for (int i = 0; i < replacements && i < garbled.Length; i++)
            {
                int index = _random.Next(garbled.Length);
                garbled[index] = specialChars[_random.Next(specialChars.Length)];
            }

            return new string(garbled);
        }

        /// <summary>
        /// Mutates a token value to a different valid value
        /// </summary>
        private string MutateValue(TokenType type, string currentValue)
        {
            return type switch
            {
                TokenType.INTEGER_LITERAL => _random.Next(0, 100).ToString(),
                TokenType.FLOAT_LITERAL => (_random.NextDouble() * 100).ToString("F2"),
                TokenType.STRING_LITERAL => $"\"str{_random.Next(0, 100)}\"",
                TokenType.BOOLEAN_LITERAL => _random.Next(2) == 0 ? "false" : "true",
                TokenType.IDENTIFIER => $"var{_random.Next(0, 100)}",

                // Mutate operators
                TokenType.OPERATOR_PLUS => new[] { "+", "-", "*", "/" }[_random.Next(4)],
                TokenType.OPERATOR_MINUS => new[] { "+", "-", "*", "/" }[_random.Next(4)],
                TokenType.OPERATOR_MULTIPLY => new[] { "+", "-", "*", "/" }[_random.Next(4)],
                TokenType.OPERATOR_DIVIDE => new[] { "+", "-", "*", "/" }[_random.Next(4)],

                _ => currentValue // Keep current value for other types
            };
        }

        /// <summary>
        /// Mutates a token type to a related type
        /// </summary>
        private TokenType MutateType(TokenType currentType)
        {
            // Define mutation groups (types that can mutate into each other)
            var mutations = new Dictionary<TokenType, TokenType[]>
            {
                { TokenType.INTEGER_LITERAL, new[] { TokenType.FLOAT_LITERAL, TokenType.STRING_LITERAL } },
                { TokenType.FLOAT_LITERAL, new[] { TokenType.INTEGER_LITERAL, TokenType.STRING_LITERAL } },
                { TokenType.STRING_LITERAL, new[] { TokenType.INTEGER_LITERAL, TokenType.FLOAT_LITERAL } },

                { TokenType.TYPE_INT, new[] { TokenType.TYPE_FLOAT, TokenType.TYPE_LONG } },
                { TokenType.TYPE_FLOAT, new[] { TokenType.TYPE_INT, TokenType.TYPE_DOUBLE } },

                { TokenType.OPERATOR_PLUS, new[] { TokenType.OPERATOR_MINUS, TokenType.OPERATOR_MULTIPLY } },
                { TokenType.OPERATOR_MINUS, new[] { TokenType.OPERATOR_PLUS, TokenType.OPERATOR_DIVIDE } },
                { TokenType.OPERATOR_MULTIPLY, new[] { TokenType.OPERATOR_DIVIDE, TokenType.OPERATOR_MODULO } },
                { TokenType.OPERATOR_DIVIDE, new[] { TokenType.OPERATOR_MULTIPLY, TokenType.OPERATOR_MODULO } },

                { TokenType.KEYWORD_IF, new[] { TokenType.KEYWORD_WHILE, TokenType.KEYWORD_FOR } },
                { TokenType.KEYWORD_WHILE, new[] { TokenType.KEYWORD_IF, TokenType.KEYWORD_FOR } },
                { TokenType.KEYWORD_FOR, new[] { TokenType.KEYWORD_WHILE, TokenType.KEYWORD_IF } },
            };

            if (mutations.TryGetValue(currentType, out var possibleMutations))
            {
                return possibleMutations[_random.Next(possibleMutations.Length)];
            }

            return currentType; // No mutation available
        }

        /// <summary>
        /// Handles critically damaged tokens (breaks bonds, may destroy token)
        /// </summary>
        private void HandleCriticalDamage(Token token, long currentTick)
        {
            // Break all bonds
            if (token.BondedTokens.Count > 0)
            {
                var bondedTokensCopy = new List<Token>(token.BondedTokens);
                foreach (var bondedToken in bondedTokensCopy)
                {
                    _bondingManager?.BreakBond(token, bondedToken, currentTick);
                }
            }

            // Critically damaged tokens have chance to be destroyed
            if (_random.NextDouble() < 0.3f) // 30% chance
            {
                token.IsActive = false;
                token.Energy = 0;
            }
        }

        /// <summary>
        /// Attempts to repair a damaged token (for future use with repair mechanisms)
        /// </summary>
        public bool AttemptRepair(Token token, int energyCost)
        {
            if (token == null || !token.IsDamaged)
                return false;

            if (token.Energy < energyCost)
                return false;

            // Deduct energy
            token.Energy -= energyCost;

            // Reduce damage
            float repairAmount = (float)_random.NextDouble() * 0.3f; // Repair 0-30%
            token.DamageLevel = Math.Max(0.0f, token.DamageLevel - repairAmount);

            if (token.DamageLevel <= 0.0f)
            {
                token.IsDamaged = false;
                token.DamageLevel = 0.0f;
            }

            return true;
        }

        /// <summary>
        /// Processes damage for all tokens in a list
        /// </summary>
        public void ProcessTokenDamage(List<Token> tokens, long currentTick)
        {
            if (tokens == null)
                return;

            foreach (var token in tokens.Where(t => t.IsActive))
            {
                ApplyDamage(token, currentTick);
            }
        }

        /// <summary>
        /// Gets damage statistics for reporting
        /// </summary>
        public DamageStatistics GetDamageStatistics(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return new DamageStatistics();

            var activeTokens = tokens.Where(t => t.IsActive).ToList();

            return new DamageStatistics
            {
                TotalTokens = activeTokens.Count,
                DamagedTokens = activeTokens.Count(t => t.IsDamaged),
                CriticallyDamagedTokens = activeTokens.Count(t => t.DamageLevel >= CRITICAL_DAMAGE_THRESHOLD),
                AverageDamageLevel = activeTokens.Count > 0
                    ? activeTokens.Average(t => t.DamageLevel)
                    : 0.0f,
                MaxDamageLevel = activeTokens.Count > 0
                    ? activeTokens.Max(t => t.DamageLevel)
                    : 0.0f
            };
        }
    }

    /// <summary>
    /// Statistics about damage in the simulation
    /// </summary>
    public class DamageStatistics
    {
        public int TotalTokens { get; set; }
        public int DamagedTokens { get; set; }
        public int CriticallyDamagedTokens { get; set; }
        public float AverageDamageLevel { get; set; }
        public float MaxDamageLevel { get; set; }

        public override string ToString()
        {
            return $"Damage: {DamagedTokens}/{TotalTokens} damaged " +
                   $"({CriticallyDamagedTokens} critical), Avg: {AverageDamageLevel:F2}";
        }
    }
}

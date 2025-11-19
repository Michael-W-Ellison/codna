using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Utilities;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Represents a thermal vent that generates tokens into the environment.
    /// Based on section 3.7 of the design specification.
    /// </summary>
    public class ThermalVent
    {
        public Vector3Int Position { get; set; }
        public int EmissionRate { get; set; } // Tokens per N ticks
        public Dictionary<TokenType, float> TokenDistribution { get; set; }
        public int InitialEnergy { get; set; }

        private TokenFactory _tokenFactory;
        private Random _random;
        private long _lastEmissionTick;
        private int _tokensGenerated;

        public int TokensGenerated => _tokensGenerated;

        public ThermalVent(Vector3Int position, TokenFactory tokenFactory,
                          int emissionRate = 10, int initialEnergy = 50)
        {
            Position = position;
            EmissionRate = emissionRate;
            InitialEnergy = initialEnergy;
            _tokenFactory = tokenFactory;
            _random = new Random();
            _lastEmissionTick = 0;
            _tokensGenerated = 0;

            TokenDistribution = new Dictionary<TokenType, float>();
            InitializeDefaultDistribution();
        }

        /// <summary>
        /// Attempts to generate a token if enough ticks have passed
        /// </summary>
        public Token? GenerateToken(long currentTick)
        {
            if (currentTick - _lastEmissionTick < EmissionRate)
                return null;

            _lastEmissionTick = currentTick;
            _tokensGenerated++;

            var tokenType = SelectWeightedRandomToken();
            var token = _tokenFactory.CreateToken(tokenType, Position, InitialEnergy);

            return token;
        }

        /// <summary>
        /// Selects a random token type based on weighted distribution
        /// </summary>
        private TokenType SelectWeightedRandom Token()
        {
            if (TokenDistribution.Count == 0)
                return TokenType.IDENTIFIER;

            float totalWeight = TokenDistribution.Values.Sum();
            float randomValue = (float)_random.NextDouble() * totalWeight;
            float cumulativeWeight = 0;

            foreach (var kvp in TokenDistribution)
            {
                cumulativeWeight += kvp.Value;
                if (randomValue <= cumulativeWeight)
                {
                    return kvp.Key;
                }
            }

            // Fallback to last token type
            return TokenDistribution.Keys.Last();
        }

        /// <summary>
        /// Initializes a balanced token distribution
        /// Based on section 3.7.2 BALANCED_DISTRIBUTION
        /// </summary>
        private void InitializeDefaultDistribution()
        {
            // INTEGER_LITERAL: 0.15
            TokenDistribution[TokenType.INTEGER_LITERAL] = 0.15f;

            // IDENTIFIER: 0.15
            TokenDistribution[TokenType.IDENTIFIER] = 0.15f;

            // OPERATORS: 0.20 (distributed among operator types)
            TokenDistribution[TokenType.OPERATOR_PLUS] = 0.05f;
            TokenDistribution[TokenType.OPERATOR_MINUS] = 0.05f;
            TokenDistribution[TokenType.OPERATOR_MULTIPLY] = 0.03f;
            TokenDistribution[TokenType.OPERATOR_DIVIDE] = 0.03f;
            TokenDistribution[TokenType.OPERATOR_ASSIGN] = 0.04f;

            // KEYWORDS: 0.15 (distributed among keywords)
            TokenDistribution[TokenType.KEYWORD_IF] = 0.03f;
            TokenDistribution[TokenType.KEYWORD_WHILE] = 0.03f;
            TokenDistribution[TokenType.KEYWORD_FOR] = 0.03f;
            TokenDistribution[TokenType.KEYWORD_RETURN] = 0.03f;
            TokenDistribution[TokenType.KEYWORD_VAR] = 0.03f;

            // TYPES: 0.10
            TokenDistribution[TokenType.TYPE_INT] = 0.04f;
            TokenDistribution[TokenType.TYPE_FLOAT] = 0.03f;
            TokenDistribution[TokenType.TYPE_STRING] = 0.03f;

            // STRUCTURAL: 0.15
            TokenDistribution[TokenType.PAREN_OPEN] = 0.04f;
            TokenDistribution[TokenType.PAREN_CLOSE] = 0.04f;
            TokenDistribution[TokenType.BRACE_OPEN] = 0.035f;
            TokenDistribution[TokenType.BRACE_CLOSE] = 0.035f;

            // PUNCTUATION: 0.10
            TokenDistribution[TokenType.SEMICOLON] = 0.05f;
            TokenDistribution[TokenType.COMMA] = 0.05f;
        }

        /// <summary>
        /// Sets a custom token distribution
        /// </summary>
        public void SetDistribution(Dictionary<TokenType, float> distribution)
        {
            TokenDistribution = new Dictionary<TokenType, float>(distribution);
        }

        /// <summary>
        /// Sets distribution for expression-heavy simulation
        /// Based on section 3.7.2 EXPRESSION_HEAVY
        /// </summary>
        public void SetExpressionHeavyDistribution()
        {
            TokenDistribution.Clear();

            // INTEGER_LITERAL: 0.20
            TokenDistribution[TokenType.INTEGER_LITERAL] = 0.20f;

            // IDENTIFIER: 0.15
            TokenDistribution[TokenType.IDENTIFIER] = 0.15f;

            // OPERATORS: 0.35 (heavy on operators)
            TokenDistribution[TokenType.OPERATOR_PLUS] = 0.10f;
            TokenDistribution[TokenType.OPERATOR_MINUS] = 0.10f;
            TokenDistribution[TokenType.OPERATOR_MULTIPLY] = 0.075f;
            TokenDistribution[TokenType.OPERATOR_DIVIDE] = 0.075f;

            // STRUCTURAL: 0.20
            TokenDistribution[TokenType.PAREN_OPEN] = 0.10f;
            TokenDistribution[TokenType.PAREN_CLOSE] = 0.10f;

            // PUNCTUATION: 0.10
            TokenDistribution[TokenType.SEMICOLON] = 0.10f;
        }

        /// <summary>
        /// Sets distribution for control-structure-heavy simulation
        /// Based on section 3.7.2 CONTROL_HEAVY
        /// </summary>
        public void SetControlHeavyDistribution()
        {
            TokenDistribution.Clear();

            // IDENTIFIER: 0.20
            TokenDistribution[TokenType.IDENTIFIER] = 0.20f;

            // KEYWORDS: 0.30 (heavy on control flow)
            TokenDistribution[TokenType.KEYWORD_IF] = 0.08f;
            TokenDistribution[TokenType.KEYWORD_ELSE] = 0.05f;
            TokenDistribution[TokenType.KEYWORD_WHILE] = 0.07f;
            TokenDistribution[TokenType.KEYWORD_FOR] = 0.07f;
            TokenDistribution[TokenType.KEYWORD_RETURN] = 0.03f;

            // OPERATORS: 0.15
            TokenDistribution[TokenType.OPERATOR_ASSIGN] = 0.05f;
            TokenDistribution[TokenType.OPERATOR_EQUALS] = 0.05f;
            TokenDistribution[TokenType.OPERATOR_LESS_THAN] = 0.05f;

            // TYPES: 0.15
            TokenDistribution[TokenType.TYPE_INT] = 0.05f;
            TokenDistribution[TokenType.TYPE_FLOAT] = 0.05f;
            TokenDistribution[TokenType.TYPE_BOOL] = 0.05f;

            // STRUCTURAL: 0.20
            TokenDistribution[TokenType.BRACE_OPEN] = 0.10f;
            TokenDistribution[TokenType.BRACE_CLOSE] = 0.10f;
        }

        public override string ToString()
        {
            return $"ThermalVent({Position}, Rate:{EmissionRate}, Tokens Generated:{TokensGenerated})";
        }
    }
}

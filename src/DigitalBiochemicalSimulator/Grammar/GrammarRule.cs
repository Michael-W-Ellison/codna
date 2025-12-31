using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// Defines a grammar rule for valid token sequences.
    /// Based on section 3.5.1 of the design specification.
    /// </summary>
    public class GrammarRule
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<TokenPattern> Pattern { get; set; }
        public BondType BondType { get; set; }
        public float BondStrength { get; set; }
        public int EnergyCost { get; set; }
        public ValidationLevel ValidationLevel { get; set; }

        public GrammarRule(string id, string name)
        {
            Id = id;
            Name = name;
            Pattern = new List<TokenPattern>();
            BondType = BondType.VAN_DER_WAALS;
            BondStrength = 0.5f;
            EnergyCost = 0;
            ValidationLevel = ValidationLevel.DELAYED;
        }

        /// <summary>
        /// Checks if a sequence of tokens matches this grammar rule
        /// </summary>
        public bool Matches(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return false;

            // Simple sequential matching
            int tokenIndex = 0;
            int patternIndex = 0;

            while (patternIndex < Pattern.Count && tokenIndex < tokens.Count)
            {
                var pattern = Pattern[patternIndex];
                var token = tokens[tokenIndex];

                if (pattern.Matches(token.Type))
                {
                    tokenIndex++;
                    patternIndex++;
                }
                else if (pattern.Quantifier == Quantifier.OPTIONAL ||
                        pattern.Quantifier == Quantifier.ZERO_OR_MORE)
                {
                    // Pattern is optional, skip it
                    patternIndex++;
                }
                else
                {
                    // Pattern doesn't match and is not optional
                    return false;
                }
            }

            // Check if all required patterns were matched
            while (patternIndex < Pattern.Count)
            {
                var pattern = Pattern[patternIndex];
                if (pattern.Quantifier == Quantifier.ONE ||
                    pattern.Quantifier == Quantifier.ONE_OR_MORE)
                {
                    return false; // Required pattern not matched
                }
                patternIndex++;
            }

            return true;
        }

        /// <summary>
        /// Checks if two tokens can bond according to this rule
        /// </summary>
        public bool CanBond(Token token1, Token token2)
        {
            if (Pattern.Count < 2)
                return false;

            // Check if the two tokens match the first two patterns
            return Pattern[0].Matches(token1.Type) && Pattern[1].Matches(token2.Type);
        }

        public override string ToString()
        {
            var patternStr = string.Join(" ", Pattern.Select(p => p.Description));
            return $"Rule({Id}: {Name}, Pattern:[{patternStr}], Bond:{BondType}, Strength:{BondStrength})";
        }
    }
}

using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// Represents a pattern that tokens must match in a grammar rule.
    /// Based on section 3.5.1 of the design specification.
    /// </summary>
    public class TokenPattern
    {
        public List<TokenType> AcceptedTypes { get; set; }
        public Quantifier Quantifier { get; set; }
        public string Description { get; set; }

        public TokenPattern()
        {
            AcceptedTypes = new List<TokenType>();
            Quantifier = Quantifier.ONE;
            Description = string.Empty;
        }

        public TokenPattern(TokenType type, Quantifier quantifier = Quantifier.ONE)
        {
            AcceptedTypes = new List<TokenType> { type };
            Quantifier = quantifier;
            Description = $"{type} ({quantifier})";
        }

        public TokenPattern(List<TokenType> types, Quantifier quantifier = Quantifier.ONE)
        {
            AcceptedTypes = new List<TokenType>(types);
            Quantifier = quantifier;
            Description = $"[{string.Join(", ", types)}] ({quantifier})";
        }

        /// <summary>
        /// Checks if a token type matches this pattern
        /// </summary>
        public bool Matches(TokenType type)
        {
            return AcceptedTypes.Count == 0 || AcceptedTypes.Contains(type);
        }

        /// <summary>
        /// Checks if the pattern is satisfied by a count
        /// </summary>
        public bool IsSatisfiedBy(int count)
        {
            return Quantifier switch
            {
                Quantifier.ONE => count == 1,
                Quantifier.OPTIONAL => count <= 1,
                Quantifier.ZERO_OR_MORE => count >= 0,
                Quantifier.ONE_OR_MORE => count >= 1,
                _ => false
            };
        }
    }
}

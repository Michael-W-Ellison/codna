using System;

namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Metadata for tokens that determines bonding compatibility.
    /// Subject to damage/corruption at high altitudes.
    /// Based on section 3.1.1 of the design specification.
    /// </summary>
    public class TokenMetadata
    {
        /// <summary>
        /// Syntactic category (e.g., "expression", "statement", "declaration")
        /// </summary>
        public string SyntaxCategory { get; set; }

        /// <summary>
        /// Semantic type (e.g., "int", "string", "boolean")
        /// </summary>
        public string SemanticType { get; set; }

        /// <summary>
        /// Grammar role (e.g., "operator", "operand", "keyword")
        /// </summary>
        public string GrammarRole { get; set; }

        /// <summary>
        /// Electronegativity value (0.0 - 1.0) determining bond attraction
        /// </summary>
        public float Electronegativity { get; set; }

        /// <summary>
        /// Maximum number of bonds this token can form
        /// </summary>
        public int BondingCapacity { get; set; }

        public TokenMetadata()
        {
            SyntaxCategory = string.Empty;
            SemanticType = string.Empty;
            GrammarRole = string.Empty;
            Electronegativity = 0.5f;
            BondingCapacity = 2; // Default: can bond start and end
        }

        public TokenMetadata(string syntaxCategory, string semanticType, string grammarRole,
                            float electronegativity, int bondingCapacity)
        {
            SyntaxCategory = syntaxCategory;
            SemanticType = semanticType;
            GrammarRole = grammarRole;
            Electronegativity = Math.Clamp(electronegativity, 0.0f, 1.0f);
            BondingCapacity = bondingCapacity;
        }

        /// <summary>
        /// Creates a deep copy of this metadata
        /// </summary>
        public TokenMetadata Clone()
        {
            return new TokenMetadata(
                SyntaxCategory,
                SemanticType,
                GrammarRole,
                Electronegativity,
                BondingCapacity
            );
        }
    }
}

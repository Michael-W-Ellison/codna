using System;
using System.Collections.Generic;

namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Represents a location on a token where bonds can form.
    /// Based on section 3.1.3 of the design specification.
    /// </summary>
    public class BondSite
    {
        /// <summary>
        /// Location of the bond site on the token
        /// </summary>
        public BondLocation Location { get; set; }

        /// <summary>
        /// Whether this bond site is currently occupied
        /// </summary>
        public bool IsOccupied { get; set; }

        /// <summary>
        /// The token bonded at this site (null if unoccupied)
        /// </summary>
        public Token? BondedTo { get; set; }

        /// <summary>
        /// Current strength of the bond (0.0 - 1.0)
        /// </summary>
        public float BondStrength { get; set; }

        /// <summary>
        /// Token types that can bond at this site
        /// </summary>
        public HashSet<TokenType> AcceptedTypes { get; set; }

        /// <summary>
        /// Grammar rule ID that governs this bond site
        /// </summary>
        public string? GrammarRuleId { get; set; }

        public BondSite(BondLocation location)
        {
            Location = location;
            IsOccupied = false;
            BondedTo = null;
            BondStrength = 0.0f;
            AcceptedTypes = new HashSet<TokenType>();
            GrammarRuleId = null;
        }

        public BondSite(BondLocation location, HashSet<TokenType> acceptedTypes)
        {
            Location = location;
            IsOccupied = false;
            BondedTo = null;
            BondStrength = 0.0f;
            AcceptedTypes = acceptedTypes;
            GrammarRuleId = null;
        }

        /// <summary>
        /// Checks if a token type is accepted at this bond site
        /// </summary>
        public bool AcceptsType(TokenType type)
        {
            return AcceptedTypes.Count == 0 || AcceptedTypes.Contains(type);
        }

        /// <summary>
        /// Forms a bond with another token
        /// </summary>
        public void FormBond(Token token, float strength)
        {
            BondedTo = token;
            BondStrength = Math.Clamp(strength, 0.0f, 1.0f);
            IsOccupied = true;
        }

        /// <summary>
        /// Breaks the current bond
        /// </summary>
        public void BreakBond()
        {
            BondedTo = null;
            BondStrength = 0.0f;
            IsOccupied = false;
        }
    }
}

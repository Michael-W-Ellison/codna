using System;
using System.Collections.Generic;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Represents a programming language token in the simulation.
    /// Based on section 3.1.1 of the design specification.
    /// </summary>
    public class Token
    {
        // Identity
        private static long _nextId = 0;
        public long Id { get; set; }
        public TokenType Type { get; set; }
        public string Value { get; set; }

        // Physical Properties
        public Vector3Int Position { get; set; }
        public int Mass { get; set; } // Character count
        public int Energy { get; set; }
        public Vector3Int Velocity { get; set; }

        // Bonding Properties
        public List<BondSite> BondSites { get; set; }
        public List<Token> BondedTokens { get; set; }
        public Token? ChainHead { get; set; }
        public int ChainPosition { get; set; }

        // Metadata (subject to damage)
        public TokenMetadata Metadata { get; set; }

        // State
        public bool IsActive { get; set; }
        public bool IsDamaged { get; set; }
        public float DamageLevel { get; set; } // 0.0 - 1.0
        public float StabilityScore { get; set; }
        public long TicksSinceLastBond { get; set; }

        // Movement state
        public bool IsRising => Energy > 0;
        public bool IsFalling => Energy <= 0;

        /// <summary>
        /// Constructor with explicit ID (for testing and controlled scenarios)
        /// </summary>
        public Token(long id, TokenType type, string value, Vector3Int position)
        {
            Id = id;
            Type = type;
            Value = value;
            Position = position;
            Mass = value.Length;
            Energy = 0;
            Velocity = Vector3Int.Zero;

            BondSites = new List<BondSite>();
            BondedTokens = new List<Token>();
            ChainHead = null;
            ChainPosition = 0;

            Metadata = new TokenMetadata();

            IsActive = true;
            IsDamaged = false;
            DamageLevel = 0.0f;
            StabilityScore = 0.0f;
            TicksSinceLastBond = 0;

            InitializeBondSites();
        }

        /// <summary>
        /// Constructor with auto-generated ID (for production use)
        /// </summary>
        public Token(TokenType type, string value, Vector3Int position)
            : this(System.Threading.Interlocked.Increment(ref _nextId), type, value, position)
        {
        }

        /// <summary>
        /// Initialize bond sites based on token type
        /// </summary>
        private void InitializeBondSites()
        {
            // Most tokens have START and END bond sites
            BondSites.Add(new BondSite(BondLocation.START));
            BondSites.Add(new BondSite(BondLocation.END));

            // Some tokens may have additional bond sites
            // This will be expanded based on grammar rules
        }

        /// <summary>
        /// Finds an available bond site that accepts the given token type
        /// </summary>
        public BondSite? FindAvailableBondSite(TokenType targetType)
        {
            foreach (var site in BondSites)
            {
                if (!site.IsOccupied && site.AcceptsType(targetType))
                {
                    return site;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the bond site at a specific location
        /// </summary>
        public BondSite? GetBondSite(BondLocation location)
        {
            return BondSites.Find(s => s.Location == location);
        }

        /// <summary>
        /// Checks if this token is bonded to any other tokens
        /// </summary>
        public bool IsBonded => BondedTokens.Count > 0;

        /// <summary>
        /// Checks if this token is part of a chain
        /// </summary>
        public bool IsInChain => ChainHead != null;

        /// <summary>
        /// Gets total mass including bonded tokens (if chain head)
        /// </summary>
        public int GetTotalChainMass()
        {
            if (!IsInChain)
                return Mass;

            int totalMass = Mass;
            foreach (var bondedToken in BondedTokens)
            {
                totalMass += bondedToken.Mass;
            }
            return totalMass;
        }

        public override string ToString()
        {
            return $"Token({Type}, \"{Value}\", Pos:{Position}, Energy:{Energy})";
        }
    }
}

using System;
using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Utilities
{
    /// <summary>
    /// Object pool for token reuse and performance optimization.
    /// Based on section 8.1 of the design specification.
    /// </summary>
    public class TokenPool
    {
        private Queue<Token> _availableTokens;
        private HashSet<Token> _activeTokens;
        private int _maxPoolSize;
        private int _totalCreated;

        public int AvailableCount => _availableTokens.Count;
        public int ActiveCount => _activeTokens.Count;
        public int TotalCreated => _totalCreated;

        public TokenPool(int initialSize = 100, int maxPoolSize = 1000)
        {
            _availableTokens = new Queue<Token>(initialSize);
            _activeTokens = new HashSet<Token>();
            _maxPoolSize = maxPoolSize;
            _totalCreated = 0;

            // Pre-allocate tokens
            for (int i = 0; i < initialSize; i++)
            {
                var token = CreateNewToken();
                _availableTokens.Enqueue(token);
            }
        }

        /// <summary>
        /// Gets a token from the pool or creates a new one
        /// </summary>
        public Token GetToken(TokenType type, string value, Vector3Int position)
        {
            Token token;

            if (_availableTokens.Count > 0)
            {
                token = _availableTokens.Dequeue();
                ResetToken(token, type, value, position);
            }
            else
            {
                token = CreateNewToken();
                InitializeToken(token, type, value, position);
            }

            _activeTokens.Add(token);
            return token;
        }

        /// <summary>
        /// Returns a token to the pool for reuse
        /// </summary>
        public void ReleaseToken(Token token)
        {
            if (token == null || !_activeTokens.Contains(token))
                return;

            _activeTokens.Remove(token);

            // Only pool if we haven't exceeded max size
            if (_availableTokens.Count < _maxPoolSize)
            {
                CleanToken(token);
                _availableTokens.Enqueue(token);
            }
        }

        /// <summary>
        /// Returns multiple tokens to the pool
        /// </summary>
        public void ReleaseTokens(IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                ReleaseToken(token);
            }
        }

        /// <summary>
        /// Creates a new token instance
        /// </summary>
        private Token CreateNewToken()
        {
            _totalCreated++;
            return new Token(TokenType.IDENTIFIER, "", Vector3Int.Zero);
        }

        /// <summary>
        /// Initializes a newly created token
        /// </summary>
        private void InitializeToken(Token token, TokenType type, string value, Vector3Int position)
        {
            token.Type = type;
            token.Value = value;
            token.Position = position;
            token.Mass = value.Length;
            token.Energy = 0;
            token.IsActive = true;
        }

        /// <summary>
        /// Resets a pooled token for reuse
        /// </summary>
        private void ResetToken(Token token, TokenType type, string value, Vector3Int position)
        {
            // Reset identity - create temporary token to get new auto-generated ID
            var tempToken = new Token(type, value, position);
            token.Id = tempToken.Id;
            token.Type = type;
            token.Value = value;

            // Reset physical properties
            token.Position = position;
            token.Mass = value.Length;
            token.Energy = 0;
            token.Velocity = Vector3Int.Zero;

            // Reset bonding
            token.BondedTokens.Clear();
            foreach (var site in token.BondSites)
            {
                site.BreakBond();
            }
            token.ChainHead = null;
            token.ChainPosition = 0;

            // Reset state
            token.IsActive = true;
            token.IsDamaged = false;
            token.DamageLevel = 0.0f;
            token.StabilityScore = 0.0f;
            token.TicksSinceLastBond = 0;
        }

        /// <summary>
        /// Cleans a token before returning to pool
        /// </summary>
        private void CleanToken(Token token)
        {
            token.BondedTokens.Clear();
            foreach (var site in token.BondSites)
            {
                site.BreakBond();
            }
            token.ChainHead = null;
            token.IsActive = false;
        }

        /// <summary>
        /// Clears the entire pool
        /// </summary>
        public void Clear()
        {
            _availableTokens.Clear();
            _activeTokens.Clear();
        }

        public override string ToString()
        {
            return $"TokenPool(Active:{ActiveCount}, Available:{AvailableCount}, Total Created:{TotalCreated})";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.DataStructures
{
    /// <summary>
    /// Represents a cell in the 3D grid that contains tokens.
    /// Based on section 3.2 of the design specification.
    /// </summary>
    public class Cell
    {
        public Vector3Int Position { get; set; }
        public List<Token> Tokens { get; set; }
        public int TotalMass { get; set; }
        public int Capacity { get; set; }
        public bool IsOverflowing => TotalMass > Capacity;
        public bool IsEmpty => Tokens.Count == 0;
        public bool IsInMutationZone { get; set; }

        public Cell(Vector3Int position, int capacity = 1000)
        {
            Position = position;
            Tokens = new List<Token>();
            TotalMass = 0;
            Capacity = capacity;
            IsInMutationZone = false;
        }

        /// <summary>
        /// Adds a token to this cell
        /// </summary>
        public bool AddToken(Token token)
        {
            if (token == null)
                return false;

            Tokens.Add(token);
            TotalMass += token.Mass;
            token.Position = Position;
            return true;
        }

        /// <summary>
        /// Removes a token from this cell
        /// </summary>
        public bool RemoveToken(Token token)
        {
            if (token == null || !Tokens.Contains(token))
                return false;

            Tokens.Remove(token);
            TotalMass -= token.Mass;
            return true;
        }

        /// <summary>
        /// Checks if this cell can accept a token without overflowing
        /// </summary>
        public bool CanAcceptToken(Token token)
        {
            return (TotalMass + token.Mass) <= Capacity;
        }

        /// <summary>
        /// Gets the overflow amount (excess mass beyond capacity)
        /// </summary>
        public int GetOverflowAmount()
        {
            return Math.Max(0, TotalMass - Capacity);
        }

        /// <summary>
        /// Gets all tokens that can be redistributed (for overflow handling)
        /// </summary>
        public List<Token> GetRedistributableTokens()
        {
            // Prioritize falling tokens for redistribution
            return Tokens
                .Where(t => t.IsFalling)
                .OrderBy(t => t.Mass)
                .ToList();
        }

        /// <summary>
        /// Clears all tokens from this cell
        /// </summary>
        public void Clear()
        {
            Tokens.Clear();
            TotalMass = 0;
        }

        public override string ToString()
        {
            return $"Cell({Position}, Tokens:{Tokens.Count}, Mass:{TotalMass}/{Capacity})";
        }
    }
}

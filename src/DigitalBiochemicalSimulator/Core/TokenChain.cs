using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Represents a chain of bonded tokens forming a code structure.
    /// Based on section 4.3 of the design specification.
    /// </summary>
    public class TokenChain
    {
        public Guid Id { get; set; }
        public Token Head { get; set; }
        public Token Tail { get; set; }
        public int Length { get; set; }
        public int TotalMass { get; set; }
        public int TotalEnergy { get; set; }
        public float StabilityScore { get; set; }
        public long LastModifiedTick { get; set; }
        public bool IsValid { get; set; }

        /// <summary>
        /// All tokens in this chain (ordered from head to tail)
        /// </summary>
        public List<Token> Tokens { get; set; }

        /// <summary>
        /// Average bond strength of all bonds in the chain
        /// </summary>
        public float AverageBondStrength { get; set; }

        /// <summary>
        /// Number of ticks since the chain was last modified
        /// </summary>
        public long TicksSinceModified { get; set; }

        public TokenChain(Token headToken)
        {
            Id = Guid.NewGuid();
            Head = headToken;
            Tail = headToken;
            Length = 1;
            TotalMass = headToken.Mass;
            TotalEnergy = headToken.Energy;
            StabilityScore = 0.0f;
            LastModifiedTick = 0;
            IsValid = false;
            AverageBondStrength = 0.0f;
            TicksSinceModified = 0;

            Tokens = new List<Token> { headToken };
            headToken.ChainHead = headToken;
            headToken.ChainPosition = 0;
        }

        /// <summary>
        /// Adds a token to the chain (at head or tail)
        /// </summary>
        public void AddToken(Token token, bool atTail = true)
        {
            if (atTail)
            {
                Tokens.Add(token);
                Tail = token;
            }
            else
            {
                Tokens.Insert(0, token);
                Head = token;
            }

            Length++;
            TotalMass += token.Mass;
            TotalEnergy += token.Energy;

            // Update all tokens' chain references
            UpdateChainReferences();
        }

        /// <summary>
        /// Removes a token from the chain
        /// </summary>
        public void RemoveToken(Token token)
        {
            if (!Tokens.Contains(token))
                return;

            Tokens.Remove(token);
            Length--;
            TotalMass -= token.Mass;
            TotalEnergy -= token.Energy;

            // Update head and tail
            if (Tokens.Count > 0)
            {
                Head = Tokens[0];
                Tail = Tokens[^1];
                UpdateChainReferences();
            }
        }

        /// <summary>
        /// Updates all tokens in the chain with correct chain references
        /// </summary>
        private void UpdateChainReferences()
        {
            for (int i = 0; i < Tokens.Count; i++)
            {
                Tokens[i].ChainHead = Head;
                Tokens[i].ChainPosition = i;
            }
        }

        /// <summary>
        /// Calculates and updates the stability score
        /// Based on section 4.4 of the design specification
        /// </summary>
        public void CalculateStability(long currentTick)
        {
            float stability = 1.0f;

            // Factor 1: Bond strength
            if (Length > 1)
            {
                stability *= AverageBondStrength;
            }

            // Factor 2: Grammar validity
            if (IsValid)
            {
                stability *= 1.2f;
            }
            else
            {
                stability *= 0.5f;
            }

            // Factor 3: Age/consistency
            TicksSinceModified = currentTick - LastModifiedTick;
            float ageBonus = Math.Min(TicksSinceModified / 100.0f, 0.5f);
            stability += ageBonus;

            // Factor 4: Damage levels
            float avgDamage = Tokens.Average(t => t.DamageLevel);
            stability *= (1.0f - avgDamage);

            // Factor 5: Energy reserves
            float energyRatio = TotalEnergy / (Length * 10.0f);
            stability *= Math.Min(energyRatio, 1.0f);

            StabilityScore = Math.Clamp(stability, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets the code string represented by this chain
        /// </summary>
        public string ToCodeString()
        {
            return string.Join(" ", Tokens.Select(t => t.Value));
        }

        public override string ToString()
        {
            return $"Chain(Length:{Length}, Stability:{StabilityScore:F2}, Code:\"{ToCodeString()}\")";
        }
    }
}

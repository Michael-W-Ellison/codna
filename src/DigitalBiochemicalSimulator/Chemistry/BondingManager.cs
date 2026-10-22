using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Manages token bonding, chain formation, and energy distribution.
    /// Based on section 3.6 of the design specification.
    /// </summary>
    public class BondingManager
    {
        private readonly BondRulesEngine _rulesEngine;
        private readonly BondStrengthCalculator _strengthCalculator;
        private readonly SimulationConfig _config;
        private readonly Grid _grid;
        private readonly List<TokenChain> _activeChains;

        private long _nextChainId = 1;

        public BondingManager(BondRulesEngine rulesEngine, BondStrengthCalculator strengthCalculator,
                             SimulationConfig config, Grid grid)
        {
            _rulesEngine = rulesEngine;
            _strengthCalculator = strengthCalculator;
            _config = config;
            _grid = grid;
            _activeChains = new List<TokenChain>();
        }

        /// <summary>
        /// Attempts to bond two tokens together
        /// Returns true if bond was successful
        /// </summary>
        public bool AttemptBond(Token token1, Token token2, long currentTick)
        {
            if (token1 == null || token2 == null)
                return false;

            if (!token1.IsActive || !token2.IsActive)
                return false;

            // Prevent self-bonding
            if (token1.Id == token2.Id)
                return false;

            // Prevent duplicate bonds
            if (token1.BondedTokens.Contains(token2) || token2.BondedTokens.Contains(token1))
                return false;

            // Step 1: Find free bond sites: token1 bonds at its END to token2's START, so each token
            // bonds at most once on each side and chains stay linear (design spec section 3.4.3)
            var endSite = token1.GetBondSite(BondLocation.END);
            var startSite = token2.GetBondSite(BondLocation.START);
            if (endSite == null || endSite.IsOccupied || startSite == null || startSite.IsOccupied)
                return false;

            // Step 2: Check grammar compatibility (token1 precedes token2)
            if (!_rulesEngine.CanBond(token1, token2))
                return false;

            // Step 3: Calculate bond strength
            float bondStrength = _strengthCalculator.CalculateBondStrength(token1, token2);
            if (bondStrength < _config.MinBondStrength)
                return false;

            // Step 4: Determine bond type
            BondType bondType = _strengthCalculator.DetermineBondType(token1, token2);

            // Step 5: Create the bond (bonding needs no energy; design spec section 3.4.3)
            CreateBond(token1, token2, bondType, bondStrength);

            // Step 6: Update or create chains
            UpdateChains(token1, token2, bondType, bondStrength, currentTick);

            // Step 7: Generate bonding energy: (chain length - 1) * EnergyPerBond
            DistributeEnergyToChain(token1, _config.EnergyPerBond);

            return true;
        }

        /// <summary>
        /// Creates a bond between two tokens
        /// </summary>
        private void CreateBond(Token token1, Token token2, BondType bondType, float strength)
        {
            // Add tokens to each other's bonded lists
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Update bond sites (if available)
            UpdateBondSites(token1, token2, bondType);
        }

        /// <summary>
        /// Updates bond sites for the bonded tokens
        /// </summary>
        private void UpdateBondSites(Token token1, Token token2, BondType bondType)
        {
            // token1's END site holds token2; token2's START site holds token1
            token1.GetBondSite(BondLocation.END)?.FormBond(token2, _strengthCalculator.CalculateBondStrength(token1, token2));
            token2.GetBondSite(BondLocation.START)?.FormBond(token1, _strengthCalculator.CalculateBondStrength(token1, token2));
        }

        /// <summary>
        /// Updates or creates token chains after bonding
        /// </summary>
        private void UpdateChains(Token token1, Token token2, BondType bondType, float strength, long currentTick)
        {
            var chain1 = FindChainContaining(token1);
            var chain2 = FindChainContaining(token2);

            if (chain1 == null && chain2 == null)
            {
                // Create new chain with both tokens
                CreateNewChain(token1, token2, bondType, strength, currentTick);
            }
            else if (chain1 != null && chain2 == null)
            {
                // Add token2 to chain1
                AddTokenToChain(chain1, token2, currentTick);
            }
            else if (chain1 == null && chain2 != null)
            {
                // Add token1 to chain2
                AddTokenToChain(chain2, token1, currentTick);
            }
            else if (chain1 != null && chain2 != null && chain1 != chain2)
            {
                // Merge two chains
                MergeChains(chain1, chain2, currentTick);
            }
            else if (chain1 != null)
            {
                // Both tokens already in same chain, just update the chain
                RefreshChain(chain1, token1);
                chain1.LastModifiedAt = currentTick;
            }
        }

        /// <summary>
        /// Creates a new token chain
        /// </summary>
        private void CreateNewChain(Token head, Token tail, BondType bondType, float strength, long currentTick)
        {
            var chain = new TokenChain
            {
                Id = _nextChainId++,
                BondType = bondType,
                AverageBondStrength = strength,
                IsValid = false, // Will be validated later
                CreatedAt = currentTick,
                LastModifiedAt = currentTick,
                StabilityScore = 0.5f
            };

            RefreshChain(chain, head);
            _activeChains.Add(chain);
        }

        /// <summary>
        /// Adds a token to an existing chain
        /// </summary>
        private void AddTokenToChain(TokenChain chain, Token token, long currentTick)
        {
            if (chain == null || token == null)
                return;

            RefreshChain(chain, token);
            chain.LastModifiedAt = currentTick;
        }

        /// <summary>
        /// Merges two chains into one
        /// </summary>
        private void MergeChains(TokenChain chain1, TokenChain chain2, long currentTick)
        {
            if (chain1 == null || chain2 == null || chain1 == chain2)
                return;

            // Remove chain2 from active chains; its tokens now belong to chain1
            _activeChains.Remove(chain2);

            var start = chain1.Head ?? chain2.Head;
            if (start != null)
                RefreshChain(chain1, start);
            chain1.LastModifiedAt = currentTick;
        }

        /// <summary>
        /// Rebuilds a chain's ordered token list and derived properties from the bond graph,
        /// and points every token in the chain at it
        /// </summary>
        private void RefreshChain(TokenChain chain, Token start)
        {
            var component = CollectConnectedTokens(start);

            // Start from the first token of the chain: the one with no token bonded at its START site
            // (tokens bonded without sites fall back to any end of the chain)
            var head = component.FirstOrDefault(t => t.BondedTokens.Count > 0 && t.GetBondSite(BondLocation.START) is { IsOccupied: false }
                                                     && t.GetBondSite(BondLocation.END) is { IsOccupied: true })
                       ?? component.FirstOrDefault(t => t == chain.Head && t.BondedTokens.Count <= 1)
                       ?? component.FirstOrDefault(t => t.BondedTokens.Count <= 1)
                       ?? start;

            var ordered = new List<Token>();
            var visited = new HashSet<long>();
            var stack = new Stack<Token>();
            stack.Push(head);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Add(current.Id))
                    continue;

                ordered.Add(current);

                for (int i = current.BondedTokens.Count - 1; i >= 0; i--)
                {
                    var bonded = current.BondedTokens[i];
                    if (!visited.Contains(bonded.Id))
                        stack.Push(bonded);
                }
            }

            chain.Tokens = ordered;
            chain.Head = ordered[0];
            chain.Tail = ordered[ordered.Count - 1];
            chain.Length = ordered.Count;
            chain.TotalMass = ordered.Sum(t => t.Mass);
            chain.TotalEnergy = ordered.Sum(t => t.Energy);

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].ChainId = chain.Id;
                ordered[i].ChainHead = chain.Head;
                ordered[i].ChainPosition = i;
            }

            RecalculateChainStrength(chain);
        }

        /// <summary>
        /// Collects all tokens connected to a token through bonds
        /// </summary>
        private static List<Token> CollectConnectedTokens(Token start)
        {
            var result = new List<Token>();
            var visited = new HashSet<long>();
            var queue = new Queue<Token>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current.Id))
                    continue;

                result.Add(current);

                foreach (var bonded in current.BondedTokens)
                {
                    if (!visited.Contains(bonded.Id))
                        queue.Enqueue(bonded);
                }
            }

            return result;
        }

        /// <summary>
        /// Clears a token's chain membership
        /// </summary>
        private static void ClearChainMembership(Token token)
        {
            token.ChainId = 0;
            token.ChainHead = null;
            token.ChainPosition = 0;
        }

        /// <summary>
        /// Recalculates the average bond strength for a chain
        /// </summary>
        private void RecalculateChainStrength(TokenChain chain)
        {
            if (chain == null || chain.Length < 2)
                return;

            float totalStrength = 0;
            int bondCount = 0;

            var visited = new HashSet<long>();
            var current = chain.Head;

            while (current != null && !visited.Contains(current.Id))
            {
                visited.Add(current.Id);

                foreach (var bonded in current.BondedTokens)
                {
                    if (!visited.Contains(bonded.Id))
                    {
                        float strength = _strengthCalculator.CalculateBondStrength(current, bonded);
                        totalStrength += strength;
                        bondCount++;
                        current = bonded;
                        break;
                    }
                }

                if (bondCount == 0)
                    break;
            }

            if (bondCount > 0)
            {
                chain.AverageBondStrength = totalStrength / bondCount;
            }
        }

        /// <summary>
        /// Distributes energy to all tokens in a chain
        /// Based on formula: (chain.length - 1) * energyPerBond
        /// </summary>
        public void DistributeEnergyToChain(Token token, int energyPerBond)
        {
            var chain = FindChainContaining(token);
            if (chain == null || chain.Head == null || chain.Length < 2)
                return;

            int totalEnergy = (chain.Length - 1) * energyPerBond;
            int energyPerToken = totalEnergy / chain.Length;
            int remainder = totalEnergy % chain.Length; // given one each to the first tokens

            // Distribute energy to all tokens in chain
            var visited = new HashSet<long>();
            var queue = new Queue<Token>();
            queue.Enqueue(chain.Head);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current.Id))
                    continue;

                visited.Add(current.Id);
                current.Energy += energyPerToken + (remainder-- > 0 ? 1 : 0);

                foreach (var bonded in current.BondedTokens)
                {
                    if (!visited.Contains(bonded.Id))
                    {
                        queue.Enqueue(bonded);
                    }
                }
            }
        }

        /// <summary>
        /// Breaks a bond between two tokens
        /// </summary>
        /// <returns>True if the tokens were bonded and the bond was broken</returns>
        public bool BreakBond(Token token1, Token token2, long currentTick)
        {
            if (token1 == null || token2 == null)
                return false;

            if (!token1.BondedTokens.Contains(token2) && !token2.BondedTokens.Contains(token1))
                return false;

            // Remove bond references
            token1.BondedTokens.Remove(token2);
            token2.BondedTokens.Remove(token1);

            // Free bond sites
            var site1 = token1.BondSites.FirstOrDefault(s => s.BondedTo == token2);
            if (site1 != null)
            {
                site1.IsOccupied = false;
                site1.BondedTo = null;
            }

            var site2 = token2.BondSites.FirstOrDefault(s => s.BondedTo == token1);
            if (site2 != null)
            {
                site2.IsOccupied = false;
                site2.BondedTo = null;
            }

            // Update chains (may split)
            UpdateChainsAfterBreak(token1, token2, currentTick);
            return true;
        }

        /// <summary>
        /// Updates chains after a bond break (may split chain)
        /// </summary>
        private void UpdateChainsAfterBreak(Token token1, Token token2, long currentTick)
        {
            var chain = FindChainContaining(token1) ?? FindChainContaining(token2);
            if (chain == null)
                return;

            // Check if chain is now split
            if (!AreConnected(token1, token2))
            {
                // Split into two chains
                SplitChain(chain, token1, token2, currentTick);
            }
            else
            {
                // Chain still connected, just update metadata
                RefreshChain(chain, token1);
                chain.LastModifiedAt = currentTick;
            }
        }

        /// <summary>
        /// Checks if two tokens are still connected through bonds
        /// </summary>
        private bool AreConnected(Token token1, Token token2)
        {
            return CollectConnectedTokens(token1).Any(t => t.Id == token2.Id);
        }

        /// <summary>
        /// Splits a chain into two separate chains
        /// </summary>
        private void SplitChain(TokenChain originalChain, Token token1, Token token2, long currentTick)
        {
            _activeChains.Remove(originalChain);

            // Create two new chains (a token left without bonds is no longer in a chain)
            foreach (var token in new[] { token1, token2 })
            {
                var chain = CreateChainFromHead(token, originalChain.BondType, currentTick);
                if (chain != null)
                    _activeChains.Add(chain);
                else
                    ClearChainMembership(token);
            }
        }

        /// <summary>
        /// Creates a chain from the tokens connected to a token
        /// </summary>
        private TokenChain? CreateChainFromHead(Token head, BondType bondType, long currentTick)
        {
            if (head == null || head.BondedTokens.Count == 0)
                return null;

            var chain = new TokenChain
            {
                Id = _nextChainId++,
                BondType = bondType,
                CreatedAt = currentTick,
                LastModifiedAt = currentTick
            };

            RefreshChain(chain, head);
            return chain;
        }

        /// <summary>
        /// Gets the chain containing a specific token, or null if it is not in a chain
        /// </summary>
        public TokenChain? GetTokenChain(Token token)
        {
            return FindChainContaining(token);
        }

        /// <summary>
        /// Finds the chain containing a specific token
        /// </summary>
        private TokenChain? FindChainContaining(Token token)
        {
            if (token == null)
                return null;

            return _activeChains.FirstOrDefault(c => c.Id == token.ChainId);
        }

        /// <summary>
        /// Gets all active chains
        /// </summary>
        public List<TokenChain> GetActiveChains()
        {
            return new List<TokenChain>(_activeChains);
        }

        /// <summary>
        /// Removes a chain from active tracking
        /// </summary>
        public void RemoveChain(TokenChain chain)
        {
            if (chain != null)
            {
                _activeChains.Remove(chain);
            }
        }
    }
}

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

            // Step 1: Check grammar compatibility
            if (!_rulesEngine.CanBond(token1, token2))
                return false;

            // Step 2: Calculate bond strength
            float bondStrength = _strengthCalculator.CalculateBondStrength(token1, token2);
            if (bondStrength < 0.3f) // Minimum threshold
                return false;

            // Step 3: Check if bond can actually form
            if (!_strengthCalculator.CanFormBond(token1, token2))
                return false;

            // Step 4: Calculate energy cost
            int energyCost = _strengthCalculator.CalculateBondEnergyCost(token1, token2);
            int totalEnergy = token1.Energy + token2.Energy;

            if (totalEnergy < energyCost)
                return false; // Not enough energy

            // Step 5: Determine bond type
            BondType bondType = _strengthCalculator.DetermineBondType(token1, token2);

            // Step 6: Create the bond
            CreateBond(token1, token2, bondType, bondStrength, energyCost);

            // Step 7: Update or create chains
            UpdateChains(token1, token2, bondType, bondStrength, currentTick);

            // Step 8: Generate energy from bond formation (exothermic)
            int energyRelease = _strengthCalculator.CalculateBondEnergyRelease(token1, token2);
            if (energyRelease > 0)
            {
                DistributeEnergyToChain(token1, energyRelease);
            }

            return true;
        }

        /// <summary>
        /// Creates a bond between two tokens
        /// </summary>
        private void CreateBond(Token token1, Token token2, BondType bondType, float strength, int energyCost)
        {
            // Add tokens to each other's bonded lists
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Deduct energy cost (split between both tokens)
            int cost1 = energyCost / 2;
            int cost2 = energyCost - cost1;

            token1.Energy = Math.Max(0, token1.Energy - cost1);
            token2.Energy = Math.Max(0, token2.Energy - cost2);

            // Update bond sites (if available)
            UpdateBondSites(token1, token2, bondType);
        }

        /// <summary>
        /// Updates bond sites for the bonded tokens
        /// </summary>
        private void UpdateBondSites(Token token1, Token token2, BondType bondType)
        {
            // Find an available bond site on token1
            var site1 = token1.BondSites.FirstOrDefault(s => !s.IsOccupied);
            if (site1 != null)
            {
                site1.IsOccupied = true;
                site1.BondedTokenId = token2.Id;
            }

            // Find an available bond site on token2
            var site2 = token2.BondSites.FirstOrDefault(s => !s.IsOccupied);
            if (site2 != null)
            {
                site2.IsOccupied = true;
                site2.BondedTokenId = token1.Id;
            }
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
                AddTokenToChain(chain1, token2);
            }
            else if (chain1 == null && chain2 != null)
            {
                // Add token1 to chain2
                AddTokenToChain(chain2, token1);
            }
            else if (chain1 != null && chain2 != null && chain1 != chain2)
            {
                // Merge two chains
                MergeChains(chain1, chain2, currentTick);
            }
            // else: both tokens already in same chain, just update the chain
        }

        /// <summary>
        /// Creates a new token chain
        /// </summary>
        private void CreateNewChain(Token head, Token tail, BondType bondType, float strength, long currentTick)
        {
            var chain = new TokenChain
            {
                Id = _nextChainId++,
                Head = head,
                Tail = tail,
                Length = 2,
                BondType = bondType,
                AverageBondStrength = strength,
                IsValid = false, // Will be validated later
                CreatedAt = currentTick,
                LastModifiedAt = currentTick,
                StabilityScore = 0.5f
            };

            // Mark both tokens as part of this chain
            head.ChainId = chain.Id;
            head.ChainHead = head;
            tail.ChainId = chain.Id;
            tail.ChainHead = head;

            _activeChains.Add(chain);
        }

        /// <summary>
        /// Adds a token to an existing chain
        /// </summary>
        private void AddTokenToChain(TokenChain chain, Token token)
        {
            if (chain == null || token == null)
                return;

            token.ChainId = chain.Id;
            token.ChainHead = chain.Head;

            // Update chain metadata
            chain.Length++;
            chain.Tail = token; // Assume token is added to tail

            // Recalculate average bond strength
            RecalculateChainStrength(chain);
        }

        /// <summary>
        /// Merges two chains into one
        /// </summary>
        private void MergeChains(TokenChain chain1, TokenChain chain2, long currentTick)
        {
            if (chain1 == null || chain2 == null || chain1 == chain2)
                return;

            // Merge chain2 into chain1
            chain1.Length += chain2.Length;
            chain1.Tail = chain2.Tail;
            chain1.LastModifiedAt = currentTick;

            // Update all tokens in chain2 to point to chain1
            UpdateChainReferences(chain2.Head, chain1);

            // Remove chain2 from active chains
            _activeChains.Remove(chain2);

            // Recalculate merged chain strength
            RecalculateChainStrength(chain1);
        }

        /// <summary>
        /// Updates chain references for all tokens in a chain
        /// </summary>
        private void UpdateChainReferences(Token head, TokenChain newChain)
        {
            var visited = new HashSet<long>();
            var queue = new Queue<Token>();
            queue.Enqueue(head);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current.Id))
                    continue;

                visited.Add(current.Id);
                current.ChainId = newChain.Id;
                current.ChainHead = newChain.Head;

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
            if (chain == null || chain.Length < 2)
                return;

            int totalEnergy = (chain.Length - 1) * energyPerBond;
            int energyPerToken = totalEnergy / chain.Length;

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
                current.Energy += energyPerToken;

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
        public void BreakBond(Token token1, Token token2, long currentTick)
        {
            if (token1 == null || token2 == null)
                return;

            // Remove bond references
            token1.BondedTokens.Remove(token2);
            token2.BondedTokens.Remove(token1);

            // Free bond sites
            var site1 = token1.BondSites.FirstOrDefault(s => s.BondedTokenId == token2.Id);
            if (site1 != null)
            {
                site1.IsOccupied = false;
                site1.BondedTokenId = 0;
            }

            var site2 = token2.BondSites.FirstOrDefault(s => s.BondedTokenId == token1.Id);
            if (site2 != null)
            {
                site2.IsOccupied = false;
                site2.BondedTokenId = 0;
            }

            // Update chains (may split)
            UpdateChainsAfterBreak(token1, token2, currentTick);
        }

        /// <summary>
        /// Updates chains after a bond break (may split chain)
        /// </summary>
        private void UpdateChainsAfterBreak(Token token1, Token token2, long currentTick)
        {
            var chain = FindChainContaining(token1);
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
                chain.Length = CalculateChainLength(chain.Head);
                chain.LastModifiedAt = currentTick;
                RecalculateChainStrength(chain);
            }
        }

        /// <summary>
        /// Checks if two tokens are still connected through bonds
        /// </summary>
        private bool AreConnected(Token token1, Token token2)
        {
            var visited = new HashSet<long>();
            var queue = new Queue<Token>();
            queue.Enqueue(token1);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Id == token2.Id)
                    return true;

                if (visited.Contains(current.Id))
                    continue;

                visited.Add(current.Id);

                foreach (var bonded in current.BondedTokens)
                {
                    if (!visited.Contains(bonded.Id))
                    {
                        queue.Enqueue(bonded);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Splits a chain into two separate chains
        /// </summary>
        private void SplitChain(TokenChain originalChain, Token token1, Token token2, long currentTick)
        {
            _activeChains.Remove(originalChain);

            // Create two new chains
            var chain1 = CreateChainFromHead(token1, currentTick);
            var chain2 = CreateChainFromHead(token2, currentTick);

            if (chain1 != null)
                _activeChains.Add(chain1);
            if (chain2 != null)
                _activeChains.Add(chain2);
        }

        /// <summary>
        /// Creates a chain starting from a head token
        /// </summary>
        private TokenChain CreateChainFromHead(Token head, long currentTick)
        {
            if (head == null || head.BondedTokens.Count == 0)
                return null;

            var chain = new TokenChain
            {
                Id = _nextChainId++,
                Head = head,
                CreatedAt = currentTick,
                LastModifiedAt = currentTick
            };

            // Calculate chain properties
            chain.Length = CalculateChainLength(head);
            chain.Tail = FindTail(head);

            // Update all tokens in chain
            UpdateChainReferences(head, chain);
            RecalculateChainStrength(chain);

            return chain;
        }

        /// <summary>
        /// Calculates the length of a chain
        /// </summary>
        private int CalculateChainLength(Token head)
        {
            var visited = new HashSet<long>();
            var queue = new Queue<Token>();
            queue.Enqueue(head);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current.Id))
                    continue;

                visited.Add(current.Id);

                foreach (var bonded in current.BondedTokens)
                {
                    if (!visited.Contains(bonded.Id))
                    {
                        queue.Enqueue(bonded);
                    }
                }
            }

            return visited.Count;
        }

        /// <summary>
        /// Finds the tail token of a chain
        /// </summary>
        private Token FindTail(Token head)
        {
            Token tail = head;
            var visited = new HashSet<long>();

            while (tail.BondedTokens.Count > 0)
            {
                visited.Add(tail.Id);
                bool foundNext = false;

                foreach (var bonded in tail.BondedTokens)
                {
                    if (!visited.Contains(bonded.Id))
                    {
                        tail = bonded;
                        foundNext = true;
                        break;
                    }
                }

                if (!foundNext)
                    break;
            }

            return tail;
        }

        /// <summary>
        /// Finds the chain containing a specific token
        /// </summary>
        private TokenChain FindChainContaining(Token token)
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

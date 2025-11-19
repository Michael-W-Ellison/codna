using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Registry for tracking all active and stable token chains.
    /// Based on section 4.3 of the design specification.
    /// </summary>
    public class ChainRegistry
    {
        private readonly Dictionary<long, TokenChain> _chains;
        private readonly List<TokenChain> _stableChains;
        private readonly ChainStabilityCalculator _stabilityCalculator;
        private long _nextChainId;

        // Stability thresholds
        private const float STABILITY_THRESHOLD = 0.5f;      // Minimum to be considered "stable"
        private const float UNSTABLE_THRESHOLD = 0.2f;       // Below this = remove from registry
        private const long STABLE_AGE_REQUIREMENT = 50;      // Ticks before chain can be "stable"

        public ChainRegistry(ChainStabilityCalculator stabilityCalculator)
        {
            _chains = new Dictionary<long, TokenChain>();
            _stableChains = new List<TokenChain>();
            _stabilityCalculator = stabilityCalculator;
            _nextChainId = 1;
        }

        /// <summary>
        /// Registers a new chain
        /// </summary>
        public long RegisterChain(TokenChain chain)
        {
            if (chain == null)
                return -1;

            long id = _nextChainId++;
            chain.Id = id;
            _chains[id] = chain;

            return id;
        }

        /// <summary>
        /// Gets a chain by ID
        /// </summary>
        public TokenChain GetChain(long id)
        {
            return _chains.TryGetValue(id, out var chain) ? chain : null;
        }

        /// <summary>
        /// Removes a chain from the registry
        /// </summary>
        public bool RemoveChain(long id)
        {
            if (_chains.TryGetValue(id, out var chain))
            {
                _chains.Remove(id);
                _stableChains.Remove(chain);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates stability scores for all chains
        /// </summary>
        public void UpdateAllStabilities(long currentTick)
        {
            var chainsToRemove = new List<long>();

            foreach (var kvp in _chains)
            {
                var chain = kvp.Value;

                // Calculate stability
                float stability = _stabilityCalculator?.CalculateStability(chain, currentTick) ?? 0.5f;
                chain.StabilityScore = stability;

                // Update stable chains list
                UpdateStableStatus(chain, currentTick);

                // Mark extremely unstable chains for removal
                if (stability < UNSTABLE_THRESHOLD)
                {
                    chainsToRemove.Add(kvp.Key);
                }
            }

            // Remove unstable chains
            foreach (var id in chainsToRemove)
            {
                RemoveChain(id);
            }
        }

        /// <summary>
        /// Updates whether a chain is considered "stable"
        /// </summary>
        private void UpdateStableStatus(TokenChain chain, long currentTick)
        {
            long age = currentTick - chain.LastModifiedTick;
            bool meetsAgeRequirement = age >= STABLE_AGE_REQUIREMENT;
            bool meetsStabilityRequirement = chain.StabilityScore >= STABILITY_THRESHOLD;

            bool isStable = meetsAgeRequirement && meetsStabilityRequirement;

            if (isStable && !_stableChains.Contains(chain))
            {
                _stableChains.Add(chain);
            }
            else if (!isStable && _stableChains.Contains(chain))
            {
                _stableChains.Remove(chain);
            }
        }

        /// <summary>
        /// Gets all active chains
        /// </summary>
        public List<TokenChain> GetAllChains()
        {
            return _chains.Values.ToList();
        }

        /// <summary>
        /// Gets only stable chains
        /// </summary>
        public List<TokenChain> GetStableChains()
        {
            return new List<TokenChain>(_stableChains);
        }

        /// <summary>
        /// Gets chains sorted by stability (descending)
        /// </summary>
        public List<TokenChain> GetChainsByStability()
        {
            return _chains.Values.OrderByDescending(c => c.StabilityScore).ToList();
        }

        /// <summary>
        /// Gets chains sorted by length (descending)
        /// </summary>
        public List<TokenChain> GetChainsByLength()
        {
            return _chains.Values.OrderByDescending(c => c.Length).ToList();
        }

        /// <summary>
        /// Finds chains containing a specific token
        /// </summary>
        public List<TokenChain> FindChainsContainingToken(Token token)
        {
            if (token == null)
                return new List<TokenChain>();

            return _chains.Values
                .Where(c => c.Tokens != null && c.Tokens.Contains(token))
                .ToList();
        }

        /// <summary>
        /// Finds the longest chain
        /// </summary>
        public TokenChain GetLongestChain()
        {
            return _chains.Values
                .OrderByDescending(c => c.Length)
                .FirstOrDefault();
        }

        /// <summary>
        /// Finds the most stable chain
        /// </summary>
        public TokenChain GetMostStableChain()
        {
            return _chains.Values
                .OrderByDescending(c => c.StabilityScore)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets chains that are valid according to grammar
        /// </summary>
        public List<TokenChain> GetValidChains()
        {
            return _chains.Values
                .Where(c => c.IsValid)
                .ToList();
        }

        /// <summary>
        /// Gets registry statistics
        /// </summary>
        public ChainRegistryStatistics GetStatistics()
        {
            var allChains = _chains.Values.ToList();

            if (allChains.Count == 0)
            {
                return new ChainRegistryStatistics
                {
                    TotalChains = 0,
                    StableChains = 0,
                    ValidChains = 0,
                    AverageLength = 0,
                    AverageStability = 0,
                    LongestChainLength = 0,
                    TotalTokensInChains = 0
                };
            }

            return new ChainRegistryStatistics
            {
                TotalChains = allChains.Count,
                StableChains = _stableChains.Count,
                ValidChains = allChains.Count(c => c.IsValid),
                AverageLength = allChains.Average(c => c.Length),
                AverageStability = allChains.Average(c => c.StabilityScore),
                LongestChainLength = allChains.Max(c => c.Length),
                TotalTokensInChains = allChains.Sum(c => c.Length)
            };
        }

        /// <summary>
        /// Merges two chains
        /// </summary>
        public bool MergeChains(long id1, long id2, long currentTick)
        {
            var chain1 = GetChain(id1);
            var chain2 = GetChain(id2);

            if (chain1 == null || chain2 == null)
                return false;

            // Merge chain2 into chain1
            foreach (var token in chain2.Tokens)
            {
                chain1.AddToken(token);
            }

            chain1.LastModifiedTick = currentTick;

            // Remove chain2
            RemoveChain(id2);

            return true;
        }

        /// <summary>
        /// Prunes chains that haven't been modified for a long time and are unstable
        /// </summary>
        public int PruneStaleChains(long currentTick, long maxAge = 1000)
        {
            var chainsToPrune = _chains
                .Where(kvp =>
                {
                    var chain = kvp.Value;
                    long age = currentTick - chain.LastModifiedTick;
                    return age > maxAge && chain.StabilityScore < STABILITY_THRESHOLD;
                })
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var id in chainsToPrune)
            {
                RemoveChain(id);
            }

            return chainsToPrune.Count;
        }

        /// <summary>
        /// Clears all chains
        /// </summary>
        public void Clear()
        {
            _chains.Clear();
            _stableChains.Clear();
            _nextChainId = 1;
        }

        /// <summary>
        /// Gets the total number of chains
        /// </summary>
        public int Count => _chains.Count;

        /// <summary>
        /// Gets the number of stable chains
        /// </summary>
        public int StableCount => _stableChains.Count;
    }

    /// <summary>
    /// Statistics about the chain registry
    /// </summary>
    public class ChainRegistryStatistics
    {
        public int TotalChains { get; set; }
        public int StableChains { get; set; }
        public int ValidChains { get; set; }
        public double AverageLength { get; set; }
        public double AverageStability { get; set; }
        public int LongestChainLength { get; set; }
        public int TotalTokensInChains { get; set; }

        public override string ToString()
        {
            return $"Chains: {TotalChains} total, {StableChains} stable, {ValidChains} valid | " +
                   $"Avg Length: {AverageLength:F1}, Avg Stability: {AverageStability:F2} | " +
                   $"Longest: {LongestChainLength}";
        }
    }
}

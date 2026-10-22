using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Template-directed replication: a chain that has passed validation assembles a copy of
    /// itself from free (unbonded) tokens nearby. The copy matches the parent's token types in
    /// order; its values come from the tokens it is built from. With probability
    /// ReplicationErrorRate one position takes a token of a different type (a copying error).
    /// </summary>
    public class ChainReplicator
    {
        private readonly BondingManager _bondingManager;
        private readonly Grid _grid;
        private readonly SimulationConfig _config;
        private readonly Random _random;

        public ChainReplicator(BondingManager bondingManager, Grid grid, SimulationConfig config, Random? random = null)
        {
            _bondingManager = bondingManager ?? throw new ArgumentNullException(nameof(bondingManager));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? new Random();
        }

        /// <summary>
        /// Attempts to build a copy of the parent chain from free tokens within ReplicationRange
        /// cells of any of its tokens. Returns the new chain, or null if the needed tokens are not
        /// available or the copy could not be bonded (no bonds are left behind on failure).
        /// </summary>
        public TokenChain? TryReplicate(TokenChain parent, long currentTick)
        {
            var template = parent.Tokens;
            if (template.Count < 2)
                return null;

            var available = FindFreeTokens(parent);
            var byType = available
                .GroupBy(t => t.Type)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Pick one free token per template position
            var selected = new List<Token>(template.Count);
            foreach (var templateToken in template)
            {
                Token? pick;
                if (_random.NextDouble() < _config.ReplicationErrorRate)
                    pick = TakeRandom(available.Where(t => t.Type != templateToken.Type && !selected.Contains(t)).ToList());
                else
                    pick = byType.TryGetValue(templateToken.Type, out var matches)
                        ? TakeRandom(matches.Where(t => !selected.Contains(t)).ToList())
                        : null;

                if (pick == null)
                    return null;

                selected.Add(pick);
            }

            // Bond the copy in order, undoing it if any bond cannot form
            for (int i = 1; i < selected.Count; i++)
            {
                if (!_bondingManager.AttemptBond(selected[i - 1], selected[i], currentTick))
                {
                    for (int j = 1; j < i; j++)
                        _bondingManager.BreakBond(selected[j - 1], selected[j], currentTick);
                    return null;
                }
            }

            return _bondingManager.GetTokenChain(selected[0]);
        }

        /// <summary>
        /// Free tokens (active, not bonded, not in the parent) within ReplicationRange cells of the parent's tokens
        /// </summary>
        private List<Token> FindFreeTokens(TokenChain parent)
        {
            var parentTokens = new HashSet<Token>(parent.Tokens);
            var found = new HashSet<Token>();

            foreach (var position in parent.Tokens.Select(t => t.Position).Distinct())
            {
                foreach (var token in _grid.GetTokensInRadius(position, _config.ReplicationRange))
                {
                    if (token.IsActive && token.BondedTokens.Count == 0 && !parentTokens.Contains(token))
                        found.Add(token);
                }
            }

            return found.ToList();
        }

        private Token? TakeRandom(List<Token> tokens)
        {
            return tokens.Count == 0 ? null : tokens[_random.Next(tokens.Count)];
        }
    }
}

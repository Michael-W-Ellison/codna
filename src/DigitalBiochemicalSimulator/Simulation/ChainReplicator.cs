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
    /// order; its values come from the tokens it is built from. Copying errors:
    /// - substitution (ReplicationErrorRate, per position, decided when the copy starts): the
    ///   position takes a token of a different type
    /// - insertion (ReplicationInsertionRate, per copy): a run of 1-2 positions is duplicated in place
    /// - deletion (ReplicationDeletionRate, per copy): a run of 1-2 positions is left out
    ///
    /// A copy is built over several ticks: each tick it reserves free tokens of the types it still
    /// needs from within ReplicationRange cells of the parent's tokens. Reserved tokens cannot bond
    /// or be recycled. When every position is filled the copy is bonded in order; if it is not
    /// complete within ReplicationTimeout ticks, or the parent chain is gone, its tokens are released.
    /// </summary>
    public class ChainReplicator
    {
        private readonly BondingManager _bondingManager;
        private readonly Grid _grid;
        private readonly SimulationConfig _config;
        private readonly Random _random;
        private readonly List<CopyInProgress> _copies = new List<CopyInProgress>();
        private long _nextCopyId = 1;

        public ChainReplicator(BondingManager bondingManager, Grid grid, SimulationConfig config, Random? random = null)
        {
            _bondingManager = bondingManager ?? throw new ArgumentNullException(nameof(bondingManager));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? new Random();
        }

        /// <summary>
        /// Number of copies being built
        /// </summary>
        public int CopiesInProgress => _copies.Count;

        /// <summary>
        /// Whether the chain is already building a copy
        /// </summary>
        public bool IsCopying(TokenChain parent) => _copies.Any(c => c.Parent == parent);

        /// <summary>
        /// Starts building a copy of the parent chain and gathers whatever tokens are available now.
        /// Returns false if the chain is too short or already building a copy.
        /// </summary>
        public bool StartCopy(TokenChain parent)
        {
            if (parent.Tokens.Count < 2 || IsCopying(parent))
                return false;

            var template = MutateTemplate(parent.Tokens.Select(t => t.Type).ToArray(), _random,
                _config.ReplicationInsertionRate, _config.ReplicationDeletionRate);
            var substitutions = template.Select(_ => _random.NextDouble() < _config.ReplicationErrorRate).ToArray();
            var copy = new CopyInProgress(_nextCopyId++, parent, template, substitutions);
            _copies.Add(copy);
            Gather(copy);
            return true;
        }

        /// <summary>
        /// Advances every copy in progress: gathers tokens, bonds copies whose positions are all
        /// filled, and abandons copies that timed out, lost their parent, or lost a reserved token.
        /// Returns the completed copies with their parent chains.
        /// </summary>
        public List<(TokenChain Copy, TokenChain Parent)> Advance(long currentTick, Func<TokenChain, bool> parentIsAlive)
        {
            var completed = new List<(TokenChain Copy, TokenChain Parent)>();

            foreach (var copy in _copies.ToList())
            {
                copy.Age++;

                if (!parentIsAlive(copy.Parent) || copy.Age > Math.Max(1, _config.ReplicationTimeout) || !copy.ReservationsIntact())
                {
                    Abandon(copy);
                    continue;
                }

                Gather(copy);
                if (!copy.IsFilled)
                    continue;

                var chain = Assemble(copy, currentTick);
                _copies.Remove(copy);
                if (chain != null)
                    completed.Add((chain, copy.Parent));
            }

            return completed;
        }

        /// <summary>
        /// Releases the tokens of every copy in progress
        /// </summary>
        public void CancelAll()
        {
            foreach (var copy in _copies.ToList())
                Abandon(copy);
        }

        /// <summary>
        /// Attempts to build a complete copy immediately from the free tokens available now.
        /// Returns the new chain, or null if the needed tokens are not available or the copy could
        /// not be bonded (nothing is left reserved or bonded on failure).
        /// </summary>
        public TokenChain? TryReplicate(TokenChain parent, long currentTick)
        {
            if (!StartCopy(parent))
                return null;

            var copy = _copies[_copies.Count - 1];
            _copies.Remove(copy);

            if (!copy.IsFilled)
            {
                copy.Release();
                return null;
            }

            return Assemble(copy, currentTick);
        }

        /// <summary>
        /// Applies insertion and deletion copying errors to a template. Insertion duplicates a
        /// random run of 1-2 positions directly after itself; deletion removes a random run of
        /// 1-2 positions. A template never drops below 2 positions.
        /// </summary>
        internal static TokenType[] MutateTemplate(TokenType[] template, Random random, double insertionRate, double deletionRate)
        {
            var result = template.ToList();

            if (random.NextDouble() < insertionRate)
            {
                int length = Math.Min(result.Count, 1 + random.Next(2));
                int start = random.Next(result.Count - length + 1);
                result.InsertRange(start + length, result.GetRange(start, length));
            }

            if (random.NextDouble() < deletionRate)
            {
                int length = Math.Min(result.Count - 2, 1 + random.Next(2));
                if (length > 0)
                    result.RemoveRange(random.Next(result.Count - length + 1), length);
            }

            return result.ToArray();
        }

        private void Gather(CopyInProgress copy)
        {
            var available = FindFreeTokens(copy.Parent);

            for (int i = 0; i < copy.Slots.Length; i++)
            {
                if (copy.Slots[i] != null)
                    continue;

                var needed = copy.TemplateTypes[i];
                bool copyingError = copy.Substitutions[i];
                var candidates = available.Where(t => !t.IsReserved && (copyingError ? t.Type != needed : t.Type == needed)).ToList();
                if (candidates.Count == 0)
                    continue;

                var pick = candidates[_random.Next(candidates.Count)];
                pick.ReservedBy = copy.Id;
                copy.Slots[i] = pick;
            }
        }

        /// <summary>
        /// Bonds a filled copy in order; on failure undoes the bonds. Releases the reservations either way.
        /// </summary>
        private TokenChain? Assemble(CopyInProgress copy, long currentTick)
        {
            var tokens = copy.Slots.Select(t => t!).ToList();
            copy.Release();

            for (int i = 1; i < tokens.Count; i++)
            {
                if (!_bondingManager.AttemptBond(tokens[i - 1], tokens[i], currentTick))
                {
                    for (int j = 1; j < i; j++)
                        _bondingManager.BreakBond(tokens[j - 1], tokens[j], currentTick);
                    return null;
                }
            }

            return _bondingManager.GetTokenChain(tokens[0]);
        }

        private void Abandon(CopyInProgress copy)
        {
            copy.Release();
            _copies.Remove(copy);
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

        /// <summary>
        /// A copy being assembled: the token types to copy (the parent's, after insertion and
        /// deletion errors) and the positions that take a different type, all fixed when the copy
        /// starts, and the tokens reserved for each position so far
        /// </summary>
        private class CopyInProgress
        {
            public long Id { get; }
            public TokenChain Parent { get; }
            public TokenType[] TemplateTypes { get; }
            public bool[] Substitutions { get; }   // positions that take a token of a different type
            public Token?[] Slots { get; }
            public int Age { get; set; }

            public CopyInProgress(long id, TokenChain parent, TokenType[] templateTypes, bool[] substitutions)
            {
                Id = id;
                Parent = parent;
                TemplateTypes = templateTypes;
                Substitutions = substitutions;
                Slots = new Token?[TemplateTypes.Length];
            }

            public bool IsFilled => Slots.All(t => t != null);

            /// <summary>
            /// False if a reserved token was removed from the simulation (its reservation was cleared)
            /// or deactivated
            /// </summary>
            public bool ReservationsIntact() => Slots.All(t => t == null || (t.ReservedBy == Id && t.IsActive));

            public void Release()
            {
                foreach (var token in Slots)
                {
                    if (token != null && token.ReservedBy == Id)
                        token.ReservedBy = 0;
                }
            }
        }
    }
}

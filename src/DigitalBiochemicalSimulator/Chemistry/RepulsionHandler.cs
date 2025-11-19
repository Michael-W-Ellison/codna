using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Handles repulsion between mutually exclusive tokens.
    /// Based on section 3.6 of the design specification.
    ///
    /// Repulsion occurs when:
    /// - Tokens have highly incompatible types (e.g., conflicting keywords)
    /// - Grammar forbids their coexistence
    /// - Token types are mutually exclusive
    ///
    /// Resolution: Smaller token or chain is displaced to a neighboring cell
    /// </summary>
    public class RepulsionHandler
    {
        private readonly Grid _grid;
        private readonly BondRulesEngine _rulesEngine;
        private readonly Dictionary<TokenType, List<TokenType>> _repulsionRules;
        private readonly Random _random;

        public RepulsionHandler(Grid grid, BondRulesEngine rulesEngine)
        {
            _grid = grid;
            _rulesEngine = rulesEngine;
            _repulsionRules = new Dictionary<TokenType, List<TokenType>>();
            _random = new Random();

            InitializeRepulsionRules();
        }

        /// <summary>
        /// Initializes rules for which token types repel each other
        /// </summary>
        private void InitializeRepulsionRules()
        {
            // Conflicting control keywords
            AddRepulsion(TokenType.KEYWORD_IF, TokenType.KEYWORD_WHILE);
            AddRepulsion(TokenType.KEYWORD_IF, TokenType.KEYWORD_FOR);
            AddRepulsion(TokenType.KEYWORD_WHILE, TokenType.KEYWORD_FOR);

            // Conflicting declaration keywords
            AddRepulsion(TokenType.KEYWORD_VAR, TokenType.KEYWORD_CONST);
            AddRepulsion(TokenType.KEYWORD_VAR, TokenType.KEYWORD_LET);
            AddRepulsion(TokenType.KEYWORD_CONST, TokenType.KEYWORD_LET);

            // Conflicting type keywords
            AddRepulsion(TokenType.TYPE_INT, TokenType.TYPE_FLOAT);
            AddRepulsion(TokenType.TYPE_INT, TokenType.TYPE_STRING);
            AddRepulsion(TokenType.TYPE_FLOAT, TokenType.TYPE_STRING);
            AddRepulsion(TokenType.TYPE_BOOL, TokenType.TYPE_INT);

            // Conflicting operators
            AddRepulsion(TokenType.OPERATOR_AND, TokenType.OPERATOR_OR);
            AddRepulsion(TokenType.OPERATOR_PLUS, TokenType.OPERATOR_MINUS);

            // Opening/closing mismatches
            AddRepulsion(TokenType.PAREN_OPEN, TokenType.BRACE_CLOSE);
            AddRepulsion(TokenType.PAREN_OPEN, TokenType.BRACKET_CLOSE);
            AddRepulsion(TokenType.BRACE_OPEN, TokenType.PAREN_CLOSE);
            AddRepulsion(TokenType.BRACE_OPEN, TokenType.BRACKET_CLOSE);
            AddRepulsion(TokenType.BRACKET_OPEN, TokenType.PAREN_CLOSE);
            AddRepulsion(TokenType.BRACKET_OPEN, TokenType.BRACE_CLOSE);

            // True/False conflict
            AddRepulsion(TokenType.KEYWORD_TRUE, TokenType.KEYWORD_FALSE);
        }

        /// <summary>
        /// Adds a bidirectional repulsion rule
        /// </summary>
        private void AddRepulsion(TokenType type1, TokenType type2)
        {
            if (!_repulsionRules.ContainsKey(type1))
                _repulsionRules[type1] = new List<TokenType>();

            if (!_repulsionRules.ContainsKey(type2))
                _repulsionRules[type2] = new List<TokenType>();

            if (!_repulsionRules[type1].Contains(type2))
                _repulsionRules[type1].Add(type2);

            if (!_repulsionRules[type2].Contains(type1))
                _repulsionRules[type2].Add(type1);
        }

        /// <summary>
        /// Checks if two tokens repel each other
        /// </summary>
        public bool DoTokensRepel(Token token1, Token token2)
        {
            if (token1 == null || token2 == null)
                return false;

            // Check explicit repulsion rules
            if (_repulsionRules.TryGetValue(token1.Type, out var repelledTypes))
            {
                if (repelledTypes.Contains(token2.Type))
                    return true;
            }

            // Check if grammar explicitly forbids coexistence
            // (if they can't bond and have high electronegativity difference)
            if (_rulesEngine != null && !_rulesEngine.CanBond(token1, token2))
            {
                float en1 = ElectronegativityTable.GetValue(token1.Type);
                float en2 = ElectronegativityTable.GetValue(token2.Type);
                float difference = Math.Abs(en1 - en2);

                // Large electronegativity difference + no bonding = repulsion
                if (difference > 0.7f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks for repulsion between tokens in a cell and resolves conflicts
        /// </summary>
        public void CheckAndResolveRepulsion(Cell cell)
        {
            if (cell == null || cell.Tokens.Count < 2)
                return;

            var tokensToDisplace = new List<Token>();

            // Check all pairs of tokens in the cell
            for (int i = 0; i < cell.Tokens.Count; i++)
            {
                for (int j = i + 1; j < cell.Tokens.Count; j++)
                {
                    var token1 = cell.Tokens[i];
                    var token2 = cell.Tokens[j];

                    if (DoTokensRepel(token1, token2))
                    {
                        // Determine which token to displace (smaller one)
                        var tokenToDisplace = DetermineDisplacedToken(token1, token2);
                        if (!tokensToDisplace.Contains(tokenToDisplace))
                        {
                            tokensToDisplace.Add(tokenToDisplace);
                        }
                    }
                }
            }

            // Displace conflicting tokens
            foreach (var token in tokensToDisplace)
            {
                DisplaceToken(token, cell);
            }
        }

        /// <summary>
        /// Determines which token should be displaced based on size/chain membership
        /// </summary>
        private Token DetermineDisplacedToken(Token token1, Token token2)
        {
            // Factor 1: Chain membership (unchained tokens are easier to displace)
            bool inChain1 = token1.ChainId > 0;
            bool inChain2 = token2.ChainId > 0;

            if (inChain1 && !inChain2)
                return token2; // Displace unchained token

            if (!inChain1 && inChain2)
                return token1; // Displace unchained token

            // Factor 2: Chain length (smaller chain is displaced)
            int chainLength1 = GetChainLength(token1);
            int chainLength2 = GetChainLength(token2);

            if (chainLength1 < chainLength2)
                return token1;
            else if (chainLength2 < chainLength1)
                return token2;

            // Factor 3: Energy (lower energy token is displaced)
            if (token1.Energy < token2.Energy)
                return token1;
            else if (token2.Energy < token1.Energy)
                return token2;

            // Factor 4: Mass (lighter token is displaced)
            if (token1.Mass < token2.Mass)
                return token1;
            else if (token2.Mass < token1.Mass)
                return token2;

            // Tie-breaker: random
            return _random.Next(2) == 0 ? token1 : token2;
        }

        /// <summary>
        /// Gets the length of the chain a token belongs to
        /// </summary>
        private int GetChainLength(Token token)
        {
            if (token.ChainId == 0 || token.ChainHead == null)
                return 1; // Not in a chain

            // Count bonded tokens
            var visited = new HashSet<long>();
            var queue = new Queue<Token>();
            queue.Enqueue(token.ChainHead);

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
        /// Displaces a token to a neighboring cell
        /// </summary>
        private void DisplaceToken(Token token, Cell currentCell)
        {
            if (token == null || currentCell == null)
                return;

            // Get neighboring cells (8 horizontal neighbors)
            var neighbors = _grid.GetNeighbors(currentCell.Position)
                .Where(n => n.Position.Y == currentCell.Position.Y) // Same altitude
                .ToList();

            if (neighbors.Count == 0)
                return; // No neighbors available

            // Find the least crowded neighbor
            Cell targetCell = FindLeastCrowdedCell(neighbors);

            if (targetCell == null)
                return; // All neighbors full

            // Move token to target cell
            _grid.RemoveToken(token);
            token.Position = targetCell.Position;
            _grid.AddToken(token);

            // Token loses some energy from displacement
            const int REPULSION_ENERGY_COST = 3;
            token.Energy = Math.Max(0, token.Energy - REPULSION_ENERGY_COST);
        }

        /// <summary>
        /// Finds the least crowded neighboring cell
        /// </summary>
        private Cell FindLeastCrowdedCell(List<Cell> cells)
        {
            if (cells == null || cells.Count == 0)
                return null;

            Cell leastCrowded = null;
            int minCount = int.MaxValue;

            foreach (var cell in cells)
            {
                if (cell.Tokens.Count < cell.Capacity && cell.Tokens.Count < minCount)
                {
                    minCount = cell.Tokens.Count;
                    leastCrowded = cell;
                }
            }

            // If all cells are full, pick a random one
            if (leastCrowded == null && cells.Count > 0)
            {
                leastCrowded = cells[_random.Next(cells.Count)];
            }

            return leastCrowded;
        }

        /// <summary>
        /// Checks if a specific token type repels another
        /// </summary>
        public bool IsRepelledBy(TokenType type1, TokenType type2)
        {
            if (_repulsionRules.TryGetValue(type1, out var repelledTypes))
            {
                return repelledTypes.Contains(type2);
            }
            return false;
        }

        /// <summary>
        /// Gets all token types that repel a given type
        /// </summary>
        public List<TokenType> GetRepelledTypes(TokenType type)
        {
            if (_repulsionRules.TryGetValue(type, out var repelledTypes))
            {
                return new List<TokenType>(repelledTypes);
            }
            return new List<TokenType>();
        }

        /// <summary>
        /// Adds a custom repulsion rule at runtime
        /// </summary>
        public void AddCustomRepulsion(TokenType type1, TokenType type2)
        {
            AddRepulsion(type1, type2);
        }

        /// <summary>
        /// Removes a repulsion rule
        /// </summary>
        public void RemoveRepulsion(TokenType type1, TokenType type2)
        {
            if (_repulsionRules.TryGetValue(type1, out var list1))
            {
                list1.Remove(type2);
            }

            if (_repulsionRules.TryGetValue(type2, out var list2))
            {
                list2.Remove(type1);
            }
        }
    }
}

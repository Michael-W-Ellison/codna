using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Grammar;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Determines if tokens can bond based on grammar rules.
    /// Based on section 3.6 of the design specification.
    /// </summary>
    public class BondRulesEngine
    {
        private readonly List<GrammarRule> _grammarRules;
        private readonly Dictionary<string, GrammarRule> _ruleIndex;

        public BondRulesEngine(List<GrammarRule> grammarRules)
        {
            _grammarRules = grammarRules ?? new List<GrammarRule>();
            _ruleIndex = new Dictionary<string, GrammarRule>();

            // Index rules by ID for fast lookup
            foreach (var rule in _grammarRules)
            {
                if (!string.IsNullOrEmpty(rule.Id))
                {
                    _ruleIndex[rule.Id] = rule;
                }
            }
        }

        /// <summary>
        /// Checks if two tokens can bond according to grammar rules
        /// </summary>
        public bool CanBond(Token token1, Token token2)
        {
            if (token1 == null || token2 == null)
                return false;

            if (!token1.IsActive || !token2.IsActive)
                return false;

            // Check if any grammar rule allows this bond
            foreach (var rule in _grammarRules)
            {
                if (rule.CanBond(token1, token2))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a token sequence matches a grammar rule
        /// </summary>
        public bool MatchesGrammar(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return false;

            // Try to match against any grammar rule
            foreach (var rule in _grammarRules)
            {
                if (rule.Matches(tokens))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the best matching grammar rule for a token sequence
        /// </summary>
        public GrammarRule FindBestMatch(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return null;

            GrammarRule bestMatch = null;
            int maxMatchLength = 0;

            foreach (var rule in _grammarRules)
            {
                if (rule.Matches(tokens))
                {
                    // Prefer rules that match more patterns
                    if (rule.Pattern.Count > maxMatchLength)
                    {
                        maxMatchLength = rule.Pattern.Count;
                        bestMatch = rule;
                    }
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Gets the bond type for two tokens based on grammar rules
        /// </summary>
        public BondType GetBondType(Token token1, Token token2)
        {
            if (!CanBond(token1, token2))
                return BondType.VAN_DER_WAALS; // Default weak bond

            // Find the first matching rule
            foreach (var rule in _grammarRules)
            {
                if (rule.CanBond(token1, token2))
                {
                    return rule.BondType;
                }
            }

            return BondType.VAN_DER_WAALS;
        }

        /// <summary>
        /// Gets the base bond strength for two tokens based on grammar rules
        /// </summary>
        public float GetBaseBondStrength(Token token1, Token token2)
        {
            if (!CanBond(token1, token2))
                return 0.1f; // Minimal strength

            // Find the first matching rule
            foreach (var rule in _grammarRules)
            {
                if (rule.CanBond(token1, token2))
                {
                    return rule.BondStrength;
                }
            }

            return 0.5f; // Default medium strength
        }

        /// <summary>
        /// Gets the validation level for a token sequence
        /// </summary>
        public ValidationLevel GetValidationLevel(List<Token> tokens)
        {
            var matchingRule = FindBestMatch(tokens);
            return matchingRule?.ValidationLevel ?? ValidationLevel.DEFERRED;
        }

        /// <summary>
        /// Validates a token chain based on grammar rules
        /// </summary>
        public bool ValidateChain(TokenChain chain)
        {
            if (chain == null || chain.Length == 0)
                return false;

            // Collect all tokens in the chain
            var tokens = new List<Token>();
            var current = chain.Head;

            while (current != null)
            {
                tokens.Add(current);

                // Move to next bonded token
                if (current.BondedTokens.Count > 0)
                {
                    current = current.BondedTokens[0];
                }
                else
                {
                    break;
                }
            }

            return MatchesGrammar(tokens);
        }

        /// <summary>
        /// Gets all grammar rules
        /// </summary>
        public List<GrammarRule> GetAllRules()
        {
            return new List<GrammarRule>(_grammarRules);
        }

        /// <summary>
        /// Gets a grammar rule by ID
        /// </summary>
        public GrammarRule GetRuleById(string id)
        {
            return _ruleIndex.TryGetValue(id, out var rule) ? rule : null;
        }

        /// <summary>
        /// Adds a new grammar rule
        /// </summary>
        public void AddRule(GrammarRule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.Id))
                return;

            if (!_ruleIndex.ContainsKey(rule.Id))
            {
                _grammarRules.Add(rule);
                _ruleIndex[rule.Id] = rule;
            }
        }

        /// <summary>
        /// Removes a grammar rule by ID
        /// </summary>
        public bool RemoveRule(string id)
        {
            if (_ruleIndex.TryGetValue(id, out var rule))
            {
                _grammarRules.Remove(rule);
                _ruleIndex.Remove(id);
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"BondRulesEngine({_grammarRules.Count} rules)";
        }
    }
}

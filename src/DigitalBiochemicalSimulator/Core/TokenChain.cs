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
        public long Id { get; set; }
        public Token Head { get; set; }
        public Token Tail { get; set; }
        public int Length { get; set; }
        public int TotalMass { get; set; }
        public int TotalEnergy { get; set; }
        public float StabilityScore { get; set; }
        public long LastModifiedTick { get; set; }
        public long LastModifiedAt { get; set; } // Alias for compatibility
        public long CreatedAt { get; set; }
        public bool IsValid { get; set; }
        public BondType BondType { get; set; }

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

        public TokenChain()
        {
            Tokens = new List<Token>();
            StabilityScore = 0.0f;
            IsValid = false;
            AverageBondStrength = 0.0f;
            TicksSinceModified = 0;
            BondType = BondType.COVALENT;
        }

        public TokenChain(Token headToken) : this()
        {
            Id = 0; // Will be set by BondingManager
            Head = headToken;
            Tail = headToken;
            Length = 1;
            TotalMass = headToken.Mass;
            TotalEnergy = headToken.Energy;
            LastModifiedTick = 0;
            LastModifiedAt = 0;
            CreatedAt = 0;

            Tokens = new List<Token> { headToken };
            headToken.ChainHead = headToken;
            headToken.ChainPosition = 0;
        }

        /// <summary>
        /// Adds a token to the chain (at head or tail) with validation
        /// Returns true if successful
        /// </summary>
        public bool AddToken(Token token, bool atTail = true, long currentTick = 0)
        {
            if (token == null || !token.IsActive)
                return false;

            // Prevent duplicate additions
            if (Tokens.Contains(token))
                return false;

            // Validate bonding compatibility if adding to existing chain
            if (Length > 0)
            {
                Token adjacentToken = atTail ? Tail : Head;

                // Check if tokens are actually bonded
                if (!adjacentToken.BondedTokens.Contains(token) &&
                    !token.BondedTokens.Contains(adjacentToken))
                {
                    return false; // Not bonded, cannot add
                }
            }

            // Add token to appropriate end
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
            LastModifiedTick = currentTick;
            LastModifiedAt = currentTick;

            // Update all tokens' chain references
            UpdateChainReferences();

            return true;
        }

        /// <summary>
        /// Removes a token from the chain with automatic chain splitting
        /// Returns list of resulting chains (may split into 0, 1, or 2 chains)
        /// </summary>
        public List<TokenChain> RemoveToken(Token token, long currentTick = 0)
        {
            var resultChains = new List<TokenChain>();

            if (!Tokens.Contains(token))
                return resultChains;

            int tokenIndex = Tokens.IndexOf(token);

            // Update totals
            Length--;
            TotalMass -= token.Mass;
            TotalEnergy -= token.Energy;

            // Clear token's chain references
            token.ChainHead = null;
            token.ChainPosition = -1;

            // Remove the token
            Tokens.Remove(token);

            // Handle chain splitting based on position
            if (Tokens.Count == 0)
            {
                // Chain is now empty, return empty list
                return resultChains;
            }
            else if (tokenIndex == 0)
            {
                // Removed from head, remaining tokens form one chain
                Head = Tokens[0];
                Tail = Tokens[^1];
                UpdateChainReferences();
                LastModifiedTick = currentTick;
                LastModifiedAt = currentTick;
                resultChains.Add(this);
            }
            else if (tokenIndex == Tokens.Count) // Was at tail
            {
                // Removed from tail, remaining tokens form one chain
                Head = Tokens[0];
                Tail = Tokens[^1];
                UpdateChainReferences();
                LastModifiedTick = currentTick;
                LastModifiedAt = currentTick;
                resultChains.Add(this);
            }
            else
            {
                // Removed from middle - may need to split
                var leftTokens = Tokens.Take(tokenIndex).ToList();
                var rightTokens = Tokens.Skip(tokenIndex).ToList();

                // Check if left and right segments are still connected
                // (they might be if the chain has branching bonds)
                bool stillConnected = false;
                if (leftTokens.Count > 0 && rightTokens.Count > 0)
                {
                    foreach (var leftToken in leftTokens)
                    {
                        foreach (var rightToken in rightTokens)
                        {
                            if (leftToken.BondedTokens.Contains(rightToken))
                            {
                                stillConnected = true;
                                break;
                            }
                        }
                        if (stillConnected) break;
                    }
                }

                if (stillConnected)
                {
                    // Chain remains connected, keep as one
                    Head = Tokens[0];
                    Tail = Tokens[^1];
                    UpdateChainReferences();
                    LastModifiedTick = currentTick;
                    LastModifiedAt = currentTick;
                    resultChains.Add(this);
                }
                else
                {
                    // Split into two chains
                    if (leftTokens.Count > 0)
                    {
                        var leftChain = new TokenChain(leftTokens[0])
                        {
                            CreatedAt = this.CreatedAt,
                            LastModifiedTick = currentTick,
                            LastModifiedAt = currentTick,
                            BondType = this.BondType
                        };

                        for (int i = 1; i < leftTokens.Count; i++)
                        {
                            leftChain.AddToken(leftTokens[i], atTail: true, currentTick);
                        }

                        resultChains.Add(leftChain);
                    }

                    if (rightTokens.Count > 0)
                    {
                        var rightChain = new TokenChain(rightTokens[0])
                        {
                            CreatedAt = this.CreatedAt,
                            LastModifiedTick = currentTick,
                            LastModifiedAt = currentTick,
                            BondType = this.BondType
                        };

                        for (int i = 1; i < rightTokens.Count; i++)
                        {
                            rightChain.AddToken(rightTokens[i], atTail: true, currentTick);
                        }

                        resultChains.Add(rightChain);
                    }
                }
            }

            return resultChains;
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
            // Guard against invalid chains
            if (Length == 0 || Tokens == null || Tokens.Count == 0)
            {
                StabilityScore = 0.0f;
                return;
            }

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
            float energyRatio = Length > 0 ? TotalEnergy / (Length * 10.0f) : 0.0f;
            stability *= Math.Min(energyRatio, 1.0f);

            StabilityScore = Math.Clamp(stability, 0.0f, 1.0f);
        }

        /// <summary>
        /// Validates the chain for syntactic and grammatical correctness
        /// Returns validation result with detailed error information
        /// </summary>
        public ChainValidationResult ValidateChain()
        {
            var result = new ChainValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            };

            // Empty chain is invalid
            if (Length == 0 || Tokens.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("Chain is empty");
                return result;
            }

            // Single token is valid
            if (Length == 1)
            {
                IsValid = true;
                return result;
            }

            // Check token connectivity
            for (int i = 0; i < Tokens.Count - 1; i++)
            {
                var current = Tokens[i];
                var next = Tokens[i + 1];

                // Verify tokens are actually bonded
                if (!current.BondedTokens.Contains(next) && !next.BondedTokens.Contains(current))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Tokens at positions {i} and {i + 1} are not bonded");
                }

                // Check for damaged tokens
                if (current.DamageLevel >= 0.8f)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Token at position {i} is critically damaged ({current.DamageLevel:F2})");
                }
            }

            // Check for balanced structures (basic syntax check)
            if (!CheckBalancedStructures())
            {
                result.IsValid = false;
                result.Errors.Add("Unbalanced parentheses, brackets, or braces");
            }

            // Check for valid expression structure (operands and operators)
            if (!CheckExpressionStructure())
            {
                result.IsValid = false;
                result.Errors.Add("Invalid expression structure (operator/operand mismatch)");
            }

            IsValid = result.IsValid;
            return result;
        }

        /// <summary>
        /// Performs complete validation including syntax and semantics
        /// Returns comprehensive validation result
        /// </summary>
        public ComprehensiveValidationResult ValidateComplete()
        {
            var result = new ComprehensiveValidationResult();

            // Perform syntactic validation
            var syntaxResult = ValidateChain();
            result.SyntaxResult = syntaxResult;
            result.SyntaxValid = syntaxResult.IsValid;

            // Perform semantic validation (requires Grammar namespace)
            // This will be called by external validators
            result.SemanticValid = true; // Placeholder
            result.IsFullyValid = result.SyntaxValid && result.SemanticValid;

            return result;
        }

        /// <summary>
        /// Checks for balanced parentheses, brackets, and braces
        /// </summary>
        private bool CheckBalancedStructures()
        {
            var stack = new Stack<char>();
            var openChars = new HashSet<string> { "(", "[", "{" };
            var closeChars = new Dictionary<string, string>
            {
                { ")", "(" },
                { "]", "[" },
                { "}", "{" }
            };

            foreach (var token in Tokens)
            {
                if (openChars.Contains(token.Value))
                {
                    stack.Push(token.Value[0]);
                }
                else if (closeChars.ContainsKey(token.Value))
                {
                    if (stack.Count == 0)
                        return false;

                    var expected = closeChars[token.Value];
                    var actual = stack.Pop().ToString();

                    if (actual != expected)
                        return false;
                }
            }

            return stack.Count == 0;
        }

        /// <summary>
        /// Checks for valid expression structure (operators between operands)
        /// </summary>
        private bool CheckExpressionStructure()
        {
            // Simple check: operators should be between operands
            bool expectOperand = true;

            foreach (var token in Tokens)
            {
                bool isOperand = token.Type == TokenType.INTEGER_LITERAL ||
                                token.Type == TokenType.FLOAT_LITERAL ||
                                token.Type == TokenType.IDENTIFIER;

                bool isOperator = token.Type == TokenType.OPERATOR_PLUS ||
                                 token.Type == TokenType.OPERATOR_MINUS ||
                                 token.Type == TokenType.OPERATOR_MULTIPLY ||
                                 token.Type == TokenType.OPERATOR_DIVIDE;

                if (expectOperand && !isOperand)
                {
                    // Allow parentheses and keywords
                    if (token.Type != TokenType.LPAREN &&
                        token.Type != TokenType.KEYWORD_IF &&
                        token.Type != TokenType.KEYWORD_WHILE)
                    {
                        return false;
                    }
                }
                else if (!expectOperand && !isOperator)
                {
                    // Allow closing parentheses and semicolons
                    if (token.Type != TokenType.RPAREN &&
                        token.Type != TokenType.SEMICOLON)
                    {
                        return false;
                    }
                }

                if (isOperand)
                    expectOperand = false;
                if (isOperator)
                    expectOperand = true;
            }

            return true;
        }

        /// <summary>
        /// Builds a simple Abstract Syntax Tree representation
        /// Returns root node of the AST
        /// </summary>
        public ASTNode BuildAST()
        {
            if (Length == 0 || Tokens.Count == 0)
                return null;

            // For simple expressions, build basic AST
            if (Length == 1)
            {
                return new ASTNode
                {
                    NodeType = ASTNodeType.LITERAL,
                    Token = Tokens[0],
                    Value = Tokens[0].Value
                };
            }

            // Build expression tree for simple binary operations
            return BuildExpressionTree(0, Tokens.Count - 1);
        }

        /// <summary>
        /// Recursively builds expression tree
        /// </summary>
        private ASTNode BuildExpressionTree(int start, int end)
        {
            if (start > end)
                return null;

            if (start == end)
            {
                return new ASTNode
                {
                    NodeType = ASTNodeType.LITERAL,
                    Token = Tokens[start],
                    Value = Tokens[start].Value
                };
            }

            // Find operator with lowest precedence (rightmost)
            int operatorIndex = FindLowestPrecedenceOperator(start, end);

            if (operatorIndex == -1)
            {
                // No operator found, might be a single value with parentheses
                return BuildExpressionTree(start, end);
            }

            var operatorToken = Tokens[operatorIndex];
            var node = new ASTNode
            {
                NodeType = ASTNodeType.BINARY_OPERATION,
                Token = operatorToken,
                Value = operatorToken.Value
            };

            // Build left and right subtrees
            node.Left = BuildExpressionTree(start, operatorIndex - 1);
            node.Right = BuildExpressionTree(operatorIndex + 1, end);

            return node;
        }

        /// <summary>
        /// Finds operator with lowest precedence for AST construction
        /// </summary>
        private int FindLowestPrecedenceOperator(int start, int end)
        {
            int lowestPrecedence = int.MaxValue;
            int lowestIndex = -1;

            for (int i = start; i <= end; i++)
            {
                var token = Tokens[i];
                int precedence = GetOperatorPrecedence(token);

                if (precedence > 0 && precedence <= lowestPrecedence)
                {
                    lowestPrecedence = precedence;
                    lowestIndex = i;
                }
            }

            return lowestIndex;
        }

        /// <summary>
        /// Gets operator precedence (lower number = lower precedence)
        /// </summary>
        private int GetOperatorPrecedence(Token token)
        {
            return token.Type switch
            {
                TokenType.OPERATOR_PLUS => 1,
                TokenType.OPERATOR_MINUS => 1,
                TokenType.OPERATOR_MULTIPLY => 2,
                TokenType.OPERATOR_DIVIDE => 2,
                TokenType.OPERATOR_MODULO => 2,
                _ => 0 // Not an operator
            };
        }

        /// <summary>
        /// Gets the code string represented by this chain
        /// </summary>
        public string ToCodeString()
        {
            return string.Join(" ", Tokens.Select(t => t.Value));
        }

        /// <summary>
        /// Gets detailed code representation with type information
        /// </summary>
        public string GetDetailedRepresentation()
        {
            var parts = Tokens.Select(t => $"{t.Value}:{t.Type}");
            return string.Join(" ", parts);
        }

        public override string ToString()
        {
            return $"Chain(ID:{Id}, Length:{Length}, Stability:{StabilityScore:F2}, Valid:{IsValid}, Code:\"{ToCodeString()}\")";
        }
    }

    /// <summary>
    /// Result of chain validation
    /// </summary>
    public class ChainValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }

        public override string ToString()
        {
            if (IsValid)
                return "Valid";

            return $"Invalid: {string.Join("; ", Errors)}";
        }
    }

    /// <summary>
    /// Simple Abstract Syntax Tree Node
    /// </summary>
    public class ASTNode
    {
        public ASTNodeType NodeType { get; set; }
        public Token Token { get; set; }
        public string Value { get; set; }
        public ASTNode Left { get; set; }
        public ASTNode Right { get; set; }
        public List<ASTNode> Children { get; set; }

        public ASTNode()
        {
            Children = new List<ASTNode>();
        }

        public override string ToString()
        {
            if (NodeType == ASTNodeType.LITERAL)
                return Value;

            if (NodeType == ASTNodeType.BINARY_OPERATION)
                return $"({Left} {Value} {Right})";

            return Value;
        }
    }

    /// <summary>
    /// AST Node types
    /// </summary>
    public enum ASTNodeType
    {
        LITERAL,
        IDENTIFIER,
        BINARY_OPERATION,
        UNARY_OPERATION,
        FUNCTION_CALL,
        BLOCK,
        IF_STATEMENT,
        WHILE_LOOP,
        FOR_LOOP
    }

    /// <summary>
    /// Comprehensive validation result combining syntax and semantics
    /// </summary>
    public class ComprehensiveValidationResult
    {
        public bool SyntaxValid { get; set; }
        public bool SemanticValid { get; set; }
        public bool IsFullyValid { get; set; }
        public ChainValidationResult SyntaxResult { get; set; }
        public object SemanticResult { get; set; } // Will be SemanticValidationResult when available

        public override string ToString()
        {
            if (IsFullyValid)
                return "Fully Valid (Syntax + Semantics)";

            var issues = new List<string>();
            if (!SyntaxValid)
                issues.Add("Syntax");
            if (!SemanticValid)
                issues.Add("Semantics");

            return $"Invalid: {string.Join(" + ", issues)} issues";
        }
    }
}

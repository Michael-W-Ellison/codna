using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// Performs semantic validation on token chains and ASTs.
    /// Checks type compatibility, variable usage, and semantic correctness.
    /// </summary>
    public class SemanticValidator
    {
        private readonly Dictionary<string, TokenType> _variableTypes;
        private readonly HashSet<string> _declaredVariables;

        public SemanticValidator()
        {
            _variableTypes = new Dictionary<string, TokenType>();
            _declaredVariables = new HashSet<string>();
        }

        /// <summary>
        /// Performs comprehensive semantic validation on a token chain
        /// </summary>
        public SemanticValidationResult ValidateChain(TokenChain chain)
        {
            var result = new SemanticValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            if (chain == null || chain.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("Cannot validate null or empty chain");
                return result;
            }

            // Clear state for new validation
            _variableTypes.Clear();
            _declaredVariables.Clear();

            // Validate type compatibility in expressions
            ValidateTypeCompatibility(chain, result);

            // Validate variable declarations and usage
            ValidateVariableUsage(chain, result);

            // Validate operator usage
            ValidateOperators(chain, result);

            // Validate control flow structures
            ValidateControlFlow(chain, result);

            return result;
        }

        /// <summary>
        /// Validates type compatibility in the chain
        /// </summary>
        private void ValidateTypeCompatibility(TokenChain chain, SemanticValidationResult result)
        {
            for (int i = 0; i < chain.Tokens.Count; i++)
            {
                var token = chain.Tokens[i];

                // Check binary operations
                if (IsBinaryOperator(token))
                {
                    if (!ValidateBinaryOperation(chain.Tokens, i, result))
                    {
                        result.IsValid = false;
                    }
                }

                // Check type literals are used correctly
                if (IsTypeLiteral(token))
                {
                    if (!ValidateTypeLiteralUsage(chain.Tokens, i, result))
                    {
                        result.IsValid = false;
                    }
                }
            }
        }

        /// <summary>
        /// Validates a binary operation for type compatibility
        /// </summary>
        private bool ValidateBinaryOperation(List<Token> tokens, int operatorIndex, SemanticValidationResult result)
        {
            if (operatorIndex == 0 || operatorIndex >= tokens.Count - 1)
            {
                result.Errors.Add($"Operator '{tokens[operatorIndex].Value}' at position {operatorIndex} has missing operands");
                return false;
            }

            var leftToken = tokens[operatorIndex - 1];
            var operatorToken = tokens[operatorIndex];
            var rightToken = tokens[operatorIndex + 1];

            var leftType = InferTokenType(leftToken);
            var rightType = InferTokenType(rightToken);

            // Check if types are compatible
            if (!AreTypesCompatible(leftType, rightType, operatorToken))
            {
                result.Errors.Add($"Type mismatch: Cannot apply '{operatorToken.Value}' to {leftType} and {rightType} at position {operatorIndex}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates variable declarations and usage
        /// </summary>
        private void ValidateVariableUsage(TokenChain chain, SemanticValidationResult result)
        {
            for (int i = 0; i < chain.Tokens.Count; i++)
            {
                var token = chain.Tokens[i];

                // Check for variable declarations (e.g., "int x = 5")
                if (token.Type == TokenType.KEYWORD_INT ||
                    token.Type == TokenType.KEYWORD_FLOAT ||
                    token.Type == TokenType.KEYWORD_BOOL)
                {
                    if (i + 1 < chain.Tokens.Count)
                    {
                        var varName = chain.Tokens[i + 1];
                        if (varName.Type == TokenType.IDENTIFIER)
                        {
                            // Register variable with its type
                            _variableTypes[varName.Value] = token.Type;
                            _declaredVariables.Add(varName.Value);
                        }
                    }
                }

                // Check for variable usage
                if (token.Type == TokenType.IDENTIFIER)
                {
                    // Skip if this is part of a declaration
                    if (i > 0 && IsTypeKeyword(chain.Tokens[i - 1]))
                        continue;

                    // Check if variable was declared
                    if (!_declaredVariables.Contains(token.Value))
                    {
                        result.Warnings.Add($"Variable '{token.Value}' used at position {i} may not be declared");
                    }
                }
            }
        }

        /// <summary>
        /// Validates operator usage and placement
        /// </summary>
        private void ValidateOperators(TokenChain chain, SemanticValidationResult result)
        {
            for (int i = 0; i < chain.Tokens.Count; i++)
            {
                var token = chain.Tokens[i];

                if (IsBinaryOperator(token))
                {
                    // Check for consecutive operators
                    if (i > 0 && IsBinaryOperator(chain.Tokens[i - 1]))
                    {
                        result.Errors.Add($"Consecutive operators at positions {i - 1} and {i}");
                        result.IsValid = false;
                    }

                    // Check for operator at start or end
                    if (i == 0 || i == chain.Tokens.Count - 1)
                    {
                        result.Errors.Add($"Operator '{token.Value}' at position {i} missing operands");
                        result.IsValid = false;
                    }
                }
            }
        }

        /// <summary>
        /// Validates control flow structures (if, while, for)
        /// </summary>
        private void ValidateControlFlow(TokenChain chain, SemanticValidationResult result)
        {
            for (int i = 0; i < chain.Tokens.Count; i++)
            {
                var token = chain.Tokens[i];

                if (token.Type == TokenType.KEYWORD_IF || token.Type == TokenType.KEYWORD_WHILE)
                {
                    // Check for condition (should have parentheses)
                    if (i + 1 >= chain.Tokens.Count || chain.Tokens[i + 1].Type != TokenType.LPAREN)
                    {
                        result.Errors.Add($"'{token.Value}' at position {i} missing condition in parentheses");
                        result.IsValid = false;
                    }
                }
            }
        }

        /// <summary>
        /// Validates type literal usage
        /// </summary>
        private bool ValidateTypeLiteralUsage(List<Token> tokens, int index, SemanticValidationResult result)
        {
            var token = tokens[index];

            // Type keywords should be followed by identifiers or operators
            if (IsTypeKeyword(token))
            {
                if (index + 1 >= tokens.Count)
                {
                    result.Errors.Add($"Type keyword '{token.Value}' at position {index} not followed by identifier");
                    return false;
                }

                var nextToken = tokens[index + 1];
                if (nextToken.Type != TokenType.IDENTIFIER && nextToken.Type != TokenType.LPAREN)
                {
                    result.Warnings.Add($"Type keyword '{token.Value}' at position {index} has unusual usage");
                }
            }

            return true;
        }

        /// <summary>
        /// Infers the type of a token
        /// </summary>
        private string InferTokenType(Token token)
        {
            if (token.Type == TokenType.INTEGER_LITERAL)
                return "int";

            if (token.Type == TokenType.FLOAT_LITERAL)
                return "float";

            if (token.Type == TokenType.STRING_LITERAL)
                return "string";

            if (token.Type == TokenType.BOOLEAN_LITERAL)
                return "bool";

            if (token.Type == TokenType.IDENTIFIER)
            {
                // Look up variable type if known
                if (_variableTypes.ContainsKey(token.Value))
                {
                    return TokenTypeToString(_variableTypes[token.Value]);
                }
                return "unknown";
            }

            return "unknown";
        }

        /// <summary>
        /// Converts TokenType to string representation
        /// </summary>
        private string TokenTypeToString(TokenType type)
        {
            return type switch
            {
                TokenType.KEYWORD_INT => "int",
                TokenType.KEYWORD_FLOAT => "float",
                TokenType.KEYWORD_BOOL => "bool",
                TokenType.KEYWORD_STRING => "string",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Checks if two types are compatible for a given operator
        /// </summary>
        private bool AreTypesCompatible(string leftType, string rightType, Token operatorToken)
        {
            // Unknown types are assumed compatible (benefit of the doubt)
            if (leftType == "unknown" || rightType == "unknown")
                return true;

            // Arithmetic operators
            if (operatorToken.Type == TokenType.OPERATOR_PLUS ||
                operatorToken.Type == TokenType.OPERATOR_MINUS ||
                operatorToken.Type == TokenType.OPERATOR_MULTIPLY ||
                operatorToken.Type == TokenType.OPERATOR_DIVIDE)
            {
                // Numeric types are compatible with each other
                if (IsNumericType(leftType) && IsNumericType(rightType))
                    return true;

                // String concatenation with + operator
                if (operatorToken.Type == TokenType.OPERATOR_PLUS &&
                    (leftType == "string" || rightType == "string"))
                    return true;

                return false;
            }

            // Comparison operators
            if (operatorToken.Type == TokenType.OPERATOR_EQUALS ||
                operatorToken.Type == TokenType.OPERATOR_NOT_EQUALS ||
                operatorToken.Type == TokenType.OPERATOR_LESS_THAN ||
                operatorToken.Type == TokenType.OPERATOR_GREATER_THAN)
            {
                // Same types can be compared
                if (leftType == rightType)
                    return true;

                // Numeric types can be compared with each other
                if (IsNumericType(leftType) && IsNumericType(rightType))
                    return true;

                return false;
            }

            // Logical operators
            if (operatorToken.Type == TokenType.OPERATOR_AND ||
                operatorToken.Type == TokenType.OPERATOR_OR)
            {
                return leftType == "bool" && rightType == "bool";
            }

            // Default: assume compatible
            return true;
        }

        /// <summary>
        /// Checks if a type is numeric
        /// </summary>
        private bool IsNumericType(string type)
        {
            return type == "int" || type == "float";
        }

        /// <summary>
        /// Checks if token is a binary operator
        /// </summary>
        private bool IsBinaryOperator(Token token)
        {
            return token.Type == TokenType.OPERATOR_PLUS ||
                   token.Type == TokenType.OPERATOR_MINUS ||
                   token.Type == TokenType.OPERATOR_MULTIPLY ||
                   token.Type == TokenType.OPERATOR_DIVIDE ||
                   token.Type == TokenType.OPERATOR_MODULO ||
                   token.Type == TokenType.OPERATOR_EQUALS ||
                   token.Type == TokenType.OPERATOR_NOT_EQUALS ||
                   token.Type == TokenType.OPERATOR_LESS_THAN ||
                   token.Type == TokenType.OPERATOR_GREATER_THAN ||
                   token.Type == TokenType.OPERATOR_AND ||
                   token.Type == TokenType.OPERATOR_OR;
        }

        /// <summary>
        /// Checks if token is a type literal
        /// </summary>
        private bool IsTypeLiteral(Token token)
        {
            return token.Type == TokenType.INTEGER_LITERAL ||
                   token.Type == TokenType.FLOAT_LITERAL ||
                   token.Type == TokenType.STRING_LITERAL ||
                   token.Type == TokenType.BOOLEAN_LITERAL;
        }

        /// <summary>
        /// Checks if token is a type keyword
        /// </summary>
        private bool IsTypeKeyword(Token token)
        {
            return token.Type == TokenType.KEYWORD_INT ||
                   token.Type == TokenType.KEYWORD_FLOAT ||
                   token.Type == TokenType.KEYWORD_BOOL ||
                   token.Type == TokenType.KEYWORD_STRING;
        }

        /// <summary>
        /// Validates an AST for semantic correctness
        /// </summary>
        public SemanticValidationResult ValidateAST(ASTNode ast)
        {
            var result = new SemanticValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            if (ast == null)
            {
                result.Errors.Add("Cannot validate null AST");
                result.IsValid = false;
                return result;
            }

            ValidateASTNode(ast, result);
            return result;
        }

        /// <summary>
        /// Recursively validates AST nodes
        /// </summary>
        private void ValidateASTNode(ASTNode node, SemanticValidationResult result)
        {
            if (node == null)
                return;

            // Validate binary operations
            if (node.NodeType == ASTNodeType.BINARY_OPERATION)
            {
                if (node.Left == null || node.Right == null)
                {
                    result.Errors.Add($"Binary operation '{node.Value}' missing operands");
                    result.IsValid = false;
                    return;
                }

                // Recursively validate children
                ValidateASTNode(node.Left, result);
                ValidateASTNode(node.Right, result);

                // Check type compatibility
                var leftType = InferASTNodeType(node.Left);
                var rightType = InferASTNodeType(node.Right);

                if (node.Token != null && !AreTypesCompatible(leftType, rightType, node.Token))
                {
                    result.Errors.Add($"Type mismatch in operation '{node.Value}': {leftType} and {rightType}");
                    result.IsValid = false;
                }
            }

            // Validate children
            foreach (var child in node.Children)
            {
                ValidateASTNode(child, result);
            }
        }

        /// <summary>
        /// Infers the type of an AST node
        /// </summary>
        private string InferASTNodeType(ASTNode node)
        {
            if (node == null || node.Token == null)
                return "unknown";

            return InferTokenType(node.Token);
        }
    }

    /// <summary>
    /// Result of semantic validation
    /// </summary>
    public class SemanticValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public SemanticValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public override string ToString()
        {
            if (IsValid && Warnings.Count == 0)
                return "Valid (no warnings)";

            if (IsValid)
                return $"Valid with {Warnings.Count} warning(s): {string.Join("; ", Warnings)}";

            return $"Invalid: {string.Join("; ", Errors)}";
        }

        /// <summary>
        /// Gets a detailed report
        /// </summary>
        public string GetDetailedReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Semantic Validation Result: {(IsValid ? "VALID" : "INVALID")}");

            if (Errors.Count > 0)
            {
                report.AppendLine($"\nErrors ({Errors.Count}):");
                for (int i = 0; i < Errors.Count; i++)
                {
                    report.AppendLine($"  {i + 1}. {Errors[i]}");
                }
            }

            if (Warnings.Count > 0)
            {
                report.AppendLine($"\nWarnings ({Warnings.Count}):");
                for (int i = 0; i < Warnings.Count; i++)
                {
                    report.AppendLine($"  {i + 1}. {Warnings[i]}");
                }
            }

            return report.ToString();
        }
    }
}

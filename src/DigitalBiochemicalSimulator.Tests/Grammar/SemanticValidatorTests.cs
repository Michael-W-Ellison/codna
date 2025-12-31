using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.DataStructures;
using System.Collections.Generic;

namespace DigitalBiochemicalSimulator.Tests.Grammar
{
    public class SemanticValidatorTests
    {
        [Fact]
        public void SemanticValidator_ValidArithmeticExpression_PassesValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "5 + 3" expression
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain = new TokenChain(token5);
            chain.AddToken(tokenPlus, atTail: true);
            chain.AddToken(token3, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.True(result.IsValid, "Valid arithmetic expression should pass");
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void SemanticValidator_TypeMismatch_FailsValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "5 + true" (invalid: int + bool)
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var tokenTrue = new Token(3, TokenType.BOOLEAN_LITERAL, "true", Vector3Int.Zero);

            var chain = new TokenChain(token5);
            chain.AddToken(tokenPlus, atTail: true);
            chain.AddToken(tokenTrue, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.False(result.IsValid, "Type mismatch should fail validation");
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Type mismatch", result.Errors[0]);
        }

        [Fact]
        public void SemanticValidator_MixedNumericTypes_PassesValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "5 + 3.14" (valid: int + float)
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var tokenPi = new Token(3, TokenType.FLOAT_LITERAL, "3.14", Vector3Int.Zero);

            var chain = new TokenChain(token5);
            chain.AddToken(tokenPlus, atTail: true);
            chain.AddToken(tokenPi, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.True(result.IsValid, "Mixed numeric types should be compatible");
        }

        [Fact]
        public void SemanticValidator_ConsecutiveOperators_FailsValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "5 + * 3" (invalid: consecutive operators)
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var tokenMult = new Token(3, TokenType.OPERATOR_MULTIPLY, "*", Vector3Int.Zero);
            var token3 = new Token(4, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain = new TokenChain(token5);
            chain.AddToken(tokenPlus, atTail: true);
            chain.AddToken(tokenMult, atTail: true);
            chain.AddToken(token3, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.False(result.IsValid, "Consecutive operators should fail");
            Assert.Contains("Consecutive operators", result.Errors[0]);
        }

        [Fact]
        public void SemanticValidator_VariableDeclaration_TracksType()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "int x = 5"
            var tokenInt = new Token(1, TokenType.KEYWORD_INT, "int", Vector3Int.Zero);
            var tokenX = new Token(2, TokenType.IDENTIFIER, "x", Vector3Int.Zero);
            var tokenEq = new Token(3, TokenType.OPERATOR_ASSIGN, "=", Vector3Int.Zero);
            var token5 = new Token(4, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);

            var chain = new TokenChain(tokenInt);
            chain.AddToken(tokenX, atTail: true);
            chain.AddToken(tokenEq, atTail: true);
            chain.AddToken(token5, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.True(result.IsValid, "Variable declaration should be valid");
        }

        [Fact]
        public void SemanticValidator_UndeclaredVariable_GeneratesWarning()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "x + 5" (x not declared)
            var tokenX = new Token(1, TokenType.IDENTIFIER, "x", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var token5 = new Token(3, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);

            var chain = new TokenChain(tokenX);
            chain.AddToken(tokenPlus, atTail: true);
            chain.AddToken(token5, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.NotEmpty(result.Warnings);
            Assert.Contains("not be declared", result.Warnings[0]);
        }

        [Fact]
        public void SemanticValidator_LogicalOperationWithBooleans_PassesValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "true && false"
            var tokenTrue = new Token(1, TokenType.BOOLEAN_LITERAL, "true", Vector3Int.Zero);
            var tokenAnd = new Token(2, TokenType.OPERATOR_AND, "&&", Vector3Int.Zero);
            var tokenFalse = new Token(3, TokenType.BOOLEAN_LITERAL, "false", Vector3Int.Zero);

            var chain = new TokenChain(tokenTrue);
            chain.AddToken(tokenAnd, atTail: true);
            chain.AddToken(tokenFalse, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.True(result.IsValid, "Boolean logical operation should be valid");
        }

        [Fact]
        public void SemanticValidator_LogicalOperationWithNonBooleans_FailsValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "5 && 3" (invalid: int && int)
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var tokenAnd = new Token(2, TokenType.OPERATOR_AND, "&&", Vector3Int.Zero);
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain = new TokenChain(token5);
            chain.AddToken(tokenAnd, atTail: true);
            chain.AddToken(token3, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.False(result.IsValid, "Logical operation with non-booleans should fail");
        }

        [Fact]
        public void SemanticValidator_ValidAST_PassesValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create AST for "5 + 3"
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var ast = new ASTNode
            {
                NodeType = ASTNodeType.BINARY_OPERATION,
                Token = tokenPlus,
                Value = "+",
                Left = new ASTNode
                {
                    NodeType = ASTNodeType.LITERAL,
                    Token = token5,
                    Value = "5"
                },
                Right = new ASTNode
                {
                    NodeType = ASTNodeType.LITERAL,
                    Token = token3,
                    Value = "3"
                }
            };

            // Act
            var result = validator.ValidateAST(ast);

            // Assert
            Assert.True(result.IsValid, "Valid AST should pass validation");
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void SemanticValidator_ASTWithMissingOperand_FailsValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create invalid AST with missing right operand
            var tokenPlus = new Token(1, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var token5 = new Token(2, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);

            var ast = new ASTNode
            {
                NodeType = ASTNodeType.BINARY_OPERATION,
                Token = tokenPlus,
                Value = "+",
                Left = new ASTNode
                {
                    NodeType = ASTNodeType.LITERAL,
                    Token = token5,
                    Value = "5"
                },
                Right = null // Missing operand
            };

            // Act
            var result = validator.ValidateAST(ast);

            // Assert
            Assert.False(result.IsValid, "AST with missing operand should fail");
            Assert.Contains("missing operands", result.Errors[0]);
        }

        [Fact]
        public void SemanticValidator_ControlFlowWithCondition_PassesValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "if ( true )"
            var tokenIf = new Token(1, TokenType.KEYWORD_IF, "if", Vector3Int.Zero);
            var tokenLParen = new Token(2, TokenType.LPAREN, "(", Vector3Int.Zero);
            var tokenTrue = new Token(3, TokenType.BOOLEAN_LITERAL, "true", Vector3Int.Zero);
            var tokenRParen = new Token(4, TokenType.RPAREN, ")", Vector3Int.Zero);

            var chain = new TokenChain(tokenIf);
            chain.AddToken(tokenLParen, atTail: true);
            chain.AddToken(tokenTrue, atTail: true);
            chain.AddToken(tokenRParen, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.True(result.IsValid, "Control flow with condition should be valid");
        }

        [Fact]
        public void SemanticValidator_ControlFlowWithoutCondition_FailsValidation()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create "if true" (missing parentheses)
            var tokenIf = new Token(1, TokenType.KEYWORD_IF, "if", Vector3Int.Zero);
            var tokenTrue = new Token(2, TokenType.BOOLEAN_LITERAL, "true", Vector3Int.Zero);

            var chain = new TokenChain(tokenIf);
            chain.AddToken(tokenTrue, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);

            // Assert
            Assert.False(result.IsValid, "Control flow without parentheses should fail");
            Assert.Contains("missing condition", result.Errors[0]);
        }

        [Fact]
        public void SemanticValidator_DetailedReport_ContainsErrorsAndWarnings()
        {
            // Arrange
            var validator = new SemanticValidator();

            // Create expression with both errors and warnings
            var tokenX = new Token(1, TokenType.IDENTIFIER, "x", Vector3Int.Zero);
            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            var chain = new TokenChain(tokenX);
            chain.AddToken(tokenPlus, atTail: true);

            // Act
            var result = validator.ValidateChain(chain);
            var report = result.GetDetailedReport();

            // Assert
            Assert.NotEmpty(report);
            Assert.Contains("Errors", report);
        }
    }
}

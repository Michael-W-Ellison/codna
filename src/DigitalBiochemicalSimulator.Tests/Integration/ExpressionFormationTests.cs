using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Tests.Integration
{
    /// <summary>
    /// Tests that verify the system can form valid code expressions like "5 + 3"
    /// </summary>
    public class ExpressionFormationTests
    {
        [Fact]
        public void SimpleExpression_5Plus3_CanFormValidBonds()
        {
            // Arrange: Create the tokens for "5 + 3"
            var config = SimulationConfig.CreateMinimal();
            var grid = new Grid(10, 10, 10, 3);
            var grammarRules = GrammarLibrary.GetArithmeticGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);
            var bondingManager = new BondingManager(rulesEngine, strengthCalculator, config, grid);

            // Create tokens at same position so they can bond
            var position = new Vector3Int(5, 5, 5);

            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", position)
            {
                Energy = 100,
                IsActive = true,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", position)
            {
                Energy = 100,
                IsActive = true,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", position)
            {
                Energy = 100,
                IsActive = true,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            grid.AddToken(token5);
            grid.AddToken(tokenPlus);
            grid.AddToken(token3);

            // Act: Attempt to form bonds
            long currentTick = 1;
            bool bond1 = bondingManager.AttemptBond(token5, tokenPlus, currentTick);
            bool bond2 = bondingManager.AttemptBond(tokenPlus, token3, currentTick);

            // Assert: Bonds should form to create "5 + 3"
            Assert.True(bond1, "Should bond '5' and '+'");
            Assert.True(bond2, "Should bond '+' and '3'");

            // Verify the chain structure
            Assert.Contains(tokenPlus, token5.BondedTokens);
            Assert.Contains(token5, tokenPlus.BondedTokens);
            Assert.Contains(token3, tokenPlus.BondedTokens);
            Assert.Contains(tokenPlus, token3.BondedTokens);
        }

        [Fact]
        public void SimpleExpression_ValidatesAsCorrectGrammar()
        {
            // Arrange
            var grammarRules = GrammarLibrary.GetArithmeticGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);

            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero)
            {
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero)
            {
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var tokens = new System.Collections.Generic.List<Token> { token5, tokenPlus, token3 };

            // Act
            bool isValid = rulesEngine.MatchesGrammar(tokens);

            // Assert
            Assert.True(isValid, "The sequence '5 + 3' should match arithmetic grammar");
        }

        [Fact]
        public void ChainStability_IncreasesWith ValidGrammar()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            var grid = new Grid(10, 10, 10, 3);
            var grammarRules = GrammarLibrary.GetArithmeticGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);
            var stabilityCalculator = new ChainStabilityCalculator(rulesEngine, strengthCalculator);

            // Create a valid chain: "5 + 3"
            var token5 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero)
            {
                Energy = 100,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var tokenPlus = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Energy = 100,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero)
            {
                Energy = 100,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            // Create chain
            var chain = new TokenChain(token5);
            chain.AddToken(tokenPlus);
            chain.AddToken(token3);
            chain.IsValid = rulesEngine.MatchesGrammar(chain.Tokens);
            chain.AverageBondStrength = 0.7f;
            chain.LastModifiedTick = 0;

            // Act
            long currentTick = 50; // Some time has passed
            float stability = stabilityCalculator.CalculateStability(chain, currentTick);

            // Assert
            Assert.True(stability > 0.5f, "Valid grammar chain should have decent stability");
        }

        [Fact]
        public void InvalidExpression_HasLowerStability()
        {
            // Arrange
            var grammarRules = GrammarLibrary.GetArithmeticGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);
            var stabilityCalculator = new ChainStabilityCalculator(rulesEngine, strengthCalculator);

            // Create an invalid chain: "+ 5 +" (nonsensical)
            var tokenPlus1 = new Token(1, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Energy = 100,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            var token5 = new Token(2, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero)
            {
                Energy = 100,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var tokenPlus2 = new Token(3, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Energy = 100,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            var chain = new TokenChain(tokenPlus1);
            chain.AddToken(token5);
            chain.AddToken(tokenPlus2);
            chain.IsValid = rulesEngine.MatchesGrammar(chain.Tokens);
            chain.AverageBondStrength = 0.5f;
            chain.LastModifiedTick = 0;

            // Act
            long currentTick = 50;
            float stability = stabilityCalculator.CalculateStability(chain, currentTick);

            // Assert
            Assert.True(stability < 0.5f, "Invalid grammar should have lower stability");
        }
    }
}

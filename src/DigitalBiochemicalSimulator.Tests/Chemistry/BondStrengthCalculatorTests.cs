using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Tests.Chemistry
{
    public class BondStrengthCalculatorTests
    {
        [Fact]
        public void BondStrengthCalculator_CalculateBondStrength_ReturnsValidValue()
        {
            // Arrange
            var grammarRules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var calculator = new BondStrengthCalculator(rulesEngine);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero)
            {
                Energy = 50,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Energy = 50,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            // Act
            float strength = calculator.CalculateBondStrength(token1, token2);

            // Assert
            Assert.InRange(strength, 0.0f, 1.0f);
        }

        [Fact]
        public void BondStrengthCalculator_CanFormBond_ReturnsTrueForCompatibleTokens()
        {
            // Arrange
            var grammarRules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var calculator = new BondStrengthCalculator(rulesEngine);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero)
            {
                Energy = 50,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Energy = 50,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            // Act
            bool canForm = calculator.CanFormBond(token1, token2);

            // Assert
            // Should be true if bond strength >= 0.3 threshold
            Assert.True(canForm);
        }

        [Fact]
        public void BondStrengthCalculator_DetermineBondType_ReturnsValidBondType()
        {
            // Arrange
            var grammarRules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var calculator = new BondStrengthCalculator(rulesEngine);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            // Act
            BondType bondType = calculator.DetermineBondType(token1, token2);

            // Assert
            Assert.True(bondType == BondType.COVALENT ||
                       bondType == BondType.IONIC ||
                       bondType == BondType.VAN_DER_WAALS);
        }

        [Fact]
        public void BondStrengthCalculator_CalculateBondEnergyCost_ReturnsPositiveValue()
        {
            // Arrange
            var grammarRules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var calculator = new BondStrengthCalculator(rulesEngine);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero)
            {
                Energy = 50,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero)
            {
                Energy = 50,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            // Act
            int cost = calculator.CalculateBondEnergyCost(token1, token2);

            // Assert
            Assert.InRange(cost, 5, 20); // Based on MIN_COST and MAX_COST in implementation
        }
    }
}

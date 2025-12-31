using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Tests.Core
{
    public class TokenTests
    {
        [Fact]
        public void Token_Creation_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 10, 3));

            // Assert
            Assert.Equal(1, token.Id);
            Assert.Equal(TokenType.INTEGER_LITERAL, token.Type);
            Assert.Equal("42", token.Value);
            Assert.Equal(new Vector3Int(5, 10, 3), token.Position);
            Assert.True(token.IsActive);
            Assert.NotNull(token.BondedTokens);
            Assert.Empty(token.BondedTokens);
        }

        [Fact]
        public void Token_EnergyManagement_WorksCorrectly()
        {
            // Arrange
            var token = new Token(1, TokenType.IDENTIFIER, "x", Vector3Int.Zero);

            // Act
            token.Energy = 50;

            // Assert
            Assert.Equal(50, token.Energy);
            Assert.False(token.IsFalling);
        }

        [Fact]
        public void Token_IsFalling_WhenEnergyIsZero()
        {
            // Arrange
            var token = new Token(1, TokenType.IDENTIFIER, "x", Vector3Int.Zero);

            // Act
            token.Energy = 0;

            // Assert
            Assert.True(token.IsFalling);
        }

        [Fact]
        public void Token_DamageTracking_WorksCorrectly()
        {
            // Arrange
            var token = new Token(1, TokenType.IDENTIFIER, "x", Vector3Int.Zero);

            // Act
            token.DamageLevel = 0.5f;
            token.IsDamaged = true;

            // Assert
            Assert.Equal(0.5f, token.DamageLevel);
            Assert.True(token.IsDamaged);
        }

        [Fact]
        public void Token_BondedTokens_CanBeAdded()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            // Act
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Assert
            Assert.Single(token1.BondedTokens);
            Assert.Contains(token2, token1.BondedTokens);
            Assert.Single(token2.BondedTokens);
            Assert.Contains(token1, token2.BondedTokens);
        }

        [Fact]
        public void Token_Metadata_CanBeSet()
        {
            // Arrange
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);
            var metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2);

            // Act
            token.Metadata = metadata;

            // Assert
            Assert.NotNull(token.Metadata);
            Assert.Equal("literal", token.Metadata.SyntaxCategory);
            Assert.Equal(0.3f, token.Metadata.Electronegativity);
        }
    }
}

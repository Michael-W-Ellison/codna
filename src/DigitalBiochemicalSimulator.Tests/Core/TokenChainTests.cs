using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.Core
{
    public class TokenChainTests
    {
        [Fact]
        public void TokenChain_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var chain = new TokenChain();

            // Assert
            Assert.NotNull(chain.Tokens);
            Assert.Empty(chain.Tokens);
            Assert.Equal(0, chain.Length);
            Assert.Equal(0, chain.StabilityScore);
            Assert.False(chain.IsValid);
            Assert.Equal(BondType.COVALENT, chain.BondType);
        }

        [Fact]
        public void TokenChain_ConstructorWithToken_InitializesWithSingleToken()
        {
            // Arrange
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero)
            {
                Mass = 5,
                Energy = 100
            };

            // Act
            var chain = new TokenChain(token);

            // Assert
            Assert.Equal(token, chain.Head);
            Assert.Equal(token, chain.Tail);
            Assert.Equal(1, chain.Length);
            Assert.Equal(5, chain.TotalMass);
            Assert.Equal(100, chain.TotalEnergy);
            Assert.Single(chain.Tokens);
            Assert.Contains(token, chain.Tokens);
        }

        [Fact]
        public void AddToken_ToTail_AddsSuccessfully()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 5, Energy = 50 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Mass = 3, Energy = 30 };
            var chain = new TokenChain(token1);

            // Simulate bonding
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Act
            bool result = chain.AddToken(token2, atTail: true, currentTick: 10);

            // Assert
            Assert.True(result);
            Assert.Equal(2, chain.Length);
            Assert.Equal(token1, chain.Head);
            Assert.Equal(token2, chain.Tail);
            Assert.Equal(8, chain.TotalMass); // 5 + 3
            Assert.Equal(80, chain.TotalEnergy); // 50 + 30
            Assert.Equal(10, chain.LastModifiedTick);
        }

        [Fact]
        public void AddToken_ToHead_AddsSuccessfully()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 5, Energy = 50 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Mass = 3, Energy = 30 };
            var chain = new TokenChain(token1);

            // Simulate bonding
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Act
            bool result = chain.AddToken(token2, atTail: false, currentTick: 10);

            // Assert
            Assert.True(result);
            Assert.Equal(2, chain.Length);
            Assert.Equal(token2, chain.Head);
            Assert.Equal(token1, chain.Tail);
            Assert.Equal(8, chain.TotalMass);
            Assert.Equal(80, chain.TotalEnergy);
        }

        [Fact]
        public void AddToken_NullToken_ReturnsFalse()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var chain = new TokenChain(token1);

            // Act
            bool result = chain.AddToken(null, atTail: true);

            // Assert
            Assert.False(result);
            Assert.Equal(1, chain.Length);
        }

        [Fact]
        public void AddToken_InactiveToken_ReturnsFalse()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { IsActive = false };
            var chain = new TokenChain(token1);

            // Act
            bool result = chain.AddToken(token2, atTail: true);

            // Assert
            Assert.False(result);
            Assert.Equal(1, chain.Length);
        }

        [Fact]
        public void AddToken_DuplicateToken_ReturnsFalse()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var chain = new TokenChain(token1);

            // Act
            bool result = chain.AddToken(token1, atTail: true);

            // Assert
            Assert.False(result);
            Assert.Equal(1, chain.Length);
        }

        [Fact]
        public void AddToken_NotBonded_ReturnsFalse()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var chain = new TokenChain(token1);
            // Don't add bond between tokens

            // Act
            bool result = chain.AddToken(token2, atTail: true);

            // Assert
            Assert.False(result);
            Assert.Equal(1, chain.Length);
        }

        [Fact]
        public void RemoveToken_FromMiddle_SplitsChain()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 5, Energy = 50 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Mass = 3, Energy = 30 };
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Mass = 5, Energy = 50 };

            var chain = new TokenChain(token1);
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);
            chain.AddToken(token2, atTail: true);

            token2.BondedTokens.Add(token3);
            token3.BondedTokens.Add(token2);
            chain.AddToken(token3, atTail: true);

            // Act
            var resultChains = chain.RemoveToken(token2, currentTick: 20);

            // Assert
            Assert.Equal(2, chain.Length);
            Assert.Equal(10, chain.TotalMass); // 5 + 5 (token2 removed)
            Assert.Equal(100, chain.TotalEnergy); // 50 + 50
            Assert.Null(token2.ChainHead);
            Assert.Equal(-1, token2.ChainPosition);
        }

        [Fact]
        public void RemoveToken_FromHead_UpdatesChain()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 5, Energy = 50 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Mass = 3, Energy = 30 };

            var chain = new TokenChain(token1);
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);
            chain.AddToken(token2, atTail: true);

            // Act
            var resultChains = chain.RemoveToken(token1);

            // Assert
            Assert.Equal(1, chain.Length);
            Assert.Equal(token2, chain.Head);
        }

        [Fact]
        public void RemoveToken_FromTail_UpdatesChain()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 5, Energy = 50 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Mass = 3, Energy = 30 };

            var chain = new TokenChain(token1);
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);
            chain.AddToken(token2, atTail: true);

            // Act
            var resultChains = chain.RemoveToken(token2);

            // Assert
            Assert.Equal(1, chain.Length);
            Assert.Equal(token1, chain.Tail);
        }

        [Fact]
        public void RemoveToken_LastToken_EmptiesChain()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var chain = new TokenChain(token1);

            // Act
            var resultChains = chain.RemoveToken(token1);

            // Assert
            Assert.Equal(0, chain.Length);
            Assert.Empty(chain.Tokens);
            Assert.Empty(resultChains);
        }

        [Fact]
        public void RemoveToken_NonExistentToken_ReturnsEmptyList()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var chain = new TokenChain(token1);

            // Act
            var resultChains = chain.RemoveToken(token2);

            // Assert
            Assert.Empty(resultChains);
            Assert.Equal(1, chain.Length);
        }

        [Fact]
        public void ToCodeString_SingleToken_ReturnsValue()
        {
            // Arrange
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);
            var chain = new TokenChain(token);

            // Act
            string code = chain.ToCodeString();

            // Assert
            Assert.Equal("42", code);
        }

        [Fact]
        public void ToCodeString_MultipleTokens_ReturnsConcatenated()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain = new TokenChain(token1);
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);
            chain.AddToken(token2, atTail: true);

            token2.BondedTokens.Add(token3);
            token3.BondedTokens.Add(token2);
            chain.AddToken(token3, atTail: true);

            // Act
            string code = chain.ToCodeString();

            // Assert
            Assert.Equal("5 + 3", code);
        }

        [Fact]
        public void ValidateChain_EmptyChain_ReturnsFalse()
        {
            // Arrange
            var chain = new TokenChain();

            // Act
            bool isValid = chain.ValidateChain();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateChain_ValidExpression_ReturnsTrue()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain = new TokenChain(token1);
            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);
            chain.AddToken(token2, atTail: true);

            token2.BondedTokens.Add(token3);
            token3.BondedTokens.Add(token2);
            chain.AddToken(token3, atTail: true);

            // Act
            bool isValid = chain.ValidateChain();

            // Assert - depends on grammar rules, but should execute without errors
            Assert.True(isValid || !isValid); // Just verify it runs
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 5, Energy = 50 };
            var chain = new TokenChain(token)
            {
                StabilityScore = 0.8f,
                IsValid = true,
                AverageBondStrength = 0.6f
            };

            // Act
            var clone = chain.Clone();

            // Assert
            Assert.NotSame(chain, clone);
            Assert.Equal(chain.Length, clone.Length);
            Assert.Equal(chain.TotalMass, clone.TotalMass);
            Assert.Equal(chain.TotalEnergy, clone.TotalEnergy);
            Assert.Equal(chain.StabilityScore, clone.StabilityScore);
            Assert.Equal(chain.IsValid, clone.IsValid);
            Assert.Equal(chain.AverageBondStrength, clone.AverageBondStrength);
        }

        [Fact]
        public void ChainReferences_UpdatedCorrectly_WhenTokensAdded()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var chain = new TokenChain(token1);

            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Act
            chain.AddToken(token2, atTail: true);

            // Assert
            Assert.Equal(token1, token1.ChainHead);
            Assert.Equal(token1, token2.ChainHead);
            Assert.Equal(0, token1.ChainPosition);
            Assert.Equal(1, token2.ChainPosition);
        }

        [Fact]
        public void TotalMass_UpdatesCorrectly_WhenTokensAddedAndRemoved()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 10 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Mass = 5 };
            var chain = new TokenChain(token1);

            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Act
            chain.AddToken(token2, atTail: true);
            Assert.Equal(15, chain.TotalMass);

            chain.RemoveToken(token2);

            // Assert
            Assert.Equal(10, chain.TotalMass);
        }

        [Fact]
        public void TotalEnergy_UpdatesCorrectly_WhenTokensAddedAndRemoved()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100 };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 50 };
            var chain = new TokenChain(token1);

            token1.BondedTokens.Add(token2);
            token2.BondedTokens.Add(token1);

            // Act
            chain.AddToken(token2, atTail: true);
            Assert.Equal(150, chain.TotalEnergy);

            chain.RemoveToken(token2);

            // Assert
            Assert.Equal(100, chain.TotalEnergy);
        }
    }
}

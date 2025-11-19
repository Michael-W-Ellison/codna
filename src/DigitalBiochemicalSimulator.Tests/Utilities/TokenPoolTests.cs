using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Utilities;
using System.Collections.Generic;

namespace DigitalBiochemicalSimulator.Tests.Utilities
{
    public class TokenPoolTests
    {
        [Fact]
        public void TokenPool_Constructor_PreAllocatesTokens()
        {
            // Arrange & Act
            var pool = new TokenPool(initialSize: 50, maxPoolSize: 100);

            // Assert
            Assert.Equal(50, pool.AvailableCount);
            Assert.Equal(0, pool.ActiveCount);
            Assert.Equal(50, pool.TotalCreated);
        }

        [Fact]
        public void TokenPool_DefaultConstructor_Uses100InitialSize()
        {
            // Arrange & Act
            var pool = new TokenPool();

            // Assert
            Assert.Equal(100, pool.AvailableCount);
            Assert.Equal(100, pool.TotalCreated);
        }

        [Fact]
        public void GetToken_FromPreAllocatedPool_ReusesToken()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 10, maxPoolSize: 20);
            int initialCreated = pool.TotalCreated;

            // Act
            var token = pool.GetToken(TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 10, 3));

            // Assert
            Assert.Equal(initialCreated, pool.TotalCreated); // No new token created
            Assert.Equal(9, pool.AvailableCount); // One less available
            Assert.Equal(1, pool.ActiveCount); // One active
            Assert.Equal(TokenType.INTEGER_LITERAL, token.Type);
            Assert.Equal("42", token.Value);
            Assert.Equal(new Vector3Int(5, 10, 3), token.Position);
            Assert.True(token.IsActive);
        }

        [Fact]
        public void GetToken_WhenPoolEmpty_CreatesNewToken()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 0, maxPoolSize: 10);

            // Act
            var token = pool.GetToken(TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);

            // Assert
            Assert.Equal(1, pool.TotalCreated);
            Assert.Equal(1, pool.ActiveCount);
            Assert.Equal(0, pool.AvailableCount);
            Assert.NotNull(token);
        }

        [Fact]
        public void GetToken_InitializesTokenCorrectly()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5);
            var position = new Vector3Int(10, 20, 30);

            // Act
            var token = pool.GetToken(TokenType.OPERATOR_PLUS, "+", position);

            // Assert
            Assert.Equal(TokenType.OPERATOR_PLUS, token.Type);
            Assert.Equal("+", token.Value);
            Assert.Equal(position, token.Position);
            Assert.Equal(1, token.Mass); // Length of "+"
            Assert.Equal(0, token.Energy);
            Assert.True(token.IsActive);
        }

        [Fact]
        public void ReleaseToken_ActiveToken_ReturnsToPool()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5, maxPoolSize: 10);
            var token = pool.GetToken(TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);

            // Act
            pool.ReleaseToken(token);

            // Assert
            Assert.Equal(5, pool.AvailableCount); // Back to initial size
            Assert.Equal(0, pool.ActiveCount);
            Assert.False(token.IsActive);
        }

        [Fact]
        public void ReleaseToken_NullToken_DoesNothing()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5);
            int initialAvailable = pool.AvailableCount;

            // Act
            pool.ReleaseToken(null);

            // Assert
            Assert.Equal(initialAvailable, pool.AvailableCount);
        }

        [Fact]
        public void ReleaseToken_NonActiveToken_DoesNothing()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5);
            var externalToken = new Token(1, TokenType.INTEGER_LITERAL, "99", Vector3Int.Zero);

            // Act
            pool.ReleaseToken(externalToken);

            // Assert
            Assert.Equal(5, pool.AvailableCount); // Unchanged
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        public void ReleaseToken_ExceedsMaxPoolSize_DiscardsToken()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 2, maxPoolSize: 2);
            var token1 = pool.GetToken(TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero);
            var token2 = pool.GetToken(TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero);
            var token3 = pool.GetToken(TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero); // Creates new token

            // Act
            pool.ReleaseToken(token1);
            pool.ReleaseToken(token2);
            pool.ReleaseToken(token3); // Should be discarded (pool at max)

            // Assert
            Assert.Equal(2, pool.AvailableCount); // Max pool size
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        public void ReleaseTokens_MultipleTokens_ReturnsAllToPool()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 10, maxPoolSize: 20);
            var tokens = new List<Token>
            {
                pool.GetToken(TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero),
                pool.GetToken(TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero),
                pool.GetToken(TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero)
            };

            // Act
            pool.ReleaseTokens(tokens);

            // Assert
            Assert.Equal(10, pool.AvailableCount);
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        public void TokenReuse_ClearsAllState()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5);
            var token = pool.GetToken(TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);

            // Modify token state
            token.Energy = 100;
            token.IsDamaged = true;
            token.DamageLevel = 0.5f;
            token.BondedTokens.Add(new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero));

            // Act
            pool.ReleaseToken(token);
            var reusedToken = pool.GetToken(TokenType.IDENTIFIER, "x", new Vector3Int(1, 2, 3));

            // Assert
            Assert.Same(token, reusedToken); // Same instance reused
            Assert.Equal(TokenType.IDENTIFIER, reusedToken.Type);
            Assert.Equal("x", reusedToken.Value);
            Assert.Equal(new Vector3Int(1, 2, 3), reusedToken.Position);
            Assert.Equal(0, reusedToken.Energy);
            Assert.False(reusedToken.IsDamaged);
            Assert.Equal(0.0f, reusedToken.DamageLevel);
            Assert.Empty(reusedToken.BondedTokens);
            Assert.True(reusedToken.IsActive);
        }

        [Fact]
        public void Clear_RemovesAllTokens()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 10);
            pool.GetToken(TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero);
            pool.GetToken(TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero);

            // Act
            pool.Clear();

            // Assert
            Assert.Equal(0, pool.AvailableCount);
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        public void MultipleGetAndRelease_MaintainsCorrectCounts()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5, maxPoolSize: 10);

            // Act
            var token1 = pool.GetToken(TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero);
            var token2 = pool.GetToken(TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero);
            Assert.Equal(3, pool.AvailableCount);
            Assert.Equal(2, pool.ActiveCount);

            pool.ReleaseToken(token1);
            Assert.Equal(4, pool.AvailableCount);
            Assert.Equal(1, pool.ActiveCount);

            var token3 = pool.GetToken(TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);
            Assert.Equal(3, pool.AvailableCount);
            Assert.Equal(2, pool.ActiveCount);

            pool.ReleaseToken(token2);
            pool.ReleaseToken(token3);
            Assert.Equal(5, pool.AvailableCount);
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        public void TokenPool_HandlesHighVolume()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 10, maxPoolSize: 50);
            var tokens = new List<Token>();

            // Act - Get 100 tokens
            for (int i = 0; i < 100; i++)
            {
                tokens.Add(pool.GetToken(TokenType.INTEGER_LITERAL, i.ToString(), Vector3Int.Zero));
            }

            // Assert - Pool created new tokens as needed
            Assert.Equal(100, pool.ActiveCount);
            Assert.True(pool.TotalCreated >= 100);

            // Act - Release all tokens
            pool.ReleaseTokens(tokens);

            // Assert - Pool keeps up to maxPoolSize
            Assert.Equal(50, pool.AvailableCount); // Max pool size
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        public void TokenPool_ResetToken_GeneratesNewGuid()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5);
            var token = pool.GetToken(TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);
            var firstId = token.Id;

            // Act
            pool.ReleaseToken(token);
            var reusedToken = pool.GetToken(TokenType.INTEGER_LITERAL, "99", Vector3Int.Zero);

            // Assert
            Assert.Same(token, reusedToken);
            Assert.NotEqual(firstId, reusedToken.Id); // New GUID
        }

        [Fact]
        public void TokenPool_CleansTokenBondSites()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 5);
            var token = pool.GetToken(TokenType.IDENTIFIER, "x", Vector3Int.Zero);

            // Bond the token
            var otherToken = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            token.BondedTokens.Add(otherToken);
            if (token.BondSites.Count > 0)
            {
                token.BondSites[0].BondTo(otherToken, 0);
            }

            // Act
            pool.ReleaseToken(token);

            // Assert
            Assert.Empty(token.BondedTokens);
            foreach (var site in token.BondSites)
            {
                Assert.False(site.IsBonded);
            }
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 10, maxPoolSize: 20);
            pool.GetToken(TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero);
            pool.GetToken(TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero);

            // Act
            string result = pool.ToString();

            // Assert
            Assert.Contains("Active:2", result);
            Assert.Contains("Available:8", result);
            Assert.Contains("Total Created:10", result);
        }

        [Fact]
        public void TokenPool_StressTest_NoMemoryLeaks()
        {
            // Arrange
            var pool = new TokenPool(initialSize: 10, maxPoolSize: 50);

            // Act - Rapid allocation and deallocation
            for (int i = 0; i < 1000; i++)
            {
                var token = pool.GetToken(TokenType.INTEGER_LITERAL, i.ToString(), Vector3Int.Zero);
                pool.ReleaseToken(token);
            }

            // Assert - Pool size should be stable
            Assert.Equal(50, pool.AvailableCount);
            Assert.Equal(0, pool.ActiveCount);
        }
    }
}

using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.DataStructures
{
    public class SpatialIndexTests
    {
        [Fact]
        public void SpatialIndex_Constructor_InitializesCorrectly()
        {
            // Arrange
            // Act
            var index = new SpatialIndex(100, 100, 100, maxTokensPerNode: 10);

            // Assert
            Assert.NotNull(index);
        }

        [Fact]
        public void Insert_Token_AddsToIndex()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100, maxTokensPerNode: 10);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(50, 50, 50));

            // Act
            index.AddToken(token);

            // Assert
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);
            Assert.Contains(token, nearby);
        }

        [Fact]
        public void FindTokensInRange_ReturnsTokensWithinRadius()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 50, 50));
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(60, 50, 50));

            index.AddToken(token1);
            index.AddToken(token2);
            index.AddToken(token3);

            // Act
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);

            // Assert
            Assert.Equal(2, nearby.Count);
            Assert.Contains(token1, nearby);
            Assert.Contains(token2, nearby);
            Assert.DoesNotContain(token3, nearby); // Too far
        }

        [Fact]
        public void FindTokensInRange_EmptyIndex_ReturnsEmpty()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            // Act
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 10);

            // Assert
            Assert.Empty(nearby);
        }

        [Fact]
        public void FindPotentialBondingPartners_ExcludesSelf()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 50, 50));

            index.AddToken(token1);
            index.AddToken(token2);

            // Act
            var partners = index.FindPotentialBondingPartners(token1, 5);

            // Assert
            Assert.Single(partners);
            Assert.Contains(token2, partners);
            Assert.DoesNotContain(token1, partners); // Should not include self
        }

        [Fact]
        public void FindPotentialBondingPartners_ExcludesInactiveTokens()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 50, 50)) { IsActive = false };

            index.AddToken(token1);
            index.AddToken(token2);

            // Act
            var partners = index.FindPotentialBondingPartners(token1, 5);

            // Assert
            Assert.Empty(partners); // token2 is inactive
        }

        [Fact]
        public void Remove_Token_RemovesFromIndex()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(50, 50, 50));

            index.AddToken(token);

            // Act
            index.RemoveToken(token);
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);

            // Assert
            Assert.Empty(nearby);
        }

        [Fact]
        public void Rebuild_ReconstructsIndex()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            var tokens = new[]
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(10, 10, 10)),
                new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(20, 20, 20)),
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(30, 30, 30))
            };

            foreach (var token in tokens)
            {
                index.AddToken(token);
            }

            // Modify token positions
            tokens[0].Position = new Vector3Int(50, 50, 50);

            // Act
            index.IndexTokens(tokens);

            // Assert
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);
            Assert.Contains(tokens[0], nearby); // Should find token at new position
        }

        [Fact]
        public void Clear_RemovesAllTokens()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(60, 60, 60));

            index.AddToken(token1);
            index.AddToken(token2);

            // Act
            index.Clear();

            // Assert
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 20);
            Assert.Empty(nearby);
        }

        [Fact]
        public void FindNearestNeighbors_ReturnsClosestTokens()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            var center = new Vector3Int(50, 50, 50);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(52, 50, 50)); // Distance: 2
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(55, 50, 50)); // Distance: 5
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(60, 50, 50)); // Distance: 10

            index.AddToken(token1);
            index.AddToken(token2);
            index.AddToken(token3);

            // Act
            var nearest = index.FindNearestNeighbors(center, k: 2);

            // Assert
            Assert.Equal(2, nearest.Count);
            Assert.Contains(token1, nearest);
            Assert.Contains(token2, nearest);
        }

        [Fact]
        public void AutoRebuild_TriggersAfterThresholdUpdates()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100, rebuildThreshold: 10);

            // Act - Add more tokens than threshold
            for (int i = 0; i < 15; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(i * 5, i * 5, i * 5));
                index.AddToken(token);
            }

            // Assert - Index should still work correctly after auto-rebuild
            var nearby = index.FindTokensInRange(new Vector3Int(0, 0, 0), 10);
            Assert.NotEmpty(nearby);
        }

        [Fact]
        public void FindClusters_GroupsNearbyTokens()
        {
            // Arrange
            var index = new SpatialIndex(200, 200, 200);

            // Create two clusters
            var cluster1Tokens = new[]
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(10, 10, 10)),
                new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(12, 10, 10)),
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(11, 11, 10))
            };

            var cluster2Tokens = new[]
            {
                new Token(4, TokenType.INTEGER_LITERAL, "4", new Vector3Int(100, 100, 100)),
                new Token(5, TokenType.INTEGER_LITERAL, "5", new Vector3Int(102, 100, 100))
            };

            foreach (var token in cluster1Tokens.Concat(cluster2Tokens))
            {
                index.AddToken(token);
            }

            // Act
            var clusters = index.FindClusters(cluster1Tokens.Concat(cluster2Tokens).ToList(), clusterRadius: 5);

            // Assert
            Assert.True(clusters.Count >= 2); // At least 2 clusters
        }

        [Fact]
        public void CalculateDensity_CalculatesDensity()
        {
            // Arrange
            var index = new SpatialIndex(100, 100, 100);

            // Add tokens in one area
            for (int i = 0; i < 10; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(50 + i, 50, 50));
                index.AddToken(token);
            }

            // Act
            float density = index.CalculateDensity(new Vector3Int(55, 50, 50), 10);

            // Assert
            Assert.True(density > 0);
        }
    }
}

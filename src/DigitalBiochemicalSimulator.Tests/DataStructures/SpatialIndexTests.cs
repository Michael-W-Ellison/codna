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
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));

            // Act
            var index = new SpatialIndex(bounds, capacity: 10);

            // Assert
            Assert.NotNull(index);
        }

        [Fact]
        public void Insert_Token_AddsToIndex()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds, capacity: 10);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(50, 50, 50));

            // Act
            index.Insert(token);

            // Assert
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);
            Assert.Contains(token, nearby);
        }

        [Fact]
        public void FindTokensInRange_ReturnsTokensWithinRadius()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 50, 50));
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(60, 50, 50));

            index.Insert(token1);
            index.Insert(token2);
            index.Insert(token3);

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
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            // Act
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 10);

            // Assert
            Assert.Empty(nearby);
        }

        [Fact]
        public void FindPotentialBondingPartners_ExcludesSelf()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 50, 50));

            index.Insert(token1);
            index.Insert(token2);

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
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 50, 50)) { IsActive = false };

            index.Insert(token1);
            index.Insert(token2);

            // Act
            var partners = index.FindPotentialBondingPartners(token1, 5);

            // Assert
            Assert.Empty(partners); // token2 is inactive
        }

        [Fact]
        public void Remove_Token_RemovesFromIndex()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(50, 50, 50));

            index.Insert(token);

            // Act
            index.Remove(token);
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);

            // Assert
            Assert.Empty(nearby);
        }

        [Fact]
        public void Rebuild_ReconstructsIndex()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            var tokens = new[]
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(10, 10, 10)),
                new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(20, 20, 20)),
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(30, 30, 30))
            };

            foreach (var token in tokens)
            {
                index.Insert(token);
            }

            // Modify token positions
            tokens[0].Position = new Vector3Int(50, 50, 50);

            // Act
            index.Rebuild(tokens);

            // Assert
            var nearby = index.FindTokensInRange(new Vector3Int(50, 50, 50), 5);
            Assert.Contains(tokens[0], nearby); // Should find token at new position
        }

        [Fact]
        public void Clear_RemovesAllTokens()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(60, 60, 60));

            index.Insert(token1);
            index.Insert(token2);

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
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            var center = new Vector3Int(50, 50, 50);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(52, 50, 50)); // Distance: 2
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(55, 50, 50)); // Distance: 5
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(60, 50, 50)); // Distance: 10

            index.Insert(token1);
            index.Insert(token2);
            index.Insert(token3);

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
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds, rebuildThreshold: 10);

            // Act - Add more tokens than threshold
            for (int i = 0; i < 15; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(i * 5, i * 5, i * 5));
                index.Insert(token);
            }

            // Assert - Index should still work correctly after auto-rebuild
            var nearby = index.FindTokensInRange(new Vector3Int(0, 0, 0), 10);
            Assert.NotEmpty(nearby);
        }

        [Fact]
        public void GetTokenClusters_GroupsNearbyTokens()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(200, 200, 200));
            var index = new SpatialIndex(bounds);

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
                index.Insert(token);
            }

            // Act
            var clusters = index.GetTokenClusters(clusterRadius: 5);

            // Assert
            Assert.True(clusters.Count >= 2); // At least 2 clusters
        }

        [Fact]
        public void GetDensityMap_CalculatesDensity()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3Int(0, 0, 0), new Vector3Int(100, 100, 100));
            var index = new SpatialIndex(bounds);

            // Add tokens in one area
            for (int i = 0; i < 10; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(50 + i, 50, 50));
                index.Insert(token);
            }

            // Act
            var densityMap = index.GetDensityMap(cellSize: 10);

            // Assert
            Assert.NotNull(densityMap);
            Assert.NotEmpty(densityMap);
        }
    }
}

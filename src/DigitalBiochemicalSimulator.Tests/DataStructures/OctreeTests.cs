using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.DataStructures
{
    public class OctreeTests
    {
        [Fact]
        public void Octree_Creation_InitializesCorrectly()
        {
            // Arrange & Act
            var octree = Octree.FromGrid(100, 100, 100);

            // Assert
            Assert.NotNull(octree);
            Assert.Equal(0, octree.TotalTokens);
            Assert.NotNull(octree.Bounds);
        }

        [Fact]
        public void Octree_Insert_AddsTokenSuccessfully()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(50, 50, 50));

            // Act
            bool inserted = octree.Insert(token);

            // Assert
            Assert.True(inserted);
            Assert.Equal(1, octree.TotalTokens);
        }

        [Fact]
        public void Octree_Insert_OutOfBounds_ReturnsFalse()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(150, 50, 50));

            // Act
            bool inserted = octree.Insert(token);

            // Assert
            Assert.False(inserted);
            Assert.Equal(0, octree.TotalTokens);
        }

        [Fact]
        public void Octree_Insert_MultipleTokens_AllAdded()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(25, 25, 25)),
                new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(75, 25, 25)),
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(25, 75, 25)),
                new Token(4, TokenType.INTEGER_LITERAL, "4", new Vector3Int(75, 75, 25)),
                new Token(5, TokenType.INTEGER_LITERAL, "5", new Vector3Int(50, 50, 50))
            };

            // Act
            foreach (var token in tokens)
            {
                octree.Insert(token);
            }

            // Assert
            Assert.Equal(5, octree.TotalTokens);
        }

        [Fact]
        public void Octree_Remove_RemovesTokenSuccessfully()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(50, 50, 50));
            octree.Insert(token);

            // Act
            bool removed = octree.Remove(token);

            // Assert
            Assert.True(removed);
            Assert.Equal(0, octree.TotalTokens);
        }

        [Fact]
        public void Octree_QueryRange_FindsTokensInRange()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var center = new Vector3Int(50, 50, 50);

            // Add tokens at various distances
            var nearToken = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(51, 51, 51));
            var farToken = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(90, 90, 90));

            octree.Insert(nearToken);
            octree.Insert(farToken);

            // Act
            var results = octree.QueryRange(center, 5.0f);

            // Assert
            Assert.Single(results);
            Assert.Contains(nearToken, results);
            Assert.DoesNotContain(farToken, results);
        }

        [Fact]
        public void Octree_QueryRange_EmptyRange_ReturnsEmpty()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50));
            octree.Insert(token);

            // Act
            var results = octree.QueryRange(new Vector3Int(90, 90, 90), 5.0f);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void Octree_QueryBox_FindsTokensInBox()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);

            var insideToken = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(30, 30, 30));
            var outsideToken = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(60, 60, 60));

            octree.Insert(insideToken);
            octree.Insert(outsideToken);

            var queryBox = new BoundingBox(
                new Vector3Int(20, 20, 20),
                new Vector3Int(40, 40, 40)
            );

            // Act
            var results = octree.QueryBox(queryBox);

            // Assert
            Assert.Single(results);
            Assert.Contains(insideToken, results);
            Assert.DoesNotContain(outsideToken, results);
        }

        [Fact]
        public void Octree_FindNearest_ReturnsClosestToken()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var searchPos = new Vector3Int(50, 50, 50);

            var closestToken = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(51, 51, 51));
            var farToken = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(90, 90, 90));

            octree.Insert(closestToken);
            octree.Insert(farToken);

            // Act
            var nearest = octree.FindNearest(searchPos);

            // Assert
            Assert.Equal(closestToken, nearest);
        }

        [Fact]
        public void Octree_FindNearestNeighbors_ReturnsKClosest()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var searchPos = new Vector3Int(50, 50, 50);

            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(51, 51, 51)), // Closest
                new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(52, 52, 52)), // 2nd closest
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(60, 60, 60)), // Far
                new Token(4, TokenType.INTEGER_LITERAL, "4", new Vector3Int(90, 90, 90))  // Farthest
            };

            foreach (var token in tokens)
            {
                octree.Insert(token);
            }

            // Act
            var nearest = octree.FindNearestNeighbors(searchPos, 2);

            // Assert
            Assert.Equal(2, nearest.Count);
            Assert.Contains(tokens[0], nearest); // Closest
            Assert.Contains(tokens[1], nearest); // 2nd closest
        }

        [Fact]
        public void Octree_Update_MovesTokenCorrectly()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var oldPos = new Vector3Int(25, 25, 25);
            var newPos = new Vector3Int(75, 75, 75);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", oldPos);

            octree.Insert(token);

            // Act
            token.Position = newPos;
            bool updated = octree.Update(token, oldPos);

            // Assert
            Assert.True(updated);
            Assert.Equal(1, octree.TotalTokens);

            // Verify token is at new position
            var nearOld = octree.QueryRange(oldPos, 5.0f);
            var nearNew = octree.QueryRange(newPos, 5.0f);

            Assert.Empty(nearOld);
            Assert.Single(nearNew);
        }

        [Fact]
        public void Octree_Clear_RemovesAllTokens()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            for (int i = 0; i < 10; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(i * 10, i * 10, i * 10));
                octree.Insert(token);
            }

            // Act
            octree.Clear();

            // Assert
            Assert.Equal(0, octree.TotalTokens);
        }

        [Fact]
        public void Octree_Subdivide_HandlesMultipleTokensInSameOctant()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100, maxTokensPerNode: 2);

            // Add 3 tokens in same general area to force subdivision
            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(25, 25, 25)),
                new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(26, 26, 26)),
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(27, 27, 27))
            };

            // Act
            foreach (var token in tokens)
            {
                octree.Insert(token);
            }

            // Assert
            Assert.Equal(3, octree.TotalTokens);

            // All tokens should still be findable
            var found = octree.QueryRange(new Vector3Int(26, 26, 26), 5.0f);
            Assert.Equal(3, found.Count);
        }

        [Fact]
        public void Octree_GetStatistics_ReturnsCorrectInfo()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            for (int i = 0; i < 10; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(i * 10, i * 10, i * 10));
                octree.Insert(token);
            }

            // Act
            var stats = octree.GetStatistics();

            // Assert
            Assert.Equal(10, stats.TotalTokens);
            Assert.True(stats.TotalNodes > 0);
            Assert.True(stats.LeafNodes > 0);
            Assert.True(stats.MaxDepth >= 0);
        }

        [Fact]
        public void BoundingBox_Contains_DetectsPointInside()
        {
            // Arrange
            var box = new BoundingBox(
                new Vector3Int(0, 0, 0),
                new Vector3Int(10, 10, 10)
            );

            // Act & Assert
            Assert.True(box.Contains(new Vector3Int(5, 5, 5)));
            Assert.False(box.Contains(new Vector3Int(15, 5, 5)));
            Assert.False(box.Contains(new Vector3Int(5, 15, 5)));
            Assert.False(box.Contains(new Vector3Int(5, 5, 15)));
        }

        [Fact]
        public void BoundingBox_Intersects_DetectsOverlap()
        {
            // Arrange
            var box1 = new BoundingBox(
                new Vector3Int(0, 0, 0),
                new Vector3Int(10, 10, 10)
            );

            var box2 = new BoundingBox(
                new Vector3Int(5, 5, 5),
                new Vector3Int(15, 15, 15)
            );

            var box3 = new BoundingBox(
                new Vector3Int(20, 20, 20),
                new Vector3Int(30, 30, 30)
            );

            // Act & Assert
            Assert.True(box1.Intersects(box2)); // Overlapping
            Assert.False(box1.Intersects(box3)); // Separate
        }

        [Fact]
        public void BoundingBox_IntersectsSphere_DetectsOverlap()
        {
            // Arrange
            var box = new BoundingBox(
                new Vector3Int(0, 0, 0),
                new Vector3Int(10, 10, 10)
            );

            // Act & Assert
            Assert.True(box.IntersectsSphere(new Vector3Int(5, 5, 5), 2.0f)); // Inside
            Assert.True(box.IntersectsSphere(new Vector3Int(12, 5, 5), 5.0f)); // Touching
            Assert.False(box.IntersectsSphere(new Vector3Int(20, 20, 20), 5.0f)); // Far away
        }

        [Fact]
        public void Octree_Rebuild_MaintainsAllTokens()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var tokens = new List<Token>();

            for (int i = 0; i < 20; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(),
                    new Vector3Int(i * 5, i * 5, i * 5));
                tokens.Add(token);
                octree.Insert(token);
            }

            // Act
            octree.Rebuild();

            // Assert
            Assert.Equal(20, octree.TotalTokens);

            // Verify all tokens are still findable
            var allFound = octree.QueryBox(octree.Bounds);
            Assert.Equal(20, allFound.Count);
        }
    }
}

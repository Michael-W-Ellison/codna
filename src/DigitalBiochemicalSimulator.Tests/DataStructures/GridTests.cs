using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Tests.DataStructures
{
    public class GridTests
    {
        [Fact]
        public void Grid_Creation_InitializesCorrectly()
        {
            // Arrange & Act
            var grid = new Grid(10, 20, 5, capacity: 3);

            // Assert
            Assert.Equal(10, grid.Width);
            Assert.Equal(20, grid.Height);
            Assert.Equal(5, grid.Depth);
            Assert.NotNull(grid.ActiveCells);
            Assert.Empty(grid.ActiveCells);
        }

        [Fact]
        public void Grid_AddToken_AddsToCorrectCell()
        {
            // Arrange
            var grid = new Grid(10, 10, 10, capacity: 3);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 5, 5));

            // Act
            bool result = grid.AddToken(token);

            // Assert
            Assert.True(result);
            Assert.Single(grid.ActiveCells);
            Assert.Contains(new Vector3Int(5, 5, 5), grid.ActiveCells);
        }

        [Fact]
        public void Grid_AddToken_RespectsCellCapacity()
        {
            // Arrange
            var grid = new Grid(10, 10, 10, capacity: 2);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(5, 5, 5));
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(5, 5, 5));
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(5, 5, 5));

            // Act
            bool result1 = grid.AddToken(token1);
            bool result2 = grid.AddToken(token2);
            bool result3 = grid.AddToken(token3);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.False(result3); // Should fail due to capacity
        }

        [Fact]
        public void Grid_RemoveToken_RemovesFromCell()
        {
            // Arrange
            var grid = new Grid(10, 10, 10, capacity: 3);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 5, 5));
            grid.AddToken(token);

            // Act
            bool result = grid.RemoveToken(token);

            // Assert
            Assert.True(result);
            Assert.Empty(grid.ActiveCells); // Cell should be removed from active cells
        }

        [Fact]
        public void Grid_GetCell_ReturnsCorrectCell()
        {
            // Arrange
            var grid = new Grid(10, 10, 10, capacity: 3);
            var position = new Vector3Int(5, 5, 5);

            // Act
            var cell = grid.GetCell(position);

            // Assert
            Assert.NotNull(cell);
            Assert.Equal(position, cell.Position);
        }

        [Fact]
        public void Grid_GetNeighbors_Returns9Neighbors()
        {
            // Arrange
            var grid = new Grid(10, 10, 10, capacity: 3);
            var position = new Vector3Int(5, 5, 5);

            // Act
            var neighbors = grid.GetNeighbors(position);

            // Assert
            Assert.Equal(9, neighbors.Count); // 8 horizontal + 1 below
        }

        [Fact]
        public void Grid_BoundaryCheck_WorksCorrectly()
        {
            // Arrange
            var grid = new Grid(10, 10, 10, capacity: 3);

            // Act & Assert
            Assert.True(grid.IsInBounds(new Vector3Int(0, 0, 0)));
            Assert.True(grid.IsInBounds(new Vector3Int(9, 9, 9)));
            Assert.False(grid.IsInBounds(new Vector3Int(-1, 0, 0)));
            Assert.False(grid.IsInBounds(new Vector3Int(10, 0, 0)));
            Assert.False(grid.IsInBounds(new Vector3Int(0, 20, 0)));
        }
    }
}

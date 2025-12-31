using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.DataStructures
{
    public class CellTests
    {
        [Fact]
        public void Cell_Constructor_InitializesCorrectly()
        {
            // Arrange
            var position = new Vector3Int(5, 10, 3);
            int capacity = 500;

            // Act
            var cell = new Cell(position, capacity);

            // Assert
            Assert.Equal(position, cell.Position);
            Assert.Equal(capacity, cell.Capacity);
            Assert.Equal(0, cell.TotalMass);
            Assert.Empty(cell.Tokens);
            Assert.True(cell.IsEmpty);
            Assert.False(cell.IsOverflowing);
            Assert.False(cell.IsInMutationZone);
        }

        [Fact]
        public void Cell_DefaultCapacity_Is1000()
        {
            // Arrange & Act
            var cell = new Cell(Vector3Int.Zero);

            // Assert
            Assert.Equal(1000, cell.Capacity);
        }

        [Fact]
        public void AddToken_ValidToken_AddsSuccessfully()
        {
            // Arrange
            var cell = new Cell(new Vector3Int(5, 10, 3), 1000);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 5 };

            // Act
            bool result = cell.AddToken(token);

            // Assert
            Assert.True(result);
            Assert.Single(cell.Tokens);
            Assert.Contains(token, cell.Tokens);
            Assert.Equal(5, cell.TotalMass);
            Assert.Equal(new Vector3Int(5, 10, 3), token.Position);
        }

        [Fact]
        public void AddToken_NullToken_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero);

            // Act
            bool result = cell.AddToken(null);

            // Assert
            Assert.False(result);
            Assert.Empty(cell.Tokens);
            Assert.Equal(0, cell.TotalMass);
        }

        [Fact]
        public void AddToken_MultipleTokens_UpdatesTotalMass()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 10 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Mass = 15 };

            // Act
            cell.AddToken(token1);
            cell.AddToken(token2);

            // Assert
            Assert.Equal(2, cell.Tokens.Count);
            Assert.Equal(25, cell.TotalMass);
        }

        [Fact]
        public void RemoveToken_ExistingToken_RemovesSuccessfully()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 5 };
            cell.AddToken(token);

            // Act
            bool result = cell.RemoveToken(token);

            // Assert
            Assert.True(result);
            Assert.Empty(cell.Tokens);
            Assert.Equal(0, cell.TotalMass);
        }

        [Fact]
        public void RemoveToken_NonExistentToken_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 5 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Mass = 3 };
            cell.AddToken(token1);

            // Act
            bool result = cell.RemoveToken(token2);

            // Assert
            Assert.False(result);
            Assert.Single(cell.Tokens);
            Assert.Equal(5, cell.TotalMass);
        }

        [Fact]
        public void RemoveToken_NullToken_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 5 };
            cell.AddToken(token);

            // Act
            bool result = cell.RemoveToken(null);

            // Assert
            Assert.False(result);
            Assert.Single(cell.Tokens);
            Assert.Equal(5, cell.TotalMass);
        }

        [Fact]
        public void CanAcceptToken_WithinCapacity_ReturnsTrue()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 50 };

            // Act
            bool canAccept = cell.CanAcceptToken(token);

            // Assert
            Assert.True(canAccept);
        }

        [Fact]
        public void CanAcceptToken_ExactlyAtCapacity_ReturnsTrue()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 60 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Mass = 40 };
            cell.AddToken(token1);

            // Act
            bool canAccept = cell.CanAcceptToken(token2);

            // Assert
            Assert.True(canAccept);
        }

        [Fact]
        public void CanAcceptToken_ExceedsCapacity_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 60 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Mass = 50 };
            cell.AddToken(token1);

            // Act
            bool canAccept = cell.CanAcceptToken(token2);

            // Assert
            Assert.False(canAccept);
        }

        [Fact]
        public void IsOverflowing_BelowCapacity_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 50 };
            cell.AddToken(token);

            // Act & Assert
            Assert.False(cell.IsOverflowing);
        }

        [Fact]
        public void IsOverflowing_AtCapacity_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 100 };
            cell.AddToken(token);

            // Act & Assert
            Assert.False(cell.IsOverflowing);
        }

        [Fact]
        public void IsOverflowing_ExceedsCapacity_ReturnsTrue()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 60 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Mass = 50 };
            cell.AddToken(token1);
            cell.AddToken(token2);

            // Act & Assert
            Assert.True(cell.IsOverflowing);
            Assert.Equal(110, cell.TotalMass);
        }

        [Fact]
        public void IsEmpty_NewCell_ReturnsTrue()
        {
            // Arrange & Act
            var cell = new Cell(Vector3Int.Zero);

            // Assert
            Assert.True(cell.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithTokens_ReturnsFalse()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);

            // Act
            cell.AddToken(token);

            // Assert
            Assert.False(cell.IsEmpty);
        }

        [Fact]
        public void GetOverflowAmount_NoOverflow_ReturnsZero()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 50 };
            cell.AddToken(token);

            // Act
            int overflow = cell.GetOverflowAmount();

            // Assert
            Assert.Equal(0, overflow);
        }

        [Fact]
        public void GetOverflowAmount_WithOverflow_ReturnsExcessMass()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 100);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 60 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Mass = 50 };
            cell.AddToken(token1);
            cell.AddToken(token2);

            // Act
            int overflow = cell.GetOverflowAmount();

            // Assert
            Assert.Equal(10, overflow); // 110 - 100
        }

        [Fact]
        public void GetRedistributableTokens_ReturnsOnlyFallingTokens()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var fallingToken1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 0, Mass = 5 };
            var fallingToken2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Energy = 0, Mass = 3 };
            var activeToken = new Token(3, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Energy = 50, Mass = 10 };

            cell.AddToken(fallingToken1);
            cell.AddToken(fallingToken2);
            cell.AddToken(activeToken);

            // Act
            var redistributable = cell.GetRedistributableTokens();

            // Assert
            Assert.Equal(2, redistributable.Count);
            Assert.Contains(fallingToken1, redistributable);
            Assert.Contains(fallingToken2, redistributable);
            Assert.DoesNotContain(activeToken, redistributable);
        }

        [Fact]
        public void GetRedistributableTokens_OrderedByMass()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 0, Mass = 10 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Energy = 0, Mass = 5 };
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero) { Energy = 0, Mass = 15 };

            cell.AddToken(token1);
            cell.AddToken(token2);
            cell.AddToken(token3);

            // Act
            var redistributable = cell.GetRedistributableTokens();

            // Assert
            Assert.Equal(3, redistributable.Count);
            Assert.Equal(token2, redistributable[0]); // Mass 5
            Assert.Equal(token1, redistributable[1]); // Mass 10
            Assert.Equal(token3, redistributable[2]); // Mass 15
        }

        [Fact]
        public void Clear_RemovesAllTokens()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 10 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Mass = 15 };
            cell.AddToken(token1);
            cell.AddToken(token2);

            // Act
            cell.Clear();

            // Assert
            Assert.Empty(cell.Tokens);
            Assert.Equal(0, cell.TotalMass);
            Assert.True(cell.IsEmpty);
        }

        [Fact]
        public void IsInMutationZone_CanBeSet()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero);
            Assert.False(cell.IsInMutationZone);

            // Act
            cell.IsInMutationZone = true;

            // Assert
            Assert.True(cell.IsInMutationZone);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var position = new Vector3Int(5, 10, 3);
            var cell = new Cell(position, 500);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Mass = 100 };
            cell.AddToken(token);

            // Act
            string result = cell.ToString();

            // Assert
            Assert.Contains("5", result);
            Assert.Contains("10", result);
            Assert.Contains("3", result);
            Assert.Contains("1", result); // Token count
            Assert.Contains("100", result); // Total mass
            Assert.Contains("500", result); // Capacity
        }

        [Fact]
        public void Cell_MultipleSameTokens_TracksCorrectly()
        {
            // Arrange
            var cell = new Cell(Vector3Int.Zero, 1000);
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 10 };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 10 };
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Mass = 10 };

            // Act
            cell.AddToken(token1);
            cell.AddToken(token2);
            cell.AddToken(token3);

            // Assert
            Assert.Equal(3, cell.Tokens.Count);
            Assert.Equal(30, cell.TotalMass);
        }
    }
}

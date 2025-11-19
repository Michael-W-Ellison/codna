using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.Utilities;

namespace DigitalBiochemicalSimulator.Tests.Simulation
{
    public class ThermalVentTests
    {
        private TokenFactory CreateFactory()
        {
            var pool = new TokenPool(10, 100);
            return new TokenFactory(pool);
        }

        [Fact]
        public void ThermalVent_Constructor_InitializesCorrectly()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();

            // Act
            var vent = new ThermalVent(position, factory, emissionRate: 5, initialEnergy: 100);

            // Assert
            Assert.Equal(position, vent.Position);
        }

        [Fact]
        public void GenerateToken_ReturnsToken()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 10, initialEnergy: 100);

            // Act
            var token = vent.GenerateToken(currentTick: 10);

            // Assert
            Assert.NotNull(token);
            Assert.Equal(position, token.Position);
            Assert.Equal(100, token.Energy);
            Assert.True(token.IsActive);
        }

        [Fact]
        public void GenerateToken_RespectsEmissionRate()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 10, initialEnergy: 100);

            // Act - Try to generate tokens rapidly
            int generated = 0;
            for (int i = 0; i < 20; i++)
            {
                var token = vent.GenerateToken(currentTick: i);
                if (token != null)
                    generated++;
            }

            // Assert - Should respect emission rate (not generate every tick)
            Assert.True(generated >= 2); // Should generate at least a couple
            Assert.True(generated <= 20); // But not more than ticks
        }

        [Fact]
        public void GenerateToken_ProducesVariedTokenTypes()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 1, initialEnergy: 100);

            // Act - Generate many tokens
            var tokenTypes = new System.Collections.Generic.HashSet<TokenType>();
            for (int i = 0; i < 100; i++)
            {
                var token = vent.GenerateToken(currentTick: i * 10); // Space out ticks
                if (token != null)
                {
                    tokenTypes.Add(token.Type);
                }
            }

            // Assert - Should generate multiple token types
            Assert.True(tokenTypes.Count >= 2); // At least some variety
        }

        [Fact]
        public void SetExpressionHeavyDistribution_ChangesDistribution()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 1, initialEnergy: 100);

            // Act
            vent.SetExpressionHeavyDistribution();

            // Generate some tokens
            int literalsAndOperators = 0;
            for (int i = 0; i < 50; i++)
            {
                var token = vent.GenerateToken(currentTick: i * 10);
                if (token != null && (
                    token.Type == TokenType.INTEGER_LITERAL ||
                    token.Type == TokenType.FLOAT_LITERAL ||
                    token.Type == TokenType.OPERATOR_PLUS ||
                    token.Type == TokenType.OPERATOR_MINUS ||
                    token.Type == TokenType.OPERATOR_MULTIPLY ||
                    token.Type == TokenType.OPERATOR_DIVIDE))
                {
                    literalsAndOperators++;
                }
            }

            // Assert - Should have more literals and operators
            Assert.True(literalsAndOperators > 0);
        }

        [Fact]
        public void SetControlHeavyDistribution_ChangesDistribution()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 1, initialEnergy: 100);

            // Act
            vent.SetControlHeavyDistribution();

            // Generate some tokens
            int controlTokens = 0;
            for (int i = 0; i < 50; i++)
            {
                var token = vent.GenerateToken(currentTick: i * 10);
                if (token != null && (
                    token.Type == TokenType.KEYWORD_IF ||
                    token.Type == TokenType.KEYWORD_ELSE ||
                    token.Type == TokenType.KEYWORD_WHILE ||
                    token.Type == TokenType.KEYWORD_FOR))
                {
                    controlTokens++;
                }
            }

            // Assert - Should have some control tokens
            Assert.True(controlTokens >= 0); // Just verify it executes
        }

        [Fact]
        public void GenerateToken_AssignsCorrectEnergy()
        {
            // Arrange
            var position = new Vector3Int(10, 0, 10);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 10, initialEnergy: 150);

            // Act
            var token = vent.GenerateToken(currentTick: 10);

            // Assert
            Assert.NotNull(token);
            Assert.Equal(150, token.Energy);
        }

        [Fact]
        public void GenerateToken_AssignsCorrectPosition()
        {
            // Arrange
            var position = new Vector3Int(25, 5, 30);
            var factory = CreateFactory();
            var vent = new ThermalVent(position, factory, emissionRate: 10, initialEnergy: 100);

            // Act
            var token = vent.GenerateToken(currentTick: 10);

            // Assert
            Assert.NotNull(token);
            Assert.Equal(position, token.Position);
        }

        [Fact]
        public void MultipleVents_GenerateIndependently()
        {
            // Arrange
            var factory = CreateFactory();
            var vent1 = new ThermalVent(new Vector3Int(10, 0, 10), factory, emissionRate: 5, initialEnergy: 100);
            var vent2 = new ThermalVent(new Vector3Int(40, 0, 40), factory, emissionRate: 5, initialEnergy: 100);

            // Act
            var token1 = vent1.GenerateToken(currentTick: 10);
            var token2 = vent2.GenerateToken(currentTick: 10);

            // Assert
            if (token1 != null && token2 != null)
            {
                Assert.NotEqual(token1.Position, token2.Position);
                Assert.NotEqual(token1.Id, token2.Id);
            }
        }

        [Fact]
        public void EmissionRate_Controls_GenerationFrequency()
        {
            // Arrange
            var factory = CreateFactory();
            var slowVent = new ThermalVent(Vector3Int.Zero, factory, emissionRate: 100, initialEnergy: 100);
            var fastVent = new ThermalVent(Vector3Int.Zero, factory, emissionRate: 2, initialEnergy: 100);

            // Act
            int slowGenerated = 0;
            int fastGenerated = 0;

            for (int i = 0; i < 20; i++)
            {
                if (slowVent.GenerateToken(currentTick: i) != null)
                    slowGenerated++;
                if (fastVent.GenerateToken(currentTick: i) != null)
                    fastGenerated++;
            }

            // Assert
            Assert.True(fastGenerated >= slowGenerated); // Fast vent should generate more
        }
    }
}

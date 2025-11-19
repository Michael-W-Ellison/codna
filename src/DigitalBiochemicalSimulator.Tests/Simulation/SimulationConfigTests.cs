using Xunit;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Tests.Simulation
{
    public class SimulationConfigTests
    {
        [Fact]
        public void SimulationConfig_Constructor_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var config = new SimulationConfig(50, 50, 50);

            // Assert
            Assert.Equal(50, config.GridWidth);
            Assert.Equal(50, config.GridHeight);
            Assert.Equal(50, config.GridDepth);
            Assert.True(config.MaxTokens > 0);
            Assert.True(config.TicksPerSecond > 0);
        }

        [Fact]
        public void SimulationConfig_CustomValues_StoresCorrectly()
        {
            // Arrange & Act
            var config = new SimulationConfig(100, 200, 150)
            {
                MaxTokens = 1000,
                TokenGenerationRate = 15,
                GravityStrength = 1.5f,
                CellCapacity = 2000,
                TicksPerSecond = 120
            };

            // Assert
            Assert.Equal(100, config.GridWidth);
            Assert.Equal(200, config.GridHeight);
            Assert.Equal(150, config.GridDepth);
            Assert.Equal(1000, config.MaxTokens);
            Assert.Equal(15, config.TokenGenerationRate);
            Assert.Equal(1.5f, config.GravityStrength);
            Assert.Equal(2000, config.CellCapacity);
            Assert.Equal(120, config.TicksPerSecond);
        }

        [Fact]
        public void Validate_ValidConfig_ReturnsTrue()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            bool isValid = config.Validate(out string errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.True(string.IsNullOrEmpty(errorMessage));
        }

        [Fact]
        public void Validate_ZeroGridSize_ReturnsFalse()
        {
            // Arrange
            var config = new SimulationConfig(0, 50, 50);

            // Act
            bool isValid = config.Validate(out string errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errorMessage);
            Assert.Contains("width", errorMessage.ToLower());
        }

        [Fact]
        public void Validate_NegativeMaxTokens_ReturnsFalse()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50)
            {
                MaxTokens = -10
            };

            // Act
            bool isValid = config.Validate(out string errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errorMessage);
        }

        [Fact]
        public void Validate_ExcessiveGridSize_ReturnsFalse()
        {
            // Arrange
            var config = new SimulationConfig(10000, 10000, 10000);

            // Act
            bool isValid = config.Validate(out string errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errorMessage);
            Assert.Contains("large", errorMessage.ToLower());
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new SimulationConfig(50, 60, 70)
            {
                MaxTokens = 500,
                GravityStrength = 1.2f,
                CellCapacity = 1500
            };

            // Act
            var clone = original.Clone();
            clone.MaxTokens = 1000;
            clone.GravityStrength = 2.0f;

            // Assert
            Assert.Equal(50, clone.GridWidth);
            Assert.Equal(60, clone.GridHeight);
            Assert.Equal(70, clone.GridDepth);
            Assert.Equal(1000, clone.MaxTokens);
            Assert.Equal(2.0f, clone.GravityStrength);

            // Original should be unchanged
            Assert.Equal(500, original.MaxTokens);
            Assert.Equal(1.2f, original.GravityStrength);
        }

        [Fact]
        public void TickDuration_CalculatedFromTicksPerSecond()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50)
            {
                TicksPerSecond = 60
            };

            // Act
            long tickDuration = config.TickDuration;

            // Assert
            Assert.Equal(1000 / 60, tickDuration); // Milliseconds per tick
        }

        [Fact]
        public void VentPosition_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);
            var position = new Vector3Int(25, 0, 25);

            // Act
            config.VentPosition = position;

            // Assert
            Assert.Equal(position, config.VentPosition);
        }

        [Fact]
        public void TokenDistribution_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.TokenDistribution = TokenDistributionProfile.ExpressionHeavy;

            // Assert
            Assert.Equal(TokenDistributionProfile.ExpressionHeavy, config.TokenDistribution);
        }

        [Fact]
        public void VentDistribution_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.VentDistribution = VentDistribution.Distributed;

            // Assert
            Assert.Equal(VentDistribution.Distributed, config.VentDistribution);
        }

        [Fact]
        public void NumberOfVents_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.NumberOfVents = 5;

            // Assert
            Assert.Equal(5, config.NumberOfVents);
        }

        [Fact]
        public void VentEmissionRate_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.VentEmissionRate = 10;

            // Assert
            Assert.Equal(10, config.VentEmissionRate);
        }

        [Fact]
        public void InitialTokenEnergy_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.InitialTokenEnergy = 150;

            // Assert
            Assert.Equal(150, config.InitialTokenEnergy);
        }

        [Fact]
        public void BondingRange_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.BondingRange = 3.5f;

            // Assert
            Assert.Equal(3.5f, config.BondingRange);
        }

        [Fact]
        public void DamageMultiplier_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.DamageMultiplier = 2.0f;

            // Assert
            Assert.Equal(2.0f, config.DamageMultiplier);
        }

        [Fact]
        public void MutationRange_CanBeSet()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);

            // Act
            config.MutationRange = 15;

            // Assert
            Assert.Equal(15, config.MutationRange);
        }
    }
}

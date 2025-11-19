using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Physics;
using DigitalBiochemicalSimulator.Simulation;
using System.Collections.Generic;

namespace DigitalBiochemicalSimulator.Tests.Physics
{
    public class EnergyManagerTests
    {
        [Fact]
        public void EnergyManager_UpdateTokenEnergy_DecreasesEnergyForRisingTokens()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var energyManager = new EnergyManager(config);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 5, 5))
            {
                Energy = 50
            };
            var tokens = new List<Token> { token };

            // Act
            energyManager.UpdateTokenEnergy(tokens);

            // Assert
            Assert.True(token.Energy < 50); // Energy should decrease
        }

        [Fact]
        public void EnergyManager_UpdateTokenEnergy_SetsIsFallingWhenEnergyIsZero()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var energyManager = new EnergyManager(config);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 5, 5))
            {
                Energy = 0
            };
            var tokens = new List<Token> { token };

            // Act
            energyManager.UpdateTokenEnergy(tokens);

            // Assert
            Assert.True(token.IsFalling);
            Assert.Equal(0, token.Energy);
        }

        [Fact]
        public void EnergyManager_GetAverageEnergy_CalculatesCorrectly()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var energyManager = new EnergyManager(config);
            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero) { Energy = 10, IsActive = true },
                new Token(2, TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero) { Energy = 20, IsActive = true },
                new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Energy = 30, IsActive = true }
            };

            // Act
            float avg = energyManager.GetAverageEnergy(tokens);

            // Assert
            Assert.Equal(20f, avg);
        }

        [Fact]
        public void EnergyManager_DistributeChainEnergy_IncreasesTotalEnergy()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var energyManager = new EnergyManager(config);
            var chain = new TokenChain(new Token(1, TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero))
            {
                Length = 3,
                TotalEnergy = 30
            };
            int energyPerBond = 5;

            // Act
            energyManager.DistributeChainEnergy(chain, energyPerBond);

            // Assert
            // Energy should increase by (length-1) * energyPerBond = 2 * 5 = 10
            Assert.Equal(40, chain.TotalEnergy);
        }
    }
}

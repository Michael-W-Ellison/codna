using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.DataStructures;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.Integration
{
    public class SimulationIntegrationTests
    {
        [Fact]
        public void IntegratedSimulation_CanInitialize_WithStandardConfig()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();

            // Act
            var simulation = new IntegratedSimulationEngine(config);

            // Assert
            Assert.NotNull(simulation);
            Assert.NotNull(simulation.Grid);
            Assert.NotNull(simulation.BondingManager);
            Assert.NotNull(simulation.DamageSystem);
            Assert.NotNull(simulation.Statistics);
            Assert.Empty(simulation.ActiveTokens);
        }

        [Fact]
        public void IntegratedSimulation_GeneratesTokens_WhenRunning()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            config.NumberOfVents = 1;
            config.VentEmissionRate = 2; // Generate token every 2 ticks
            var simulation = new IntegratedSimulationEngine(config);

            // Act
            simulation.Start();
            for (int i = 0; i < 10; i++)
            {
                simulation.Update();
            }
            simulation.Stop();

            // Assert
            Assert.True(simulation.ActiveTokens.Count > 0, "Should generate at least one token");
        }

        [Fact]
        public void IntegratedSimulation_FormsBonds_BetweenCompatibleTokens()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            config.GridWidth = 5;
            config.GridHeight = 5;
            config.GridDepth = 5;

            var simulation = new IntegratedSimulationEngine(config);

            // Manually add compatible tokens to same cell
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(2, 2, 2))
            {
                Energy = 50,
                IsActive = true,
                Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2)
            };

            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(2, 2, 2))
            {
                Energy = 50,
                IsActive = true,
                Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 2)
            };

            simulation.Grid.AddToken(token1);
            simulation.Grid.AddToken(token2);
            simulation.ActiveTokens.Add(token1);
            simulation.ActiveTokens.Add(token2);

            // Act
            long currentTick = 1;
            bool bonded = simulation.BondingManager.AttemptBond(token1, token2, currentTick);

            // Assert
            Assert.True(bonded, "Compatible tokens should bond");
            Assert.Contains(token2, token1.BondedTokens);
            Assert.Contains(token1, token2.BondedTokens);
        }

        [Fact]
        public void IntegratedSimulation_AppliesDamage_AtHighAltitude()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var simulation = new IntegratedSimulationEngine(config);

            // Create token at high altitude
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, config.GridHeight - 1, 5))
            {
                Energy = 50,
                IsActive = true,
                DamageLevel = 0.0f
            };

            simulation.ActiveTokens.Add(token);

            // Act
            for (int i = 0; i < 100; i++)
            {
                simulation.DamageSystem.ApplyDamage(token, i);
            }

            // Assert
            Assert.True(token.DamageLevel > 0.0f, "Token at high altitude should accumulate damage");
        }

        [Fact]
        public void IntegratedSimulation_UpdatesStatistics_Correctly()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            var simulation = new IntegratedSimulationEngine(config);

            // Add some test tokens
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero) { Energy = 50, IsActive = true };
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero) { Energy = 30, IsActive = true };

            simulation.ActiveTokens.Add(token1);
            simulation.ActiveTokens.Add(token2);

            // Act
            var snapshot = simulation.Statistics.CaptureSnapshot(currentTick: 10);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(10, snapshot.Tick);
            Assert.Equal(2, snapshot.ActiveTokens);
            Assert.Equal(80, snapshot.TotalEnergy);
            Assert.Equal(40.0, snapshot.AverageEnergy);
        }

        [Fact]
        public void IntegratedSimulation_TracksChains_InRegistry()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            var simulation = new IntegratedSimulationEngine(config);

            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero)
            {
                Energy = 50,
                IsActive = true
            };

            var chain = new TokenChain(token);

            // Act
            long chainId = simulation.ChainRegistry.RegisterChain(chain);

            // Assert
            Assert.True(chainId > 0);
            Assert.Equal(1, simulation.ChainRegistry.Count);

            var retrievedChain = simulation.ChainRegistry.GetChain(chainId);
            Assert.NotNull(retrievedChain);
            Assert.Equal(token, retrievedChain.Head);
        }

        [Fact]
        public void IntegratedSimulation_CompleteLoop_ExecutesWithoutErrors()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            config.MaxActiveTokens = 50;
            config.NumberOfVents = 1;
            config.VentEmissionRate = 5;

            var simulation = new IntegratedSimulationEngine(config);

            // Act & Assert - should not throw
            simulation.Start();
            for (int i = 0; i < 50; i++)
            {
                simulation.Update();
            }
            simulation.Stop();

            var stats = simulation.GetStatistics();
            Assert.NotNull(stats);
            Assert.True(stats.CurrentTick > 0);
        }

        [Fact]
        public void IntegratedSimulation_Reset_ClearsAllState()
        {
            // Arrange
            var config = SimulationConfig.CreateMinimal();
            var simulation = new IntegratedSimulationEngine(config);

            // Add some data
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero) { Energy = 50, IsActive = true };
            simulation.ActiveTokens.Add(token);
            simulation.Grid.AddToken(token);

            // Act
            simulation.Reset();

            // Assert
            Assert.Empty(simulation.ActiveTokens);
            Assert.Empty(simulation.Grid.ActiveCells);
            Assert.Equal(0, simulation.ChainRegistry.Count);
        }
    }
}

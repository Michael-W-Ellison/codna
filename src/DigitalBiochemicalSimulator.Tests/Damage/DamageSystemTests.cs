using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Damage;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.DataStructures;
using System.Collections.Generic;

namespace DigitalBiochemicalSimulator.Tests.Damage
{
    public class DamageSystemTests
    {
        [Fact]
        public void DamageSystem_ApplyDamage_IncreasesDamageLevelOverTime()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var bondingManager = CreateBondingManager(config);
            var damageSystem = new DamageSystem(config, bondingManager);

            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, config.GridHeight - 1, 5))
            {
                Energy = 50,
                IsActive = true,
                DamageLevel = 0.0f
            };

            // Act
            for (int i = 0; i < 100; i++)
            {
                damageSystem.ApplyDamage(token, currentTick: i);
            }

            // Assert
            Assert.True(token.DamageLevel > 0.0f, "Damage should increase over time at high altitude");
        }

        [Fact]
        public void DamageSystem_ApplyDamage_HigherAltitudeMeansMoreDamage()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var bondingManager = CreateBondingManager(config);
            var damageSystem = new DamageSystem(config, bondingManager);

            var lowToken = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(5, 5, 5))
            {
                Energy = 50,
                IsActive = true,
                DamageLevel = 0.0f
            };

            var highToken = new Token(2, TokenType.INTEGER_LITERAL, "2", new Vector3Int(5, config.GridHeight - 1, 5))
            {
                Energy = 50,
                IsActive = true,
                DamageLevel = 0.0f
            };

            // Act
            for (int i = 0; i < 50; i++)
            {
                damageSystem.ApplyDamage(lowToken, currentTick: i);
                damageSystem.ApplyDamage(highToken, currentTick: i);
            }

            // Assert
            Assert.True(highToken.DamageLevel >= lowToken.DamageLevel,
                "Higher altitude token should have equal or more damage");
        }

        [Fact]
        public void DamageSystem_GetDamageStatistics_ReturnsCorrectCounts()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var bondingManager = CreateBondingManager(config);
            var damageSystem = new DamageSystem(config, bondingManager);

            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero)
                    { IsActive = true, IsDamaged = false, DamageLevel = 0.0f },
                new Token(2, TokenType.INTEGER_LITERAL, "2", Vector3Int.Zero)
                    { IsActive = true, IsDamaged = true, DamageLevel = 0.5f },
                new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero)
                    { IsActive = true, IsDamaged = true, DamageLevel = 0.9f }
            };

            // Act
            var stats = damageSystem.GetDamageStatistics(tokens);

            // Assert
            Assert.Equal(3, stats.TotalTokens);
            Assert.Equal(2, stats.DamagedTokens);
            Assert.Equal(1, stats.CriticallyDamagedTokens);
        }

        [Fact]
        public void DamageSystem_AttemptRepair_ReducesDamageLevel()
        {
            // Arrange
            var config = SimulationConfig.CreateStandard();
            var bondingManager = CreateBondingManager(config);
            var damageSystem = new DamageSystem(config, bondingManager);

            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero)
            {
                Energy = 100,
                IsActive = true,
                IsDamaged = true,
                DamageLevel = 0.5f
            };

            float initialDamage = token.DamageLevel;

            // Act
            bool repaired = damageSystem.AttemptRepair(token, energyCost: 20);

            // Assert
            Assert.True(repaired);
            Assert.True(token.DamageLevel < initialDamage, "Damage should decrease after repair");
        }

        private BondingManager CreateBondingManager(SimulationConfig config)
        {
            var grammarRules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(grammarRules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);
            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth, config.CellCapacity);
            return new BondingManager(rulesEngine, strengthCalculator, config, grid);
        }
    }
}

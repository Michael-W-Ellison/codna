using System.Collections.Generic;
using Xunit;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Damage;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Physics;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Tests.Simulation
{
    /// <summary>
    /// Verifies that each SimulationConfig setting changes simulation behavior
    /// </summary>
    public class ConfigEffectTests
    {
        private static Token MakeToken(long id, TokenType type, string value, Vector3Int position, int energy = 100)
        {
            return new Token(id, type, value, position) { Energy = energy, IsActive = true };
        }

        private static (BondingManager manager, BondStrengthCalculator calculator) CreateBondingManager(SimulationConfig config, Grid grid)
        {
            var rulesEngine = new BondRulesEngine(GrammarLibrary.GetDefaultGrammar());
            var calculator = new BondStrengthCalculator(rulesEngine);
            return (new BondingManager(rulesEngine, calculator, config, grid), calculator);
        }

        private static DamageSystem CreateDamageSystem(SimulationConfig config)
        {
            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth);
            return new DamageSystem(config, CreateBondingManager(config, grid).manager);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void TokenGenerationRate_SetsTokensPerEmission(int rate)
        {
            var config = SimulationConfig.CreateMinimal();
            config.NumberOfVents = 1;
            config.VentEmissionRate = 1;
            config.TokenGenerationRate = rate;
            var simulation = new IntegratedSimulationEngine(config);

            simulation.Step();

            Assert.Equal(rate, simulation.TotalTokensGenerated);
        }

        [Theory]
        [InlineData(0.0f, 0)]
        [InlineData(1.0f, 1)]
        [InlineData(2.0f, 2)]
        public void GravityStrength_ScalesFallDistance(float gravityStrength, int expectedFall)
        {
            var config = new SimulationConfig(10, 10, 20) { FallRate = 1, GravityStrength = gravityStrength };
            var grid = new Grid(10, 10, 20);
            var motion = new MotionController(config, grid);
            var token = MakeToken(1, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 10), energy: 0);
            grid.AddToken(token);

            motion.UpdateTokenMotion(new List<Token> { token });

            Assert.Equal(10 - expectedFall, token.Position.Z);
        }

        [Fact]
        public void DamageMultiplier_ScalesDamageRate()
        {
            var config = new SimulationConfig(10, 10, 20) { MutationRange = 20 };
            float baseRate = CreateDamageSystem(config).CalculateDamageRate(19);

            config.DamageMultiplier = 2.0f;
            float doubledRate = CreateDamageSystem(config).CalculateDamageRate(19);

            Assert.True(baseRate > 0);
            Assert.Equal(baseRate * 2, doubledRate, 5);
        }

        [Fact]
        public void DamageMultiplier_Zero_PreventsDamage()
        {
            var config = new SimulationConfig(10, 10, 20) { MutationRange = 20, BaseDamageRate = 1.0f, DamageMultiplier = 0.0f };
            var damageSystem = CreateDamageSystem(config);
            var token = MakeToken(1, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 19));

            for (int i = 0; i < 100; i++)
                damageSystem.ApplyDamage(token, i);

            Assert.Equal(0.0f, token.DamageLevel);
        }

        [Fact]
        public void BaseDamageRate_And_DamageExponent_SetDamageRate()
        {
            var config = new SimulationConfig(10, 10, 21) { MutationRange = 21, BaseDamageRate = 0.5f, DamageExponent = 1.0f };

            // Altitude 10 of a top layer at 20 is halfway up
            Assert.Equal(0.25f, CreateDamageSystem(config).CalculateDamageRate(10), 5);

            config.DamageExponent = 2.0f;
            Assert.Equal(0.125f, CreateDamageSystem(config).CalculateDamageRate(10), 5);
        }

        [Fact]
        public void MutationRange_LimitsDamageToTopLayers()
        {
            var config = new SimulationConfig(10, 10, 20) { MutationRange = 5 };
            var damageSystem = CreateDamageSystem(config);

            Assert.Equal(0.0f, damageSystem.CalculateDamageRate(14));
            Assert.True(damageSystem.CalculateDamageRate(15) > 0);
        }

        [Fact]
        public void MutationFalloff_Abrupt_UsesConstantRateWithinRange()
        {
            var config = new SimulationConfig(10, 10, 20)
            {
                MutationRange = 10,
                BaseDamageRate = 0.2f,
                MutationFalloff = MutationFalloff.Abrupt
            };
            var damageSystem = CreateDamageSystem(config);

            Assert.Equal(0.2f, damageSystem.CalculateDamageRate(10), 5);
            Assert.Equal(0.2f, damageSystem.CalculateDamageRate(19), 5);

            config.MutationFalloff = MutationFalloff.Gradual;
            Assert.True(CreateDamageSystem(config).CalculateDamageRate(10) < 0.2f);
        }

        [Fact]
        public void MutationRate_Zero_PreventsDamage()
        {
            var config = new SimulationConfig(10, 10, 20) { MutationRange = 20, BaseDamageRate = 1.0f, MutationRate = 0 };
            var damageSystem = CreateDamageSystem(config);
            var token = MakeToken(1, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 19));

            damageSystem.ApplyDamage(token, 0);

            Assert.Equal(0.0f, token.DamageLevel);
        }

        [Fact]
        public void DamageIncrement_Zero_LeavesDamageLevelUnchanged()
        {
            var config = new SimulationConfig(10, 10, 20) { MutationRange = 20, BaseDamageRate = 1.0f, DamageIncrement = 0.0f };
            var damageSystem = CreateDamageSystem(config);
            var token = MakeToken(1, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 19));

            for (int i = 0; i < 10; i++)
                damageSystem.ApplyDamage(token, i);

            Assert.Equal(0.0f, token.DamageLevel);
        }

        [Fact]
        public void CriticalDamageThreshold_BreaksBondsWhenReached()
        {
            var config = new SimulationConfig(10, 10, 20) { MutationRange = 20, BaseDamageRate = 1.0f, CriticalDamageThreshold = 0.0f };
            var grid = new Grid(10, 10, 20);
            var (manager, _) = CreateBondingManager(config, grid);
            var damageSystem = new DamageSystem(config, manager);
            var token1 = MakeToken(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(5, 5, 19));
            var token2 = MakeToken(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(5, 5, 19));
            Assert.True(manager.AttemptBond(token1, token2, 0));

            damageSystem.ApplyDamage(token1, 1);

            Assert.Empty(token1.BondedTokens);
        }

        [Fact]
        public void MinBondStrength_AboveBondStrength_PreventsBond()
        {
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(10, 10, 10);
            var (manager, calculator) = CreateBondingManager(config, grid);
            var token1 = MakeToken(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = MakeToken(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            config.MinBondStrength = calculator.CalculateBondStrength(token1, token2) + 0.01f;

            Assert.False(manager.AttemptBond(token1, token2, 0));
        }

        [Theory]
        [InlineData(0.0f, 0, 0)]
        [InlineData(1.0f, 0, 1)]
        [InlineData(0.0f, 1, 1)]
        public void BondingRange_And_BondingRadius_AllowBondsBetweenCells(float bondingRange, int bondingRadius, int expectedBonds)
        {
            var config = new SimulationConfig(10, 10, 10) { BondingRange = bondingRange, BondingRadius = bondingRadius };
            var grid = new Grid(10, 10, 10);
            var (manager, _) = CreateBondingManager(config, grid);
            var processor = new CellProcessor(manager, new RepulsionHandler(grid, new BondRulesEngine(GrammarLibrary.GetDefaultGrammar())), config, grid);
            var token1 = MakeToken(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(2, 2, 2));
            var token2 = MakeToken(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(3, 2, 2));
            grid.AddToken(token1);
            grid.AddToken(token2);

            var cell = grid.GetCell(token1.Position);
            Assert.NotNull(cell);
            int bonds = processor.AttemptBonding(cell, 0);

            Assert.Equal(expectedBonds, bonds);
        }

        [Theory]
        [InlineData(10, 100)]
        [InlineData(60, 16)]
        public void TicksPerSecond_SetsTickDuration(int ticksPerSecond, int expectedDuration)
        {
            var config = SimulationConfig.CreateMinimal();
            config.TicksPerSecond = ticksPerSecond;

            var simulation = new IntegratedSimulationEngine(config);

            Assert.Equal(expectedDuration, config.TickDuration);
            Assert.Equal(expectedDuration, simulation.TickManager.TickDurationMs);
        }

        [Fact]
        public void MaxChains_Zero_DissolvesChains()
        {
            var config = SimulationConfig.CreateMinimal();
            config.MaxChains = 0;
            var simulation = new IntegratedSimulationEngine(config);
            var token1 = MakeToken(1001, TokenType.INTEGER_LITERAL, "5", new Vector3Int(1, 1, 0));
            var token2 = MakeToken(1002, TokenType.OPERATOR_PLUS, "+", new Vector3Int(1, 1, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token1, token2, 0));

            simulation.Step();

            Assert.Empty(simulation.BondingManager.GetActiveChains());
            Assert.Equal(0, simulation.ChainRegistry.Count);
            Assert.Empty(token1.BondedTokens);
        }

        [Fact]
        public void ChainRegistry_TracksBondingManagerChains()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ValidationFrequency = 0;
            var simulation = new IntegratedSimulationEngine(config);
            var token1 = MakeToken(1001, TokenType.INTEGER_LITERAL, "5", new Vector3Int(1, 1, 0));
            var token2 = MakeToken(1002, TokenType.OPERATOR_PLUS, "+", new Vector3Int(1, 1, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token1, token2, 0));

            simulation.Step();

            Assert.Equal(simulation.BondingManager.GetActiveChains().Count, simulation.ChainRegistry.Count);
        }

        [Theory]
        [InlineData(3, 0, true)]   // validated and fails: dissolved
        [InlineData(4, 0, false)]  // shorter than MinValidationLength: not validated
        [InlineData(3, 50, false)] // bonded too recently (StabilityThreshold): not validated
        public void Validation_DissolvesInvalidChains(int minValidationLength, int stabilityThreshold, bool expectDissolved)
        {
            var config = SimulationConfig.CreateMinimal();
            config.ValidationFrequency = 1;
            config.MinValidationLength = minValidationLength;
            config.StabilityThreshold = stabilityThreshold;
            config.MutationRate = 0;
            var simulation = new IntegratedSimulationEngine(config);

            var token1 = MakeToken(1001, TokenType.INTEGER_LITERAL, "5", new Vector3Int(1, 1, 0));
            var token2 = MakeToken(1002, TokenType.OPERATOR_PLUS, "+", new Vector3Int(1, 1, 0));
            var token3 = MakeToken(1003, TokenType.INTEGER_LITERAL, "3", new Vector3Int(1, 1, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token1, token2, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token2, token3, 0));
            var chain = simulation.BondingManager.GetTokenChain(token1);
            Assert.NotNull(chain);

            // Track the chain, then make it fail validation (an unbonded pair is invalid)
            simulation.ChainRegistry.RegisterChain(chain);
            chain.Tokens.Reverse(0, 2);

            var validationResult = chain.ValidateChain();
            Assert.False(validationResult.IsValid);

            simulation.ValidateChains(currentTick: 1);
            bool dissolved = simulation.ChainRegistry.GetChain(chain.Id) != chain;

            Assert.Equal(expectDissolved, dissolved);
        }

        [Fact]
        public void SaveAndRestore_PreservesSettings()
        {
            var config = SimulationConfig.CreateMinimal();
            config.TokenGenerationRate = 4;
            config.GravityStrength = 0.5f;
            config.BondingRange = 1.5f;
            config.DamageMultiplier = 3.0f;
            config.BaseDamageRate = 0.05f;
            config.MutationFalloff = MutationFalloff.Abrupt;
            config.MaxChains = 7;
            config.ValidationFrequency = 3;
            var simulation = new IntegratedSimulationEngine(config);

            var restored = IntegratedSimulationEngine.RestoreFromState(simulation.CaptureState());

            Assert.Equal(4, restored.Config.TokenGenerationRate);
            Assert.Equal(0.5f, restored.Config.GravityStrength);
            Assert.Equal(1.5f, restored.Config.BondingRange);
            Assert.Equal(3.0f, restored.Config.DamageMultiplier);
            Assert.Equal(0.05f, restored.Config.BaseDamageRate);
            Assert.Equal(MutationFalloff.Abrupt, restored.Config.MutationFalloff);
            Assert.Equal(7, restored.Config.MaxChains);
            Assert.Equal(3, restored.Config.ValidationFrequency);
        }
    }
}

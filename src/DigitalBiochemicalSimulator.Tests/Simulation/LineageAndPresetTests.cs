using System.Collections.Generic;
using System.Linq;
using Xunit;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Physics;
using DigitalBiochemicalSimulator.Presets;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.Tests.TestHelpers;
using BasicPresets = DigitalBiochemicalSimulator.Simulation.SimulationPresets;
using ScenarioPresets = DigitalBiochemicalSimulator.Presets.SimulationPresets;

namespace DigitalBiochemicalSimulator.Tests.Simulation
{
    /// <summary>
    /// Evolution lineage tracking, the engine fixes it depends on, and the preset catalog
    /// </summary>
    public class LineageAndPresetTests
    {
        private static Token MakeToken(long id, TokenType type, string value, Vector3Int position, int energy = 0)
        {
            return new Token(id, type, value, position) { Energy = energy, IsActive = true };
        }

        private static IntegratedSimulationEngine CreateSimulationWithChain(SimulationConfig config, out Token token1, out Token token2)
        {
            var simulation = new IntegratedSimulationEngine(config);
            token1 = MakeToken(1001, TokenType.INTEGER_LITERAL, "5", new Vector3Int(1, 1, 0));
            token2 = MakeToken(1002, TokenType.OPERATOR_PLUS, "+", new Vector3Int(1, 1, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token1, token2, 0));
            return simulation;
        }

        [Fact]
        public void Lineage_IsRecordedWhenChainForms()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ValidationFrequency = 0;
            var simulation = CreateSimulationWithChain(config, out _, out _);

            simulation.Step();

            var stats = simulation.Analytics.Evolution.GetStatistics();
            Assert.Equal(1, stats.TotalLineages);
            Assert.Equal(1, stats.ActiveLineages);
            Assert.Equal(2, stats.LongestLineage);
        }

        [Fact]
        public void Lineage_EndsWithReason_WhenChainIsRemovedByChainLimit()
        {
            var config = SimulationConfig.CreateMinimal();
            config.MaxChains = 0;
            var simulation = CreateSimulationWithChain(config, out var token1, out _);
            long chainId = simulation.BondingManager.GetTokenChain(token1)!.Id;

            simulation.Step();

            var lineage = simulation.Analytics.Evolution.GetLineage(chainId);
            Assert.NotNull(lineage);
            Assert.Equal("ChainLimit", lineage.DeathReason);
            Assert.Equal(0, simulation.Analytics.Evolution.GetStatistics().ActiveLineages);
        }

        [Fact]
        public void Lineage_EndsAsRestructured_WhenChainLosesItsBonds()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ValidationFrequency = 0;
            var simulation = CreateSimulationWithChain(config, out var token1, out var token2);
            long chainId = simulation.BondingManager.GetTokenChain(token1)!.Id;
            simulation.Step();

            simulation.BondingManager.BreakBond(token1, token2, simulation.TickManager.CurrentTick);
            simulation.Step();

            var lineage = simulation.Analytics.Evolution.GetLineage(chainId);
            Assert.NotNull(lineage);
            Assert.Equal("Restructured", lineage.DeathReason);
        }

        [Fact]
        public void BuildAST_TokensWithoutOperator_ReturnsNullInsteadOfRecursingForever()
        {
            var chain = TestChains.Build(
                MakeToken(1, TokenType.IDENTIFIER, "x", Vector3Int.Zero),
                MakeToken(2, TokenType.IDENTIFIER, "y", Vector3Int.Zero));

            Assert.Null(chain.BuildAST());
        }

        [Fact]
        public void Bonding_NeedsNoEnergy_AndReleasesEnergyPerBond()
        {
            var config = new SimulationConfig(10, 10, 10) { EnergyPerBond = 4 };
            var grid = new Grid(10, 10, 10);
            var rulesEngine = new BondRulesEngine(GrammarLibrary.GetDefaultGrammar());
            var manager = new BondingManager(rulesEngine, new BondStrengthCalculator(rulesEngine), config, grid);
            var token1 = MakeToken(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = MakeToken(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            Assert.True(manager.AttemptBond(token1, token2, 0));

            // (chain length - 1) * EnergyPerBond = 1 * 4
            Assert.Equal(4, token1.Energy + token2.Energy);
        }

        [Fact]
        public void Bonding_UsesEndAndStartSites_SoChainsStayLinear()
        {
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(10, 10, 10);
            var rulesEngine = new BondRulesEngine(GrammarLibrary.GetDefaultGrammar());
            var manager = new BondingManager(rulesEngine, new BondStrengthCalculator(rulesEngine), config, grid);
            var five = MakeToken(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var plus = MakeToken(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);
            var three = MakeToken(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);
            var seven = MakeToken(4, TokenType.INTEGER_LITERAL, "7", Vector3Int.Zero);

            Assert.True(manager.AttemptBond(five, plus, 0));    // 5.END - +.START
            Assert.True(manager.AttemptBond(plus, three, 0));   // +.END - 3.START
            Assert.False(manager.AttemptBond(plus, seven, 0));  // + has no free END site
            Assert.False(manager.AttemptBond(seven, plus, 0));  // + has no free START site

            var chain = manager.GetTokenChain(five);
            Assert.NotNull(chain);
            Assert.Equal("5 + 3", chain.ToCodeString());
        }

        [Fact]
        public void Overflow_SpreadsPilesBeyondNeighborCapacity()
        {
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(10, 10, 10, cellCapacity: 2);
            var gravity = new GravitySimulator(config, grid);
            for (int i = 0; i < 40; i++)
                grid.AddToken(MakeToken(i, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 0)));

            for (int i = 0; i < 20; i++)
                gravity.HandleOverflow();

            var counts = grid.GetAllTokens().GroupBy(t => t.Position).Select(g => g.Count()).ToList();
            Assert.Equal(40, counts.Sum());
            Assert.True(counts.Max() <= 3, $"Largest pile still holds {counts.Max()} tokens");
        }

        [Fact]
        public void BondRulesEngine_AddRule_UpdatesCachedBondResults()
        {
            var rulesEngine = new BondRulesEngine(new List<GrammarRule>());
            var a = MakeToken(1, TokenType.IDENTIFIER, "a", Vector3Int.Zero);
            var b = MakeToken(2, TokenType.IDENTIFIER, "b", Vector3Int.Zero);
            Assert.False(rulesEngine.CanBond(a, b));

            var rule = new GrammarRule("pair", "identifier pair");
            rule.Pattern.Add(new TokenPattern(TokenType.IDENTIFIER));
            rule.Pattern.Add(new TokenPattern(TokenType.IDENTIFIER));
            rulesEngine.AddRule(rule);

            Assert.True(rulesEngine.CanBond(a, b));
        }

        [Fact]
        public void Gravity_FallingTokenWithOpenSpaceBelow_DoesNotDriftSideways()
        {
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(10, 10, 10);
            var gravity = new GravitySimulator(config, grid);
            var token = MakeToken(1, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 5));
            grid.AddToken(token);

            for (int i = 0; i < 10; i++)
                gravity.ApplyGravity(new List<Token> { token });

            Assert.Equal(new Vector3Int(5, 5, 5), token.Position);
        }

        [Fact]
        public void Gravity_FallingTokenBlockedBelow_SlidesToNeighborWithOpenSpaceBelow()
        {
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(10, 10, 10, cellCapacity: 1);
            var gravity = new GravitySimulator(config, grid);
            grid.AddToken(MakeToken(1, TokenType.IDENTIFIER, "a", new Vector3Int(5, 5, 4))); // fills the cell below
            var token = MakeToken(2, TokenType.IDENTIFIER, "x", new Vector3Int(5, 5, 5));
            grid.AddToken(token);

            gravity.ApplyGravity(new List<Token> { token });

            Assert.Equal(5, token.Position.Z);
            Assert.NotEqual(new Vector3Int(5, 5, 5), token.Position);
        }

        [Fact]
        public void Motion_ChainMembersMoveWithTheirHead()
        {
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(10, 10, 10);
            var rulesEngine = new BondRulesEngine(GrammarLibrary.GetDefaultGrammar());
            var manager = new BondingManager(rulesEngine, new BondStrengthCalculator(rulesEngine), config, grid);
            var token1 = MakeToken(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(5, 5, 2), energy: 10);
            var token2 = MakeToken(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(5, 5, 2), energy: 10);
            grid.AddToken(token1);
            grid.AddToken(token2);
            Assert.True(manager.AttemptBond(token1, token2, 0));

            new MotionController(config, grid).UpdateTokenMotion(new List<Token> { token1, token2 });

            Assert.Equal(3, token1.Position.Z);
            Assert.Equal(3, token2.Position.Z);
        }

        [Fact]
        public void Vents_StopEmittingAtMaxActiveTokens_WhenRecyclingIsOff()
        {
            var config = SimulationConfig.CreateMinimal();
            config.MaxActiveTokens = 5;
            config.VentEmissionRate = 1;
            config.TokenGenerationRate = 10;
            config.RecycleIdleTokens = false;
            var simulation = new IntegratedSimulationEngine(config);

            for (int i = 0; i < 20; i++)
                simulation.Step();

            Assert.True(simulation.ActiveTokens.Count <= 5);
            Assert.Equal(5, simulation.TotalTokensGenerated);
            Assert.Equal(0, simulation.TotalTokensRecycled);
        }

        [Fact]
        public void Vents_RecycleOldestFreeTokensAtMaxActiveTokens()
        {
            var config = SimulationConfig.CreateMinimal();
            config.MaxActiveTokens = 5;
            config.VentEmissionRate = 1;
            config.RecycleIdleTokens = true;
            var simulation = new IntegratedSimulationEngine(config);

            for (int i = 0; i < 20; i++)
                simulation.Step();

            Assert.True(simulation.ActiveTokens.Count <= 5);
            Assert.True(simulation.TotalTokensGenerated > 5);
            Assert.True(simulation.TotalTokensRecycled > 0);
        }

        [Fact]
        public void Recycling_NeverRemovesBondedTokens()
        {
            var config = SimulationConfig.CreateMinimal();
            config.MaxActiveTokens = 2;
            config.VentEmissionRate = 1;
            config.ValidationFrequency = 0;
            config.RecycleIdleTokens = true;
            var simulation = new IntegratedSimulationEngine(config);
            var token1 = MakeToken(1001, TokenType.INTEGER_LITERAL, "5", new Vector3Int(1, 1, 0));
            var token2 = MakeToken(1002, TokenType.OPERATOR_PLUS, "+", new Vector3Int(1, 1, 0));
            simulation.Grid.AddToken(token1);
            simulation.Grid.AddToken(token2);
            simulation.ActiveTokens.Add(token1);
            simulation.ActiveTokens.Add(token2);
            Assert.True(simulation.BondingManager.AttemptBond(token1, token2, 0));

            for (int i = 0; i < 10; i++)
                simulation.Step();

            Assert.Contains(token1, simulation.ActiveTokens);
            Assert.Contains(token2, simulation.ActiveTokens);
            Assert.Equal(0, simulation.TotalTokensRecycled);
        }

        [Theory]
        [InlineData(5, false)]  // bonding, and not yet StabilityThreshold ticks since it formed
        [InlineData(10, true)]  // still bonding, but due: validated and dissolved
        public void Validation_ChecksChainsThatKeepBonding(long currentTick, bool expectDissolved)
        {
            var config = SimulationConfig.CreateMinimal();
            config.MinValidationLength = 3;
            config.StabilityThreshold = 10;
            var simulation = new IntegratedSimulationEngine(config);
            var token1 = MakeToken(1001, TokenType.INTEGER_LITERAL, "5", new Vector3Int(1, 1, 0));
            var token2 = MakeToken(1002, TokenType.OPERATOR_PLUS, "+", new Vector3Int(1, 1, 0));
            var token3 = MakeToken(1003, TokenType.INTEGER_LITERAL, "3", new Vector3Int(1, 1, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token1, token2, 0));
            Assert.True(simulation.BondingManager.AttemptBond(token2, token3, 0));
            var chain = simulation.BondingManager.GetTokenChain(token1)!;
            simulation.ChainRegistry.RegisterChain(chain);
            chain.Tokens.Reverse(0, 2); // an unbonded pair makes the chain invalid

            // The chain bonded on this very tick, so it never settles
            chain.LastModifiedTick = currentTick;
            simulation.ValidateChains(currentTick);

            Assert.Equal(expectDissolved, simulation.ChainRegistry.GetChain(chain.Id) != chain);
        }

        [Fact]
        public void PresetCatalog_ContainsEveryPresetOnceByName()
        {
            var names = PresetCatalog.All.Select(p => p.Name).ToList();

            Assert.Equal(5 + ScenarioPresets.GetAllPresets().Count, names.Count);
            Assert.Equal(names.Count, names.Distinct().Count());
            Assert.NotNull(PresetCatalog.Find("rapidevolution"));
            Assert.NotNull(PresetCatalog.Find("Harsh Selection"));
        }

        public static IEnumerable<object[]> AllPresetNames() => PresetCatalog.All.Select(p => new object[] { p.Name });

        [Theory]
        [MemberData(nameof(AllPresetNames))]
        public void Preset_IsValidAndRuns(string name)
        {
            var config = PresetCatalog.Find(name)!.Create();
            Assert.True(config.Validate(out var error), error);

            var simulation = new IntegratedSimulationEngine(config);
            for (int i = 0; i < 100; i++)
                simulation.Step();

            Assert.True(simulation.TotalTokensGenerated > 0);
            Assert.True(simulation.ActiveTokens.Count <= config.MaxActiveTokens);
        }

        [Theory]
        [InlineData("HighPressure")]
        [InlineData("CompetitiveSelection")]
        public void TallPresets_AreTallInAltitude(string name)
        {
            var config = ScenarioPresets.GetAllPresets()[name];

            // Altitude is the Z axis (GridDepth)
            Assert.True(config.GridDepth > config.GridHeight);
            Assert.True(config.GridDepth > config.GridWidth);
        }

        [Fact]
        public void HighPressure_HasSteeperDamageThanStandard()
        {
            var highPressure = ScenarioPresets.HighPressure();
            var standard = BasicPresets.Standard;
            var grid = new Grid(1, 1, 1);
            var rulesEngine = new BondRulesEngine(GrammarLibrary.GetDefaultGrammar());

            float RateAtTop(SimulationConfig c) => new DigitalBiochemicalSimulator.Damage.DamageSystem(c,
                new BondingManager(rulesEngine, new BondStrengthCalculator(rulesEngine), c, grid)).CalculateDamageRate(c.GridDepth - 1);

            Assert.True(RateAtTop(highPressure) > RateAtTop(standard));
        }
    }
}

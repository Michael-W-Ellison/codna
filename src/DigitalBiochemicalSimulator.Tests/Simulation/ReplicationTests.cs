using System.Linq;
using Xunit;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Tests.Simulation
{
    public class ReplicationTests
    {
        private static Token MakeToken(long id, TokenType type, string value, Vector3Int position)
        {
            return new Token(id, type, value, position) { IsActive = true };
        }

        private static (Grid grid, BondingManager manager, SimulationConfig config) CreateWorld(float errorRate = 0.0f)
        {
            var config = new SimulationConfig(10, 10, 10) { ReplicationRange = 2, ReplicationErrorRate = errorRate };
            var grid = new Grid(10, 10, 10);
            var rulesEngine = new BondRulesEngine(GrammarLibrary.GetDefaultGrammar());
            return (grid, new BondingManager(rulesEngine, new BondStrengthCalculator(rulesEngine), config, grid), config);
        }

        private static TokenChain BuildParent(Grid grid, BondingManager manager)
        {
            var five = MakeToken(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(5, 5, 0));
            var plus = MakeToken(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(5, 5, 0));
            var three = MakeToken(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(5, 5, 0));
            foreach (var t in new[] { five, plus, three }) grid.AddToken(t);
            Assert.True(manager.AttemptBond(five, plus, 0));
            Assert.True(manager.AttemptBond(plus, three, 0));
            return manager.GetTokenChain(five)!;
        }

        [Fact]
        public void TryReplicate_BuildsCopyWithSameTokenTypesFromFreeTokens()
        {
            var (grid, manager, config) = CreateWorld();
            var parent = BuildParent(grid, manager);
            var free = new[]
            {
                MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0)),
                MakeToken(11, TokenType.OPERATOR_PLUS, "+", new Vector3Int(6, 6, 0)),
                MakeToken(12, TokenType.INTEGER_LITERAL, "9", new Vector3Int(4, 5, 0))
            };
            foreach (var t in free) grid.AddToken(t);

            var copy = new ChainReplicator(manager, grid, config).TryReplicate(parent, 1);

            Assert.NotNull(copy);
            Assert.NotSame(parent, copy);
            Assert.Equal(parent.Tokens.Select(t => t.Type), copy.Tokens.Select(t => t.Type));
            Assert.All(copy.Tokens, t => Assert.Contains(t, free));
            Assert.Equal("5 + 3", parent.ToCodeString());
        }

        [Fact]
        public void TryReplicate_WithoutEnoughFreeTokens_ReturnsNullAndBondsNothing()
        {
            var (grid, manager, config) = CreateWorld();
            var parent = BuildParent(grid, manager);
            var lone = MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0));
            grid.AddToken(lone);

            Assert.Null(new ChainReplicator(manager, grid, config).TryReplicate(parent, 1));
            Assert.Empty(lone.BondedTokens);
        }

        [Fact]
        public void TryReplicate_IgnoresFreeTokensOutsideReplicationRange()
        {
            var (grid, manager, config) = CreateWorld();
            var parent = BuildParent(grid, manager);
            grid.AddToken(MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(9, 9, 9)));
            grid.AddToken(MakeToken(11, TokenType.OPERATOR_PLUS, "+", new Vector3Int(9, 9, 8)));
            grid.AddToken(MakeToken(12, TokenType.INTEGER_LITERAL, "9", new Vector3Int(9, 8, 9)));

            Assert.Null(new ChainReplicator(manager, grid, config).TryReplicate(parent, 1));
        }

        [Fact]
        public void TryReplicate_CopyThatCannotBond_IsUndone()
        {
            // Every position is a copying error, and only semicolons are free: "; ; ;" cannot bond
            var (grid, manager, config) = CreateWorld(errorRate: 1.0f);
            var parent = BuildParent(grid, manager);
            var semicolons = Enumerable.Range(10, 3)
                .Select(id => MakeToken(id, TokenType.SEMICOLON, ";", new Vector3Int(6, 5, 0)))
                .ToList();
            foreach (var t in semicolons) grid.AddToken(t);

            Assert.Null(new ChainReplicator(manager, grid, config).TryReplicate(parent, 1));
            Assert.All(semicolons, t => Assert.Empty(t.BondedTokens));
        }

        [Fact]
        public void Engine_RecordsParentAndGenerationOfCopies()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ReplicationRate = 1.0f;
            config.ReplicationErrorRate = 0.0f;
            config.ValidationFrequency = 0;
            config.StabilityThreshold = 0;
            var simulation = new IntegratedSimulationEngine(config);
            var parent = BuildParent(simulation.Grid, simulation.BondingManager);
            simulation.Step(); // registers the parent (generation 0)
            parent.PassedValidation = true;
            foreach (var t in new[]
            {
                MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0)),
                MakeToken(11, TokenType.OPERATOR_PLUS, "+", new Vector3Int(6, 5, 0)),
                MakeToken(12, TokenType.INTEGER_LITERAL, "9", new Vector3Int(6, 5, 0))
            })
            {
                simulation.Grid.AddToken(t);
            }

            simulation.ReplicateChains(simulation.TickManager.CurrentTick);
            simulation.Step(); // registers the copy

            Assert.Equal(1, simulation.TotalReplications);
            var copyLineage = simulation.Analytics.Evolution.GetTopLineages(int.MaxValue)
                .Single(l => l.ParentIds.Contains(parent.Id));
            Assert.Equal(1, copyLineage.Generation);
            var stats = simulation.Analytics.Evolution.GetStatistics();
            Assert.Equal(1, stats.TotalGenerations);
            Assert.Equal(1, stats.ReplicatedLineages);
        }

        [Fact]
        public void Engine_CopiesMadeDuringStepsSurviveAndAreRecordedAsDescendants()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ReplicationRate = 1.0f;
            config.ReplicationErrorRate = 0.0f;
            config.ValidationFrequency = 1;
            config.StabilityThreshold = 0;
            config.MinValidationLength = 3;
            config.MutationRate = 0;
            config.VentEmissionRate = 1000; // no vent tokens during the test
            var simulation = new IntegratedSimulationEngine(config);
            var parent = BuildParent(simulation.Grid, simulation.BondingManager);
            simulation.ActiveTokens.AddRange(parent.Tokens);

            // One free token per cell, so they cannot bond to each other on their own
            foreach (var t in new[]
            {
                MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0)),
                MakeToken(11, TokenType.OPERATOR_PLUS, "+", new Vector3Int(4, 5, 0)),
                MakeToken(12, TokenType.INTEGER_LITERAL, "9", new Vector3Int(5, 6, 0))
            })
            {
                simulation.Grid.AddToken(t);
                simulation.ActiveTokens.Add(t);
            }

            for (int i = 0; i < 4; i++)
                simulation.Step();

            Assert.True(simulation.TotalReplications >= 1);
            var stats = simulation.Analytics.Evolution.GetStatistics();
            Assert.Equal(1, stats.ReplicatedLineages);
            var copy = simulation.ChainRegistry.GetAllChains().Single(c => c != parent);
            Assert.Equal(parent.Tokens.Select(t => t.Type), copy.Tokens.Select(t => t.Type));
            Assert.Contains(parent.Id, simulation.Analytics.Evolution.GetLineage(copy.Id)!.ParentIds);
        }

        [Fact]
        public void Engine_DoesNotReplicateChainsThatHaveNotPassedValidation()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ReplicationRate = 1.0f;
            config.StabilityThreshold = 0;
            var simulation = new IntegratedSimulationEngine(config);
            BuildParent(simulation.Grid, simulation.BondingManager);
            simulation.Step();
            simulation.Grid.AddToken(MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0)));
            simulation.Grid.AddToken(MakeToken(11, TokenType.OPERATOR_PLUS, "+", new Vector3Int(6, 5, 0)));
            simulation.Grid.AddToken(MakeToken(12, TokenType.INTEGER_LITERAL, "9", new Vector3Int(6, 5, 0)));

            simulation.ReplicateChains(simulation.TickManager.CurrentTick);

            Assert.Equal(0, simulation.TotalReplications);
        }
    }
}

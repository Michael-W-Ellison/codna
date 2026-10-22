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
            config.ChainDecayRate = 0; // chains must not decay at random during the test
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

            simulation.ReplicateChains(simulation.TickManager.CurrentTick); // starts the copy
            simulation.Step(); // gathers and bonds the copy
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
            config.ChainDecayRate = 0; // chains must not decay at random during the test
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

        [Theory]
        [InlineData("5 + 3", 3.0)]                 // 3 tokens, incomplete, no statement kinds
        [InlineData("5 + 3 ;", 8.0)]               // 4 tokens, complete statement: x2
        [InlineData("int x = 5 ;", 20.0)]          // 5 tokens, complete, declaration + assignment: x2 x2
        [InlineData("if ( x ) { y = 1 ; }", 50.0)] // 10 tokens, complete, control + block + assignment: x2 x2.5
        public void ChainFitness_RewardsLengthAndCompleteness(string code, double expected)
        {
            var symbols = new System.Collections.Generic.Dictionary<string, TokenType>
            {
                ["+"] = TokenType.OPERATOR_PLUS, ["="] = TokenType.OPERATOR_ASSIGN, [";"] = TokenType.SEMICOLON,
                ["("] = TokenType.PAREN_OPEN, [")"] = TokenType.PAREN_CLOSE, ["{"] = TokenType.BRACE_OPEN,
                ["}"] = TokenType.BRACE_CLOSE, ["int"] = TokenType.TYPE_INT, ["if"] = TokenType.KEYWORD_IF
            };
            var tokens = code.Split(' ').Select((text, i) => MakeToken(i + 1,
                symbols.TryGetValue(text, out var type) ? type : char.IsDigit(text[0]) ? TokenType.INTEGER_LITERAL : TokenType.IDENTIFIER,
                text, Vector3Int.Zero)).ToArray();

            Assert.Equal(expected, ChainFitness.Calculate(TestHelpers.TestChains.Build(tokens)), 5);
        }

        [Theory]
        [InlineData(true, 20.0, 10.0, 0.2)]   // twice the average fitness: twice the base rate
        [InlineData(true, 5.0, 10.0, 0.05)]   // half the average: half the base rate
        [InlineData(true, 200.0, 10.0, 1.0)]  // capped at 1
        [InlineData(false, 20.0, 10.0, 0.1)]  // weighting off: the base rate
        public void ReplicationProbability_IsProportionalToRelativeFitness(bool weighted, double fitness, double average, double expected)
        {
            var config = new SimulationConfig { ReplicationRate = 0.1f, FitnessWeightedReplication = weighted };

            Assert.Equal(expected, IntegratedSimulationEngine.ReplicationProbability(config, fitness, average), 5);
        }

        [Fact]
        public void Decay_BreaksChainAndRecordsDecayed()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ChainDecayRate = 1.0f;
            config.ValidationFrequency = 0;
            var simulation = new IntegratedSimulationEngine(config);
            var parent = BuildParent(simulation.Grid, simulation.BondingManager);
            long chainId = parent.Id;
            var tokens = parent.Tokens.ToList();
            simulation.Step(); // registered, then decays in the same tick

            Assert.Equal("Decayed", simulation.Analytics.Evolution.GetLineage(chainId)?.DeathReason);
            Assert.All(tokens, t => Assert.Empty(t.BondedTokens));
            Assert.Empty(simulation.BondingManager.GetActiveChains());
        }

        [Fact]
        public void Copy_GathersTokensOverSeveralTicks_AndReservesThem()
        {
            var (grid, manager, config) = CreateWorld();
            var replicator = new ChainReplicator(manager, grid, config);
            var parent = BuildParent(grid, manager);
            var seven = MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0));
            var plus = MakeToken(11, TokenType.OPERATOR_PLUS, "+", new Vector3Int(6, 5, 0));
            grid.AddToken(seven);
            grid.AddToken(plus);

            Assert.True(replicator.StartCopy(parent));
            Assert.Empty(replicator.Advance(1, _ => true)); // still missing the second operand
            Assert.True(seven.IsReserved);
            Assert.True(plus.IsReserved);
            Assert.False(manager.AttemptBond(seven, plus, 1)); // reserved tokens do not bond

            var nine = MakeToken(12, TokenType.INTEGER_LITERAL, "9", new Vector3Int(4, 5, 0));
            grid.AddToken(nine);
            var completed = replicator.Advance(2, _ => true);

            var (copy, copyParent) = Assert.Single(completed);
            Assert.Same(parent, copyParent);
            Assert.Equal(parent.Tokens.Select(t => t.Type), copy.Tokens.Select(t => t.Type));
            Assert.All(copy.Tokens, t => Assert.False(t.IsReserved));
            Assert.Equal(0, replicator.CopiesInProgress);
        }

        [Fact]
        public void Copy_NotCompletedWithinTimeout_ReleasesItsTokens()
        {
            var (grid, manager, config) = CreateWorld();
            config.ReplicationTimeout = 2;
            var replicator = new ChainReplicator(manager, grid, config);
            var parent = BuildParent(grid, manager);
            var seven = MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0));
            grid.AddToken(seven);

            replicator.StartCopy(parent);
            for (int tick = 1; tick <= 3; tick++)
                replicator.Advance(tick, _ => true);

            Assert.Equal(0, replicator.CopiesInProgress);
            Assert.False(seven.IsReserved);
        }

        [Fact]
        public void Copy_WhoseParentIsGone_IsAbandoned()
        {
            var (grid, manager, config) = CreateWorld();
            var replicator = new ChainReplicator(manager, grid, config);
            var parent = BuildParent(grid, manager);
            var seven = MakeToken(10, TokenType.INTEGER_LITERAL, "7", new Vector3Int(6, 5, 0));
            grid.AddToken(seven);
            replicator.StartCopy(parent);

            replicator.Advance(1, _ => false);

            Assert.Equal(0, replicator.CopiesInProgress);
            Assert.False(seven.IsReserved);
        }

        [Theory]
        [InlineData(true, 20.0, 10.0, 0.0005)]   // twice the average fitness: half the base rate
        [InlineData(true, 5.0, 10.0, 0.002)]     // half the average: twice the base rate
        [InlineData(false, 20.0, 10.0, 0.001)]   // weighting off: the base rate
        public void DecayProbability_IsInverseToRelativeFitness(bool weighted, double fitness, double average, double expected)
        {
            var config = new SimulationConfig { ChainDecayRate = 0.001f, FitnessWeightedDecay = weighted };

            Assert.Equal(expected, IntegratedSimulationEngine.DecayProbability(config, fitness, average), 6);
        }

        [Fact]
        public void Engine_DoesNotReplicateChainsThatHaveNotPassedValidation()
        {
            var config = SimulationConfig.CreateMinimal();
            config.ChainDecayRate = 0; // chains must not decay at random during the test
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

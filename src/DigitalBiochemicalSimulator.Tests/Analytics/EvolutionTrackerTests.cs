using System;
using System.Linq;
using Xunit;
using DigitalBiochemicalSimulator.Analytics;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;

namespace DigitalBiochemicalSimulator.Tests.Analytics
{
    public class EvolutionTrackerTests
    {
        [Fact]
        public void Constructor_InitializesEmptyTracker()
        {
            // Arrange & Act
            var tracker = new EvolutionTracker();

            // Assert
            Assert.Equal(0, tracker.LineageCount);
            Assert.Equal(0, tracker.SnapshotCount);
        }

        [Fact]
        public void RecordChainFormation_AddsLineage()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 3);

            // Act
            tracker.RecordChainFormation(chain, tick: 100);

            // Assert
            Assert.Equal(1, tracker.LineageCount);
            Assert.Equal(1, tracker.SnapshotCount);
        }

        [Fact]
        public void RecordChainFormation_NullChain_DoesNothing()
        {
            // Arrange
            var tracker = new EvolutionTracker();

            // Act
            tracker.RecordChainFormation(null, tick: 100);

            // Assert
            Assert.Equal(0, tracker.LineageCount);
            Assert.Equal(0, tracker.SnapshotCount);
        }

        [Fact]
        public void RecordChainFormation_ShortChain_DoesNothing()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 1); // Length 1 is too short

            // Act
            tracker.RecordChainFormation(chain, tick: 100);

            // Assert
            Assert.Equal(0, tracker.LineageCount);
        }

        [Fact]
        public void RecordChainState_UpdatesSnapshot()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 3);
            tracker.RecordChainFormation(chain, tick: 100);

            // Act
            tracker.RecordChainState(chain, tick: 150);

            // Assert
            Assert.Equal(2, tracker.SnapshotCount);
        }

        [Fact]
        public void RecordChainDestruction_UpdatesLineage()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 3);
            tracker.RecordChainFormation(chain, tick: 100);

            // Act
            tracker.RecordChainDestruction(chain.Id, tick: 200, reason: "Unstable");

            // Assert
            var lineage = tracker.GetLineage(chain.Id);
            Assert.NotNull(lineage);
            Assert.Equal(200, lineage.DeathTick);
            Assert.Equal("Unstable", lineage.DeathReason);
            Assert.Equal(100, lineage.Lifespan);
        }

        [Fact]
        public void CalculateFitness_ConsidersMultipleFactors()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 5);
            chain.StabilityScore = 0.8f;
            chain.TotalEnergy = 100;
            tracker.RecordChainFormation(chain, tick: 100);

            // Act
            var fitness = tracker.CalculateFitness(chain, currentTick: 200);

            // Assert
            Assert.True(fitness > 0);
            // Fitness should include length, stability, age, pattern, and energy factors
        }

        [Fact]
        public void CalculateFitness_NullChain_ReturnsZero()
        {
            // Arrange
            var tracker = new EvolutionTracker();

            // Act
            var fitness = tracker.CalculateFitness(null, currentTick: 100);

            // Assert
            Assert.Equal(0, fitness);
        }

        [Fact]
        public void GetTopLineages_ReturnsSortedByFitness()
        {
            // Arrange
            var tracker = new EvolutionTracker();

            var chain1 = CreateTestChain(1, 3);
            chain1.StabilityScore = 0.5f;
            tracker.RecordChainFormation(chain1, tick: 100);

            var chain2 = CreateTestChain(2, 5);
            chain2.StabilityScore = 0.9f;
            tracker.RecordChainFormation(chain2, tick: 100);

            // Act
            var topLineages = tracker.GetTopLineages(10);

            // Assert
            Assert.Equal(2, topLineages.Count);
            // Should be sorted by peak stability * peak length
            Assert.True(topLineages[0].PeakStability * topLineages[0].PeakLength >=
                       topLineages[1].PeakStability * topLineages[1].PeakLength);
        }

        [Fact]
        public void GetChainHistory_ReturnsChronologicalSnapshots()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 3);

            tracker.RecordChainFormation(chain, tick: 100);
            tracker.RecordChainState(chain, tick: 150);
            tracker.RecordChainState(chain, tick: 200);

            // Act
            var history = tracker.GetChainHistory(chain.Id);

            // Assert
            Assert.Equal(3, history.Count);
            Assert.Equal(100, history[0].Tick);
            Assert.Equal(150, history[1].Tick);
            Assert.Equal(200, history[2].Tick);
        }

        [Fact]
        public void IdentifyCommonPatterns_FindsFrequentPatterns()
        {
            // Arrange
            var tracker = new EvolutionTracker();

            // Create multiple chains with same pattern
            var chain1 = CreateTestChain(1, 3);
            var chain2 = CreateTestChain(2, 3);
            var chain3 = CreateTestChain(3, 4);

            tracker.RecordChainFormation(chain1, tick: 100);
            tracker.RecordChainFormation(chain2, tick: 110);
            tracker.RecordChainFormation(chain3, tick: 120);

            // Act
            var patterns = tracker.IdentifyCommonPatterns();

            // Assert
            Assert.NotEmpty(patterns);
            // Most common pattern should appear first
            Assert.True(patterns[0].Count >= patterns.Last().Count);
        }

        [Fact]
        public void GetStatistics_ReturnsAccurateMetrics()
        {
            // Arrange
            var tracker = new EvolutionTracker();

            var chain1 = CreateTestChain(1, 3);
            tracker.RecordChainFormation(chain1, tick: 100);
            tracker.RecordChainDestruction(1, tick: 200, reason: "Unstable");

            var chain2 = CreateTestChain(2, 5);
            tracker.RecordChainFormation(chain2, tick: 150);

            // Act
            var stats = tracker.GetStatistics();

            // Assert
            Assert.Equal(2, stats.TotalLineages);
            Assert.Equal(1, stats.ActiveLineages);
            Assert.Equal(1, stats.ExtinctLineages);
            Assert.Equal(5, stats.LongestLineage);
        }

        [Fact]
        public void ExportToCSV_GeneratesValidCSV()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 3);
            tracker.RecordChainFormation(chain, tick: 100);

            // Act
            var csv = tracker.ExportToCSV();

            // Assert
            Assert.Contains("ChainId,Tick,Length,Stability,Fitness,Pattern", csv);
            Assert.Contains("1,100,3", csv);
        }

        [Fact]
        public void Clear_RemovesAllData()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var chain = CreateTestChain(1, 3);
            tracker.RecordChainFormation(chain, tick: 100);

            // Act
            tracker.Clear();

            // Assert
            Assert.Equal(0, tracker.LineageCount);
            Assert.Equal(0, tracker.SnapshotCount);
        }

        [Fact]
        public void ThreadSafety_ConcurrentRecording_NoExceptions()
        {
            // Arrange
            var tracker = new EvolutionTracker();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 100, i =>
            {
                try
                {
                    var chain = CreateTestChain(i, 3);
                    tracker.RecordChainFormation(chain, tick: i);
                    tracker.RecordChainState(chain, tick: i + 50);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
            Assert.True(tracker.LineageCount > 0);
        }

        // Helper method
        private TokenChain CreateTestChain(long id, int length)
        {
            var tokens = new System.Collections.Generic.List<Token>();
            for (int i = 0; i < length; i++)
            {
                tokens.Add(new Token(
                    id: i,
                    type: TokenType.IDENTIFIER,
                    value: $"token{i}",
                    position: new Vector3Int(i, 0, 0)
                ));
            }

            var chain = new TokenChain(tokens[0]);
            for (int i = 1; i < tokens.Count; i++)
            {
                chain.AddToken(tokens[i]);
            }

            chain.Id = id;
            chain.StabilityScore = 0.5f;
            chain.TotalEnergy = 50;

            return chain;
        }
    }
}

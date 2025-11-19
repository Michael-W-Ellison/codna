using Xunit;
using Xunit.Abstractions;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.Performance
{
    /// <summary>
    /// Performance and stress tests for the simulation.
    /// Tests large-scale scenarios, memory usage, and execution speed.
    /// </summary>
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Performance_1000Tokens_CompletesInReasonableTime()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50)
            {
                MaxTokens = 1000,
                TicksPerSecond = 60
            };

            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth, config.CellCapacity);
            var tokens = new List<Token>();

            // Generate 1000 tokens
            var random = new Random(42);
            for (int i = 0; i < 1000; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, config.GridWidth),
                    random.Next(0, config.GridHeight),
                    random.Next(0, config.GridDepth)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = random.Next(50, 150),
                    Mass = 5
                };

                tokens.Add(token);
                grid.AddToken(token);
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Simulate basic operations
            int operationCount = 0;
            for (int tick = 0; tick < 100; tick++)
            {
                // Simulate token updates
                foreach (var token in tokens.Where(t => t.IsActive))
                {
                    // Energy decay
                    token.Energy = Math.Max(0, token.Energy - 1);

                    // Simple position update
                    if (token.Energy > 0)
                    {
                        token.Velocity = new Vector3Int(0, -1, 0);
                        operationCount++;
                    }
                }
            }

            stopwatch.Stop();

            // Assert
            var avgTimePerTick = stopwatch.ElapsedMilliseconds / 100.0;
            var targetTicksPerSecond = 30; // Minimum acceptable performance
            var maxTimePerTick = 1000.0 / targetTicksPerSecond; // ~33ms for 30 TPS

            _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average time per tick: {avgTimePerTick:F2}ms");
            _output.WriteLine($"Operations: {operationCount}");
            _output.WriteLine($"Target: <{maxTimePerTick:F2}ms per tick for {targetTicksPerSecond} TPS");

            Assert.True(avgTimePerTick < maxTimePerTick * 2,
                $"Performance too slow: {avgTimePerTick:F2}ms > {maxTimePerTick * 2:F2}ms");
        }

        [Fact]
        public void Performance_LargeGrid_HandlesEfficiently()
        {
            // Arrange - 100x100x100 grid (1 million cells)
            var stopwatch = Stopwatch.StartNew();
            var grid = new Grid(100, 100, 100, capacity: 10);
            stopwatch.Stop();

            var creationTime = stopwatch.ElapsedMilliseconds;

            // Act - Add tokens across the grid
            stopwatch.Restart();
            var random = new Random(42);
            int tokenCount = 500;

            for (int i = 0; i < tokenCount; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, 100),
                    random.Next(0, 100),
                    random.Next(0, 100)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position);
                grid.AddToken(token);
            }

            stopwatch.Stop();
            var insertionTime = stopwatch.ElapsedMilliseconds;

            // Query performance
            stopwatch.Restart();
            int queryCount = 100;
            for (int i = 0; i < queryCount; i++)
            {
                var queryPos = new Vector3Int(
                    random.Next(0, 100),
                    random.Next(0, 100),
                    random.Next(0, 100)
                );
                var cell = grid.GetCell(queryPos);
            }
            stopwatch.Stop();
            var queryTime = stopwatch.ElapsedMilliseconds;

            // Assert
            _output.WriteLine($"Grid creation: {creationTime}ms");
            _output.WriteLine($"Token insertion ({tokenCount} tokens): {insertionTime}ms");
            _output.WriteLine($"Cell queries ({queryCount} queries): {queryTime}ms");
            _output.WriteLine($"Average query time: {queryTime / (double)queryCount:F3}ms");

            Assert.True(creationTime < 1000, "Grid creation too slow");
            Assert.True(insertionTime < 500, "Token insertion too slow");
            Assert.True(queryTime < 100, "Cell queries too slow");
        }

        [Fact]
        public void Performance_OctreeSpatialIndex_FastQueries()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var random = new Random(42);
            int tokenCount = 1000;

            var tokens = new List<Token>();
            for (int i = 0; i < tokenCount; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, 100),
                    random.Next(0, 100),
                    random.Next(0, 100)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position);
                tokens.Add(token);
                octree.Insert(token);
            }

            // Act - Range queries
            var stopwatch = Stopwatch.StartNew();
            int queryCount = 100;
            int totalResults = 0;

            for (int i = 0; i < queryCount; i++)
            {
                var center = new Vector3Int(
                    random.Next(0, 100),
                    random.Next(0, 100),
                    random.Next(0, 100)
                );

                var results = octree.QueryRange(center, 10.0f);
                totalResults += results.Count;
            }

            stopwatch.Stop();

            // Assert
            var avgQueryTime = stopwatch.ElapsedMilliseconds / (double)queryCount;
            _output.WriteLine($"Octree range queries ({queryCount} queries, {tokenCount} tokens):");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Average query: {avgQueryTime:F3}ms");
            _output.WriteLine($"  Total results: {totalResults}");

            Assert.True(avgQueryTime < 1.0, $"Octree queries too slow: {avgQueryTime:F3}ms > 1.0ms");
        }

        [Fact]
        public void Performance_BondingOperations_ScalesWell()
        {
            // Arrange
            var config = new SimulationConfig(30, 30, 30);
            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth);
            var tokens = new List<Token>();

            // Create clustered tokens that can bond
            for (int i = 0; i < 100; i++)
            {
                var position = new Vector3Int(15, 15, 15); // All at same position
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = 100,
                    Mass = 5
                };
                token.Metadata = new TokenMetadata("literal", "int", "operand", 0.5f, 3);
                tokens.Add(token);
                grid.AddToken(token);
            }

            var rulesEngine = new BondRulesEngine();
            var strengthCalculator = new BondStrengthCalculator();
            var bondingManager = new BondingManager(rulesEngine, strengthCalculator, config, grid);

            // Act - Attempt bonding
            var stopwatch = Stopwatch.StartNew();
            int bondsFormed = 0;

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                for (int j = i + 1; j < Math.Min(i + 10, tokens.Count); j++)
                {
                    if (bondingManager.AttemptBond(tokens[i], tokens[j], currentTick: 0))
                    {
                        bondsFormed++;
                    }
                }
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Bonding operations:");
            _output.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Bonds formed: {bondsFormed}");
            _output.WriteLine($"  Avg time per attempt: {stopwatch.ElapsedMilliseconds / 900.0:F3}ms");

            Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Bonding operations too slow");
            Assert.True(bondsFormed > 0, "Should form at least some bonds");
        }

        [Fact]
        public void Performance_LongRunningSimulation_RemainsStable()
        {
            // Arrange
            var config = new SimulationConfig(20, 20, 20)
            {
                MaxTokens = 100,
                TicksPerSecond = 60
            };

            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth);
            var tokens = new List<Token>();
            var random = new Random(42);

            // Create initial tokens
            for (int i = 0; i < 50; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, config.GridWidth),
                    random.Next(0, config.GridHeight / 2),
                    random.Next(0, config.GridDepth)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = random.Next(50, 150),
                    Mass = 5
                };

                tokens.Add(token);
                grid.AddToken(token);
            }

            // Act - Run for 10,000 ticks
            var stopwatch = Stopwatch.StartNew();
            int tickCount = 10000;
            var tickTimes = new List<long>();

            for (int tick = 0; tick < tickCount; tick++)
            {
                var tickStart = Stopwatch.GetTimestamp();

                // Simple simulation step
                foreach (var token in tokens.Where(t => t.IsActive).ToList())
                {
                    // Energy decay
                    token.Energy = Math.Max(0, token.Energy - 1);

                    if (token.Energy == 0)
                    {
                        token.IsActive = false;
                    }
                }

                // Track every 100th tick
                if (tick % 100 == 0)
                {
                    var tickEnd = Stopwatch.GetTimestamp();
                    tickTimes.Add(tickEnd - tickStart);
                }
            }

            stopwatch.Stop();

            // Assert
            var avgTime = stopwatch.ElapsedMilliseconds / (double)tickCount;
            var activeTokens = tokens.Count(t => t.IsActive);

            _output.WriteLine($"Long-running simulation ({tickCount} ticks):");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.Elapsed.TotalSeconds:F1}s)");
            _output.WriteLine($"  Average tick time: {avgTime:F3}ms");
            _output.WriteLine($"  Estimated TPS: {1000.0 / avgTime:F0}");
            _output.WriteLine($"  Active tokens remaining: {activeTokens}/{tokens.Count}");

            Assert.True(avgTime < 10, $"Average tick time too slow: {avgTime:F3}ms");
            Assert.True(stopwatch.ElapsedMilliseconds < 60000, "Total simulation time exceeded 1 minute");
        }

        [Fact]
        public void Performance_MemoryUsage_RemainsReasonable()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            var config = new SimulationConfig(50, 50, 50)
            {
                MaxTokens = 2000
            };

            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth);
            var tokens = new List<Token>();
            var random = new Random(42);

            // Act - Create 2000 tokens
            for (int i = 0; i < 2000; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, config.GridWidth),
                    random.Next(0, config.GridHeight),
                    random.Next(0, config.GridDepth)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = 100,
                    Mass = 5
                };
                token.Metadata = new TokenMetadata("literal", "int", "operand", 0.5f, 3);

                tokens.Add(token);
                grid.AddToken(token);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB

            // Assert
            _output.WriteLine($"Memory usage for 2000 tokens:");
            _output.WriteLine($"  Initial: {initialMemory / 1024.0 / 1024.0:F2} MB");
            _output.WriteLine($"  Final: {finalMemory / 1024.0 / 1024.0:F2} MB");
            _output.WriteLine($"  Used: {memoryUsed:F2} MB");
            _output.WriteLine($"  Per token: {memoryUsed * 1024.0 / 2000.0:F2} KB");

            // Memory usage should be reasonable (less than 100 MB for 2000 tokens)
            Assert.True(memoryUsed < 100, $"Memory usage too high: {memoryUsed:F2} MB");
        }

        [Fact]
        public void Performance_ChainValidation_FastForLongChains()
        {
            // Arrange - Create long chain
            var tokens = new List<Token>();
            for (int i = 0; i < 100; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), Vector3Int.Zero)
                {
                    Energy = 100
                };
                tokens.Add(token);
            }

            // Bond them together
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                tokens[i].BondedTokens.Add(tokens[i + 1]);
                tokens[i + 1].BondedTokens.Add(tokens[i]);
            }

            var chain = new TokenChain(tokens[0]);
            for (int i = 1; i < tokens.Count; i++)
            {
                chain.AddToken(tokens[i], atTail: true);
            }

            // Act - Validate chain multiple times
            var stopwatch = Stopwatch.StartNew();
            int validationCount = 100;

            for (int i = 0; i < validationCount; i++)
            {
                var result = chain.ValidateChain();
            }

            stopwatch.Stop();

            // Assert
            var avgTime = stopwatch.ElapsedMilliseconds / (double)validationCount;
            _output.WriteLine($"Chain validation (100-token chain, {validationCount} validations):");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Average: {avgTime:F3}ms");

            Assert.True(avgTime < 5, $"Chain validation too slow: {avgTime:F3}ms");
        }

        [Fact]
        public void Performance_TimeSeriesTracking_HandlesLargeDataset()
        {
            // Arrange
            var tracker = new TimeSeriesTracker(maxDataPoints: 100000);

            // Act - Record 50,000 data points
            var stopwatch = Stopwatch.StartNew();
            int dataPoints = 50000;

            for (int i = 0; i < dataPoints; i++)
            {
                tracker.Record("metric1", Math.Sin(i * 0.1));
                tracker.Record("metric2", Math.Cos(i * 0.1));
            }

            stopwatch.Stop();
            var recordTime = stopwatch.ElapsedMilliseconds;

            // Query statistics
            stopwatch.Restart();
            var stats1 = tracker.GetStatistics("metric1");
            var stats2 = tracker.GetStatistics("metric2");
            stopwatch.Stop();
            var queryTime = stopwatch.ElapsedMilliseconds;

            // Assert
            _output.WriteLine($"Time-series tracking ({dataPoints} points, 2 metrics):");
            _output.WriteLine($"  Record time: {recordTime}ms");
            _output.WriteLine($"  Query time: {queryTime}ms");
            _output.WriteLine($"  Avg record: {recordTime / (double)(dataPoints * 2):F4}ms");

            Assert.True(recordTime < 2000, $"Recording too slow: {recordTime}ms");
            Assert.True(queryTime < 500, $"Querying too slow: {queryTime}ms");
        }

        [Fact]
        public void Performance_SaveLoad_HandlesLargeState()
        {
            // Arrange
            var config = new SimulationConfig(30, 30, 30);
            var tokens = new List<Token>();
            var random = new Random(42);

            for (int i = 0; i < 500; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, 30),
                    random.Next(0, 30),
                    random.Next(0, 30)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = random.Next(50, 150)
                };
                tokens.Add(token);
            }

            var state = new SimulationStateBuilder()
                .WithMetadata(1000, "Performance test")
                .WithConfiguration(config)
                .WithTokens(tokens)
                .WithGrid(new Grid(30, 30, 30))
                .Build();

            var tempFile = System.IO.Path.GetTempFileName();
            var manager = new SaveLoadManager(System.IO.Path.GetTempPath());

            // Act - Save
            var saveStopwatch = Stopwatch.StartNew();
            var saveResult = manager.Save(state, tempFile);
            saveStopwatch.Stop();

            // Act - Load
            var loadStopwatch = Stopwatch.StartNew();
            var loadResult = manager.Load(tempFile);
            loadStopwatch.Stop();

            // Assert
            _output.WriteLine($"Save/Load performance (500 tokens):");
            _output.WriteLine($"  Save time: {saveStopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Load time: {loadStopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  File size: {saveResult.FileSizeFormatted}");

            Assert.True(saveStopwatch.ElapsedMilliseconds < 1000, "Save too slow");
            Assert.True(loadStopwatch.ElapsedMilliseconds < 1000, "Load too slow");
            Assert.True(loadResult.Success, "Load failed");
            Assert.Equal(500, loadResult.State.Tokens.Count);

            // Cleanup
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }
}

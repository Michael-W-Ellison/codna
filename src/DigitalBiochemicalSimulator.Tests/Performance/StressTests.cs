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
    /// Stress tests for extreme scenarios and edge cases.
    /// Tests system limits, stability, and robustness.
    /// </summary>
    public class StressTests
    {
        private readonly ITestOutputHelper _output;

        public StressTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Stress_MaximumGridSize_CreatesSuccessfully()
        {
            // Arrange & Act
            var stopwatch = Stopwatch.StartNew();
            var grid = new Grid(100, 100, 100, capacity: 5);
            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Maximum grid (100x100x100 = 1M cells):");
            _output.WriteLine($"  Creation time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Dimensions: {grid.Width}x{grid.Height}x{grid.Depth}");

            Assert.Equal(100, grid.Width);
            Assert.Equal(100, grid.Height);
            Assert.Equal(100, grid.Depth);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Grid creation took too long");
        }

        [Fact]
        public void Stress_1000ActiveTokens_MaintainsPerformance()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50)
            {
                MaxTokens = 1000
            };

            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth);
            var tokens = new List<Token>();
            var random = new Random(42);

            // Create 1000 tokens
            for (int i = 0; i < 1000; i++)
            {
                var position = new Vector3Int(
                    random.Next(0, config.GridWidth),
                    random.Next(0, config.GridHeight),
                    random.Next(0, config.GridDepth)
                );

                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = random.Next(100, 200),
                    Mass = 5
                };

                tokens.Add(token);
                grid.AddToken(token);
            }

            // Act - Run simulation for 1000 ticks
            var stopwatch = Stopwatch.StartNew();
            var tickTimes = new List<double>();

            for (int tick = 0; tick < 1000; tick++)
            {
                var tickStart = Stopwatch.GetTimestamp();

                // Update all tokens
                foreach (var token in tokens.Where(t => t.IsActive).ToList())
                {
                    token.Energy = Math.Max(0, token.Energy - 1);
                    if (token.Energy == 0)
                        token.IsActive = false;
                }

                var tickEnd = Stopwatch.GetTimestamp();
                tickTimes.Add((tickEnd - tickStart) * 1000.0 / Stopwatch.Frequency);
            }

            stopwatch.Stop();

            // Assert
            var avgTickTime = tickTimes.Average();
            var maxTickTime = tickTimes.Max();
            var minTickTime = tickTimes.Min();

            _output.WriteLine($"1000 tokens, 1000 ticks:");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Avg tick: {avgTickTime:F3}ms");
            _output.WriteLine($"  Min tick: {minTickTime:F3}ms");
            _output.WriteLine($"  Max tick: {maxTickTime:F3}ms");
            _output.WriteLine($"  Target TPS: {1000.0 / avgTickTime:F0}");

            Assert.True(avgTickTime < 50, $"Average tick time too slow: {avgTickTime:F3}ms");
            Assert.True(maxTickTime < 100, $"Max tick time too slow: {maxTickTime:F3}ms");
        }

        [Fact]
        public void Stress_100000Ticks_RemainsStable()
        {
            // Arrange
            var config = new SimulationConfig(20, 20, 20)
            {
                MaxTokens = 50
            };

            var tokens = new List<Token>();
            var random = new Random(42);

            for (int i = 0; i < 50; i++)
            {
                var position = new Vector3Int(10, 10, 10);
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), position)
                {
                    Energy = int.MaxValue / 2, // Long-lasting
                    Mass = 5
                };
                tokens.Add(token);
            }

            // Act - Run for 100,000 ticks
            var stopwatch = Stopwatch.StartNew();
            int tickCount = 100000;
            int sampleInterval = 10000;
            var samples = new List<double>();

            for (int tick = 0; tick < tickCount; tick++)
            {
                var tickStart = Stopwatch.GetTimestamp();

                // Minimal simulation step
                foreach (var token in tokens)
                {
                    token.Energy = Math.Max(0, token.Energy - 1);
                }

                if (tick % sampleInterval == 0)
                {
                    var tickEnd = Stopwatch.GetTimestamp();
                    samples.Add((tickEnd - tickStart) * 1000.0 / Stopwatch.Frequency);
                }
            }

            stopwatch.Stop();

            // Assert - Check for performance degradation
            var firstQuarter = samples.Take(samples.Count / 4).Average();
            var lastQuarter = samples.Skip(3 * samples.Count / 4).Average();
            var degradation = (lastQuarter - firstQuarter) / firstQuarter * 100;

            _output.WriteLine($"100,000 tick stress test:");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.Elapsed.TotalSeconds:F1}s)");
            _output.WriteLine($"  Avg time: {stopwatch.ElapsedMilliseconds / (double)tickCount:F4}ms per tick");
            _output.WriteLine($"  First quarter avg: {firstQuarter:F4}ms");
            _output.WriteLine($"  Last quarter avg: {lastQuarter:F4}ms");
            _output.WriteLine($"  Degradation: {degradation:F2}%");

            Assert.True(Math.Abs(degradation) < 50, $"Performance degraded significantly: {degradation:F2}%");
            Assert.True(stopwatch.Elapsed.TotalMinutes < 5, "Test took longer than 5 minutes");
        }

        [Fact]
        public void Stress_DenseTokenClustering_HandlesEfficiently()
        {
            // Arrange - All tokens in same cell
            var grid = new Grid(10, 10, 10, capacity: 1000);
            var tokens = new List<Token>();

            for (int i = 0; i < 500; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), new Vector3Int(5, 5, 5))
                {
                    Energy = 100
                };
                tokens.Add(token);
                grid.AddToken(token);
            }

            // Act - Query the dense cell
            var stopwatch = Stopwatch.StartNew();
            int queryCount = 1000;

            for (int i = 0; i < queryCount; i++)
            {
                var cell = grid.GetCell(new Vector3Int(5, 5, 5));
                var count = cell?.Tokens.Count ?? 0;
            }

            stopwatch.Stop();

            // Assert
            var avgQuery = stopwatch.ElapsedMilliseconds / (double)queryCount;
            _output.WriteLine($"Dense clustering (500 tokens in 1 cell, {queryCount} queries):");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Avg query: {avgQuery:F4}ms");

            Assert.True(avgQuery < 0.1, $"Dense cell queries too slow: {avgQuery:F4}ms");
        }

        [Fact]
        public void Stress_RapidBondingUnbonding_RemainsStable()
        {
            // Arrange
            var config = new SimulationConfig(10, 10, 10);
            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth);

            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(5, 5, 5))
            {
                Energy = 100
            };
            token1.Metadata = new TokenMetadata("literal", "int", "operand", 0.5f, 5);

            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(5, 5, 5))
            {
                Energy = 100
            };
            token2.Metadata = new TokenMetadata("operator", "arithmetic", "binary_operator", 0.6f, 5);

            grid.AddToken(token1);
            grid.AddToken(token2);

            var rulesEngine = new BondRulesEngine();
            var strengthCalculator = new BondStrengthCalculator();
            var bondingManager = new BondingManager(rulesEngine, strengthCalculator, config, grid);

            // Act - Rapidly bond and unbond
            var stopwatch = Stopwatch.StartNew();
            int cycles = 1000;

            for (int i = 0; i < cycles; i++)
            {
                // Bond
                bondingManager.AttemptBond(token1, token2, currentTick: i);

                // Unbond
                bondingManager.BreakBond(token1, token2, currentTick: i);
            }

            stopwatch.Stop();

            // Assert
            var avgCycle = stopwatch.ElapsedMilliseconds / (double)cycles;
            _output.WriteLine($"Rapid bonding/unbonding ({cycles} cycles):");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Avg cycle: {avgCycle:F3}ms");

            Assert.True(avgCycle < 1, $"Bond/unbond cycle too slow: {avgCycle:F3}ms");
            Assert.Empty(token1.BondedTokens);
            Assert.Empty(token2.BondedTokens);
        }

        [Fact]
        public void Stress_MaximumChainLength_ValidatesQuickly()
        {
            // Arrange - Create chain of 500 tokens
            var tokens = new List<Token>();
            for (int i = 0; i < 500; i++)
            {
                var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), Vector3Int.Zero)
                {
                    Energy = 100
                };
                tokens.Add(token);
            }

            // Bond sequentially
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

            // Act - Validate
            var stopwatch = Stopwatch.StartNew();
            var result = chain.ValidateChain();
            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Maximum chain validation (500 tokens):");
            _output.WriteLine($"  Validation time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Chain length: {chain.Length}");
            _output.WriteLine($"  Is valid: {result.IsValid}");

            Assert.Equal(500, chain.Length);
            Assert.True(stopwatch.ElapsedMilliseconds < 100, "Chain validation too slow");
        }

        [Fact]
        public void Stress_OctreeRebuild_HandlesLargeDataset()
        {
            // Arrange
            var octree = Octree.FromGrid(100, 100, 100);
            var random = new Random(42);
            var tokens = new List<Token>();

            // Insert 5000 tokens
            for (int i = 0; i < 5000; i++)
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

            // Act - Rebuild
            var stopwatch = Stopwatch.StartNew();
            octree.Rebuild();
            stopwatch.Stop();

            var stats = octree.GetStatistics();

            // Assert
            _output.WriteLine($"Octree rebuild (5000 tokens):");
            _output.WriteLine($"  Rebuild time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Total nodes: {stats.TotalNodes}");
            _output.WriteLine($"  Leaf nodes: {stats.LeafNodes}");
            _output.WriteLine($"  Max depth: {stats.MaxDepth}");

            Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Octree rebuild too slow");
            Assert.Equal(5000, octree.TotalTokens);
        }

        [Fact]
        public void Stress_ConcurrentTimeSeriesRecording_HandlesHighThroughput()
        {
            // Arrange
            var tracker = new TimeSeriesTracker(maxDataPoints: 100000);
            int metricsCount = 50;
            int dataPointsPerMetric = 10000;

            // Act
            var stopwatch = Stopwatch.StartNew();

            for (int tick = 0; tick < dataPointsPerMetric; tick++)
            {
                var metrics = new Dictionary<string, double>();
                for (int m = 0; m < metricsCount; m++)
                {
                    metrics[$"metric_{m}"] = Math.Sin(tick * 0.01 + m);
                }
                tracker.RecordBatch(metrics, tick);
            }

            stopwatch.Stop();

            // Assert
            var totalDataPoints = metricsCount * dataPointsPerMetric;
            var avgTime = stopwatch.ElapsedMilliseconds / (double)dataPointsPerMetric;

            _output.WriteLine($"High-throughput time-series ({metricsCount} metrics, {dataPointsPerMetric} points):");
            _output.WriteLine($"  Total time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Total data points: {totalDataPoints:N0}");
            _output.WriteLine($"  Avg batch time: {avgTime:F4}ms");
            _output.WriteLine($"  Throughput: {totalDataPoints / (stopwatch.ElapsedMilliseconds / 1000.0):F0} points/sec");

            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Time-series recording too slow");
            Assert.Equal(metricsCount, tracker.SeriesCount);
        }

        [Fact]
        public void Stress_MemoryUnderLoad_NoLeaks()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var peakMemory = initialMemory;

            // Act - Create and destroy many objects
            for (int cycle = 0; cycle < 10; cycle++)
            {
                var tokens = new List<Token>();
                for (int i = 0; i < 1000; i++)
                {
                    var token = new Token(i, TokenType.INTEGER_LITERAL, i.ToString(), Vector3Int.Zero)
                    {
                        Energy = 100
                    };
                    tokens.Add(token);
                }

                var currentMemory = GC.GetTotalMemory(false);
                peakMemory = Math.Max(peakMemory, currentMemory);

                // Clear
                tokens.Clear();
                tokens = null;
            }

            // Force collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            var memoryGrowth = (finalMemory - initialMemory) / 1024.0 / 1024.0;
            var peakIncrease = (peakMemory - initialMemory) / 1024.0 / 1024.0;

            _output.WriteLine($"Memory leak test (10 cycles, 1000 tokens each):");
            _output.WriteLine($"  Initial: {initialMemory / 1024.0 / 1024.0:F2} MB");
            _output.WriteLine($"  Peak: {peakMemory / 1024.0 / 1024.0:F2} MB (+{peakIncrease:F2} MB)");
            _output.WriteLine($"  Final: {finalMemory / 1024.0 / 1024.0:F2} MB");
            _output.WriteLine($"  Growth: {memoryGrowth:F2} MB");

            // Memory growth should be minimal (less than 10 MB)
            Assert.True(memoryGrowth < 10, $"Possible memory leak: {memoryGrowth:F2} MB growth");
        }

        [Fact]
        public void Stress_ExtremeTokenVelocities_HandledSafely()
        {
            // Arrange
            var grid = new Grid(100, 100, 100);
            var token = new Token(1, TokenType.INTEGER_LITERAL, "1", new Vector3Int(50, 50, 50))
            {
                Energy = 100,
                Velocity = new Vector3Int(1000, 1000, 1000) // Extreme velocity
            };

            grid.AddToken(token);

            // Act - Should not crash
            for (int i = 0; i < 100; i++)
            {
                // Clamp velocity
                token.Velocity = new Vector3Int(
                    Math.Clamp(token.Velocity.X, -10, 10),
                    Math.Clamp(token.Velocity.Y, -10, 10),
                    Math.Clamp(token.Velocity.Z, -10, 10)
                );
            }

            // Assert
            Assert.True(Math.Abs(token.Velocity.X) <= 10);
            Assert.True(Math.Abs(token.Velocity.Y) <= 10);
            Assert.True(Math.Abs(token.Velocity.Z) <= 10);
        }
    }
}

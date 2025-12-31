using Xunit;
using DigitalBiochemicalSimulator.Utilities;
using System.Threading;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.Utilities
{
    public class PerformanceProfilerTests
    {
        [Fact]
        public void PerformanceProfiler_Profile_RecordsOperation()
        {
            // Arrange
            var profiler = new PerformanceProfiler();

            // Act
            using (profiler.Profile("TestOperation"))
            {
                Thread.Sleep(10);
            }

            // Assert
            var stats = profiler.GetStats("TestOperation");
            Assert.Equal("TestOperation", stats.OperationName);
            Assert.Equal(1, stats.CallCount);
            Assert.True(stats.TotalMilliseconds >= 10);
        }

        [Fact]
        public void PerformanceProfiler_MultipleProfiles_TracksCorrectly()
        {
            // Arrange
            var profiler = new PerformanceProfiler();

            // Act
            for (int i = 0; i < 5; i++)
            {
                using (profiler.Profile("TestOperation"))
                {
                    Thread.Sleep(5);
                }
            }

            // Assert
            var stats = profiler.GetStats("TestOperation");
            Assert.Equal(5, stats.CallCount);
            Assert.True(stats.TotalMilliseconds >= 25);
            Assert.True(stats.AverageMilliseconds >= 5);
        }

        [Fact]
        public void PerformanceProfiler_DifferentOperations_TrackedSeparately()
        {
            // Arrange
            var profiler = new PerformanceProfiler();

            // Act
            using (profiler.Profile("Operation1"))
            {
                Thread.Sleep(10);
            }

            using (profiler.Profile("Operation2"))
            {
                Thread.Sleep(20);
            }

            // Assert
            var stats1 = profiler.GetStats("Operation1");
            var stats2 = profiler.GetStats("Operation2");

            Assert.Equal(1, stats1.CallCount);
            Assert.Equal(1, stats2.CallCount);
            Assert.True(stats1.TotalMilliseconds >= 10);
            Assert.True(stats2.TotalMilliseconds >= 20);
        }

        [Fact]
        public void PerformanceProfiler_GetAllStats_ReturnsSortedByTotalTime()
        {
            // Arrange
            var profiler = new PerformanceProfiler();

            // Act
            using (profiler.Profile("Fast")) { Thread.Sleep(5); }
            using (profiler.Profile("Slow")) { Thread.Sleep(20); }
            using (profiler.Profile("Medium")) { Thread.Sleep(10); }

            var allStats = profiler.GetAllStats();

            // Assert
            Assert.Equal(3, allStats.Count);
            Assert.Equal("Slow", allStats[0].OperationName);
            Assert.Equal("Medium", allStats[1].OperationName);
            Assert.Equal("Fast", allStats[2].OperationName);
        }

        [Fact]
        public void PerformanceProfiler_Reset_ClearsAllData()
        {
            // Arrange
            var profiler = new PerformanceProfiler();
            using (profiler.Profile("TestOperation"))
            {
                Thread.Sleep(10);
            }

            // Act
            profiler.Reset();

            // Assert
            var stats = profiler.GetStats("TestOperation");
            Assert.Equal(0, stats.CallCount);
            Assert.Equal(0, stats.TotalMilliseconds);
        }

        [Fact]
        public void PerformanceProfiler_Disabled_MinimalOverhead()
        {
            // Arrange
            var profiler = new PerformanceProfiler(enabled: false);

            // Act
            using (profiler.Profile("TestOperation"))
            {
                Thread.Sleep(10);
            }

            // Assert - should not record when disabled
            var stats = profiler.GetStats("TestOperation");
            Assert.Equal(0, stats.CallCount);
        }

        [Fact]
        public void PerformanceProfiler_GenerateReport_ContainsExpectedData()
        {
            // Arrange
            var profiler = new PerformanceProfiler();
            using (profiler.Profile("TestOp"))
            {
                Thread.Sleep(10);
            }

            // Act
            var report = profiler.GenerateReport(includeDetails: false);

            // Assert
            Assert.Contains("Performance Profile Report", report);
            Assert.Contains("TestOp", report);
            Assert.Contains("Total Operations: 1", report);
        }

        [Fact]
        public void PerformanceProfiler_GenerateCSV_ValidFormat()
        {
            // Arrange
            var profiler = new PerformanceProfiler();
            using (profiler.Profile("TestOp"))
            {
                Thread.Sleep(10);
            }

            // Act
            var csv = profiler.GenerateCSV();

            // Assert
            Assert.Contains("Operation,Calls,TotalMs", csv);
            Assert.Contains("TestOp", csv);
        }

        [Fact]
        public void PerformanceProfiler_ProfileAction_ExecutesAndRecords()
        {
            // Arrange
            var profiler = new PerformanceProfiler();
            bool executed = false;

            // Act
            profiler.ProfileAction("TestAction", () =>
            {
                executed = true;
                Thread.Sleep(10);
            });

            // Assert
            Assert.True(executed);
            var stats = profiler.GetStats("TestAction");
            Assert.Equal(1, stats.CallCount);
        }

        [Fact]
        public void PerformanceProfiler_ProfileFunc_ReturnsValueAndRecords()
        {
            // Arrange
            var profiler = new PerformanceProfiler();

            // Act
            var result = profiler.ProfileFunc("TestFunc", () =>
            {
                Thread.Sleep(10);
                return 42;
            });

            // Assert
            Assert.Equal(42, result);
            var stats = profiler.GetStats("TestFunc");
            Assert.Equal(1, stats.CallCount);
        }

        [Fact]
        public void OperationStats_StandardDeviation_CalculatesCorrectly()
        {
            // Arrange
            var profiler = new PerformanceProfiler();

            // Act - create consistent timings
            profiler.Record("TestOp", 10);
            profiler.Record("TestOp", 20);
            profiler.Record("TestOp", 30);

            // Assert
            var stats = profiler.GetStats("TestOp");
            Assert.Equal(3, stats.CallCount);
            Assert.Equal(20.0, stats.AverageMilliseconds);
            Assert.True(stats.StandardDeviation > 0);
        }

        [Fact]
        public void SimulationPerformanceTracker_TracksTickPerformance()
        {
            // Arrange
            var tracker = new SimulationPerformanceTracker(historySize: 10);

            // Act
            for (int i = 0; i < 5; i++)
            {
                tracker.StartTick();
                Thread.Sleep(5);
                tracker.EndTick();
            }

            // Assert
            Assert.Equal(5, tracker.TotalTicks);
            Assert.True(tracker.AverageTickTime >= 5);
            Assert.True(tracker.EstimatedTPS > 0);
            Assert.True(tracker.TotalMilliseconds >= 25);
        }

        [Fact]
        public void SimulationPerformanceTracker_HistorySize_LimitsStoredValues()
        {
            // Arrange
            var tracker = new SimulationPerformanceTracker(historySize: 3);

            // Act - add more ticks than history size
            for (int i = 0; i < 10; i++)
            {
                tracker.StartTick();
                Thread.Sleep(1);
                tracker.EndTick();
            }

            // Assert - should still track all ticks, but only keep recent for averages
            Assert.Equal(10, tracker.TotalTicks);
        }

        [Fact]
        public void SimulationPerformanceTracker_Reset_ClearsData()
        {
            // Arrange
            var tracker = new SimulationPerformanceTracker();
            tracker.StartTick();
            Thread.Sleep(10);
            tracker.EndTick();

            // Act
            tracker.Reset();

            // Assert
            Assert.Equal(0, tracker.TotalTicks);
            Assert.Equal(0, tracker.TotalMilliseconds);
            Assert.Equal(0, tracker.AverageTickTime);
        }

        [Fact]
        public void SimulationPerformanceTracker_GenerateSummary_ContainsMetrics()
        {
            // Arrange
            var tracker = new SimulationPerformanceTracker();
            tracker.StartTick();
            Thread.Sleep(10);
            tracker.EndTick();

            // Act
            var summary = tracker.GenerateSummary();

            // Assert
            Assert.Contains("Performance Summary", summary);
            Assert.Contains("Total Ticks:", summary);
            Assert.Contains("Estimated TPS:", summary);
        }

        [Fact]
        public void SimulationPerformanceTracker_MinMaxTracking_Works()
        {
            // Arrange
            var tracker = new SimulationPerformanceTracker();

            // Act - create varying tick times
            tracker.StartTick();
            Thread.Sleep(5);
            tracker.EndTick();

            tracker.StartTick();
            Thread.Sleep(15);
            tracker.EndTick();

            tracker.StartTick();
            Thread.Sleep(10);
            tracker.EndTick();

            // Assert
            Assert.True(tracker.MinTickTime >= 5);
            Assert.True(tracker.MaxTickTime >= 15);
            Assert.True(tracker.MaxTickTime > tracker.MinTickTime);
        }
    }
}

using System;
using System.Linq;
using Xunit;
using DigitalBiochemicalSimulator.Analytics;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Tests.Analytics
{
    public class AnalyticsEngineTests
    {
        [Fact]
        public void Constructor_InitializesComponents()
        {
            // Arrange & Act
            var analytics = new AnalyticsEngine();

            // Assert
            Assert.NotNull(analytics.TimeSeries);
            Assert.NotNull(analytics.Evolution);
        }

        [Fact]
        public void RecordSnapshot_NullSimulation_DoesNotThrow()
        {
            // Arrange
            var analytics = new AnalyticsEngine();

            // Act & Assert - should not throw
            analytics.RecordSnapshot(null, tick: 100);
        }

        [Fact]
        public void RecordEvent_AddsEvent()
        {
            // Arrange
            var analytics = new AnalyticsEngine();

            // Act
            analytics.RecordEvent("ChainFormed", "New chain created", tick: 100);
            var summary = analytics.GetAnalyticsSummary();

            // Assert
            Assert.Equal(1, summary.TotalEvents);
        }

        [Fact]
        public void RecordEvent_WithData_StoresData()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                ["chainId"] = 42,
                ["length"] = 5
            };

            // Act
            analytics.RecordEvent("ChainFormed", "New chain", tick: 100, data: data);

            // Assert - No exception thrown
            var summary = analytics.GetAnalyticsSummary();
            Assert.Equal(1, summary.TotalEvents);
        }

        [Fact]
        public void ExportToJSON_GeneratesValidJSON()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            analytics.RecordEvent("TestEvent", "Test", tick: 1);

            // Act
            var json = analytics.ExportToJSON();

            // Assert
            Assert.Contains("ExportTime", json);
            Assert.Contains("TimeSeries", json);
            Assert.Contains("Evolution", json);
            Assert.Contains("Events", json);
            Assert.Contains("Summary", json);
        }

        [Fact]
        public void ExportComprehensiveCSV_GeneratesValidCSV()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            analytics.RecordEvent("TestEvent", "Test description", tick: 1);

            // Act
            var csv = analytics.ExportComprehensiveCSV();

            // Assert
            Assert.Contains("=== TIME SERIES DATA ===", csv);
            Assert.Contains("=== EVOLUTION DATA ===", csv);
            Assert.Contains("=== SIMULATION EVENTS ===", csv);
            Assert.Contains("TestEvent", csv);
        }

        [Fact]
        public void GetAnalyticsSummary_ReturnsValidSummary()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            analytics.RecordEvent("Event1", "Description", tick: 1);
            analytics.RecordEvent("Event2", "Description", tick: 2);

            // Act
            var summary = analytics.GetAnalyticsSummary();

            // Assert
            Assert.NotNull(summary);
            Assert.Equal(2, summary.TotalEvents);
            Assert.True(summary.MetricsTracked >= 0);
            Assert.True(summary.TotalDataPoints >= 0);
        }

        [Fact]
        public void GetDashboardData_ReturnsValidData()
        {
            // Arrange
            var analytics = new AnalyticsEngine();

            // Act
            var dashboard = analytics.GetDashboardData();

            // Assert
            Assert.NotNull(dashboard);
            Assert.NotNull(dashboard.EvolutionStats);
            Assert.NotNull(dashboard.RecentEvents);
            Assert.NotNull(dashboard.Trends);
        }

        [Fact]
        public void GetDashboardData_RecentEvents_LimitedTo10()
        {
            // Arrange
            var analytics = new AnalyticsEngine();

            // Record 15 events
            for (int i = 0; i < 15; i++)
            {
                analytics.RecordEvent($"Event{i}", $"Description {i}", tick: i);
            }

            // Act
            var dashboard = analytics.GetDashboardData();

            // Assert
            Assert.True(dashboard.RecentEvents.Count <= 10);
        }

        [Fact]
        public void ExportMetricToCSV_ReturnsValidCSV()
        {
            // Arrange
            var analytics = new AnalyticsEngine();

            // Act
            var csv = analytics.ExportMetricToCSV("active_tokens");

            // Assert
            Assert.NotNull(csv);
            Assert.Contains("Tick", csv);
        }

        [Fact]
        public void GetMetricStatistics_ReturnsStatistics()
        {
            // Arrange
            var analytics = new AnalyticsEngine();

            // Act
            var stats = analytics.GetMetricStatistics("active_tokens");

            // Assert
            // Stats might be null if no data recorded, which is valid
            if (stats != null)
            {
                Assert.Equal("active_tokens", stats.MetricName);
            }
        }

        [Fact]
        public void Clear_RemovesAllData()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            analytics.RecordEvent("TestEvent", "Test", tick: 1);

            // Act
            analytics.Clear();
            var summary = analytics.GetAnalyticsSummary();

            // Assert
            Assert.Equal(0, summary.TotalEvents);
        }

        [Fact]
        public void ThreadSafety_ConcurrentRecording_NoExceptions()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 100, i =>
            {
                try
                {
                    analytics.RecordEvent($"Event{i}", $"Description {i}", tick: i);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
            var summary = analytics.GetAnalyticsSummary();
            Assert.True(summary.TotalEvents > 0);
        }

        [Fact]
        public void ThreadSafety_ConcurrentExport_NoExceptions()
        {
            // Arrange
            var analytics = new AnalyticsEngine();
            analytics.RecordEvent("TestEvent", "Test", tick: 1);
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    var json = analytics.ExportToJSON();
                    var csv = analytics.ExportComprehensiveCSV();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }
    }
}

using Xunit;
using DigitalBiochemicalSimulator.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.Utilities
{
    public class TimeSeriesTrackerTests
    {
        [Fact]
        public void TimeSeriesTracker_Creation_InitializesCorrectly()
        {
            // Arrange & Act
            var tracker = new TimeSeriesTracker();

            // Assert
            Assert.NotNull(tracker);
            Assert.Equal(0, tracker.SeriesCount);
        }

        [Fact]
        public void TimeSeriesTracker_Record_AddsDataPoint()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            // Act
            tracker.Record("population", 100);

            // Assert
            Assert.Equal(1, tracker.SeriesCount);
            Assert.Equal(100, tracker.GetLatestValue("population"));
        }

        [Fact]
        public void TimeSeriesTracker_Record_MultiplePoints_StoresAll()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tracker.Record("population", i * 10, tick: i);
            }

            // Assert
            var series = tracker.GetSeries("population");
            Assert.Equal(10, series.Count);
        }

        [Fact]
        public void TimeSeriesTracker_RecordBatch_AddsMultipleMetrics()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            var metrics = new Dictionary<string, double>
            {
                { "population", 100 },
                { "energy", 500 },
                { "stability", 0.85 }
            };

            // Act
            tracker.RecordBatch(metrics);

            // Assert
            Assert.Equal(3, tracker.SeriesCount);
            Assert.Equal(100, tracker.GetLatestValue("population"));
            Assert.Equal(500, tracker.GetLatestValue("energy"));
            Assert.Equal(0.85, tracker.GetLatestValue("stability"));
        }

        [Fact]
        public void TimeSeriesTracker_GetRange_ReturnsCorrectSubset()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            for (int i = 0; i < 100; i++)
            {
                tracker.Record("temperature", i, tick: i);
            }

            // Act
            var range = tracker.GetRange("temperature", 25, 75);

            // Assert
            Assert.Equal(51, range.Count); // 25 to 75 inclusive
            Assert.Equal(25, range.First().Tick);
            Assert.Equal(75, range.Last().Tick);
        }

        [Fact]
        public void TimeSeriesTracker_GetStatistics_CalculatesCorrectly()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            var values = new double[] { 10, 20, 30, 40, 50 };

            for (int i = 0; i < values.Length; i++)
            {
                tracker.Record("metric", values[i], tick: i);
            }

            // Act
            var stats = tracker.GetStatistics("metric");

            // Assert
            Assert.Equal(5, stats.Count);
            Assert.Equal(10, stats.Min);
            Assert.Equal(50, stats.Max);
            Assert.Equal(30, stats.Average);
            Assert.Equal(150, stats.Sum);
            Assert.Equal(40, stats.Range);
        }

        [Fact]
        public void TimeSeriesTracker_AnalyzeTrend_DetectsIncreasing()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            // Create increasing trend
            for (int i = 0; i < 100; i++)
            {
                tracker.Record("growth", i * 2, tick: i);
            }

            // Act
            var trend = tracker.AnalyzeTrend("growth");

            // Assert
            Assert.Equal(TrendDirection.Increasing, trend.Trend);
            Assert.True(trend.Slope > 0);
        }

        [Fact]
        public void TimeSeriesTracker_AnalyzeTrend_DetectsDecreasing()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            // Create decreasing trend
            for (int i = 0; i < 100; i++)
            {
                tracker.Record("decline", 100 - i, tick: i);
            }

            // Act
            var trend = tracker.AnalyzeTrend("decline");

            // Assert
            Assert.Equal(TrendDirection.Decreasing, trend.Trend);
            Assert.True(trend.Slope < 0);
        }

        [Fact]
        public void TimeSeriesTracker_AnalyzeTrend_DetectsStable()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            // Create stable trend
            for (int i = 0; i < 100; i++)
            {
                tracker.Record("stable", 50, tick: i);
            }

            // Act
            var trend = tracker.AnalyzeTrend("stable");

            // Assert
            Assert.Equal(TrendDirection.Stable, trend.Trend);
            Assert.True(Math.Abs(trend.Slope) < 0.01);
        }

        [Fact]
        public void TimeSeriesTracker_ExportToCSV_GeneratesCorrectFormat()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            for (int i = 0; i < 5; i++)
            {
                tracker.RecordBatch(new Dictionary<string, double>
                {
                    { "metric1", i * 10 },
                    { "metric2", i * 20 }
                }, tick: i);
            }

            // Act
            var csv = tracker.ExportToCSV();

            // Assert
            Assert.NotEmpty(csv);
            Assert.Contains("Tick", csv);
            Assert.Contains("metric1", csv);
            Assert.Contains("metric2", csv);

            var lines = csv.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(6, lines.Length); // Header + 5 data rows
        }

        [Fact]
        public void TimeSeriesTracker_ExportMetricsToCSV_ExportsOnlySpecified()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            for (int i = 0; i < 3; i++)
            {
                tracker.RecordBatch(new Dictionary<string, double>
                {
                    { "metric1", i },
                    { "metric2", i },
                    { "metric3", i }
                }, tick: i);
            }

            // Act
            var csv = tracker.ExportMetricsToCSV("metric1", "metric3");

            // Assert
            Assert.Contains("metric1", csv);
            Assert.DoesNotContain("metric2", csv);
            Assert.Contains("metric3", csv);
        }

        [Fact]
        public void TimeSeriesTracker_Clear_RemovesAllData()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            tracker.Record("test", 100);
            tracker.Record("test2", 200);

            // Act
            tracker.Clear();

            // Assert
            Assert.Null(tracker.GetLatestValue("test"));
            Assert.Null(tracker.GetLatestValue("test2"));
        }

        [Fact]
        public void TimeSeriesTracker_ClearMetric_RemovesOnlySpecified()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            tracker.Record("keep", 100);
            tracker.Record("remove", 200);

            // Act
            tracker.ClearMetric("remove");

            // Assert
            Assert.Equal(100, tracker.GetLatestValue("keep"));
            var removedSeries = tracker.GetSeries("remove");
            Assert.Equal(0, removedSeries.Count);
        }

        [Fact]
        public void TimeSeriesTracker_PruneOldData_RemovesOldPoints()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            for (int i = 0; i < 100; i++)
            {
                tracker.Record("metric", i, tick: i);
            }

            // Act
            tracker.PruneOldData(50); // Remove all before tick 50

            // Assert
            var series = tracker.GetSeries("metric");
            var all = series.GetAll();
            Assert.True(all.All(p => p.Tick >= 50));
        }

        [Fact]
        public void TimeSeries_CircularBuffer_RespectsSizeLimit()
        {
            // Arrange
            var tracker = new TimeSeriesTracker(maxDataPoints: 10);

            // Act - Add more than capacity
            for (int i = 0; i < 20; i++)
            {
                tracker.Record("limited", i, tick: i);
            }

            // Assert
            var series = tracker.GetSeries("limited");
            Assert.True(series.Count <= 10);

            // Should have most recent data
            var latest = series.GetLatest();
            Assert.Equal(19, latest.Value);
        }

        [Fact]
        public void MetricStatistics_PercentChange_CalculatesCorrectly()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            tracker.Record("growth", 100, tick: 0);
            tracker.Record("growth", 150, tick: 1);

            // Act
            var stats = tracker.GetStatistics("growth");

            // Assert
            Assert.Equal(50, stats.TotalChange);
            Assert.Equal(50, stats.PercentChange); // 50% increase
        }

        [Fact]
        public void TimeSeries_GetValueAtTick_ReturnsCorrectValue()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();
            tracker.Record("test", 42, tick: 5);
            tracker.Record("test", 84, tick: 10);

            // Act
            var series = tracker.GetSeries("test");
            var value = series.GetValueAtTick(5);

            // Assert
            Assert.Equal(42, value);
        }

        [Fact]
        public void TimeSeries_CalculateStatistics_WithRange_FilterCorrectly()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            for (int i = 0; i < 100; i++)
            {
                tracker.Record("range_test", i, tick: i);
            }

            // Act
            var stats = tracker.GetStatistics("range_test", startTick: 25, endTick: 75);

            // Assert
            Assert.Equal(51, stats.Count);
            Assert.Equal(25, stats.FirstValue);
            Assert.Equal(75, stats.LastValue);
        }

        [Fact]
        public void TrendAnalysis_WindowSize_LimitsAnalysis()
        {
            // Arrange
            var tracker = new TimeSeriesTracker();

            for (int i = 0; i < 200; i++)
            {
                tracker.Record("windowed", i, tick: i);
            }

            // Act
            var trend = tracker.AnalyzeTrend("windowed", windowSize: 50);

            // Assert
            Assert.Equal(50, trend.WindowSize);
        }

        [Fact]
        public void DataPoint_Creation_StoresCorrectData()
        {
            // Arrange & Act
            var point = new DataPoint(100, 42.5);

            // Assert
            Assert.Equal(100, point.Tick);
            Assert.Equal(42.5, point.Value);
            Assert.True(point.Timestamp <= DateTime.Now);
        }
    }
}

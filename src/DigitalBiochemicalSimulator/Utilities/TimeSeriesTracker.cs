using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DigitalBiochemicalSimulator.Utilities
{
    /// <summary>
    /// Tracks time-series data for simulation metrics over time.
    /// Provides efficient storage, querying, and analysis of temporal data.
    /// </summary>
    public class TimeSeriesTracker
    {
        private readonly Dictionary<string, TimeSeries> _series;
        private readonly int _maxDataPoints;
        private long _currentTick;

        public int SeriesCount => _series.Count;
        public IEnumerable<string> MetricNames => _series.Keys;

        /// <summary>
        /// Creates a new time series tracker
        /// </summary>
        public TimeSeriesTracker(int maxDataPoints = 10000)
        {
            _series = new Dictionary<string, TimeSeries>();
            _maxDataPoints = maxDataPoints;
            _currentTick = 0;
        }

        /// <summary>
        /// Records a data point for a metric
        /// </summary>
        public void Record(string metricName, double value, long? tick = null)
        {
            if (!_series.ContainsKey(metricName))
            {
                _series[metricName] = new TimeSeries(metricName, _maxDataPoints);
            }

            long timestamp = tick ?? _currentTick;
            _series[metricName].AddDataPoint(timestamp, value);

            if (tick == null)
                _currentTick++;
        }

        /// <summary>
        /// Records multiple metrics at once
        /// </summary>
        public void RecordBatch(Dictionary<string, double> metrics, long? tick = null)
        {
            long timestamp = tick ?? _currentTick;

            foreach (var kvp in metrics)
            {
                Record(kvp.Key, kvp.Value, timestamp);
            }

            if (tick == null)
                _currentTick++;
        }

        /// <summary>
        /// Gets a time series by name
        /// </summary>
        public TimeSeries GetSeries(string metricName)
        {
            return _series.TryGetValue(metricName, out var series) ? series : null;
        }

        /// <summary>
        /// Gets all time series
        /// </summary>
        public Dictionary<string, TimeSeries> GetAllSeries()
        {
            return new Dictionary<string, TimeSeries>(_series);
        }

        /// <summary>
        /// Gets data points for a metric within a time range
        /// </summary>
        public List<DataPoint> GetRange(string metricName, long startTick, long endTick)
        {
            var series = GetSeries(metricName);
            return series?.GetRange(startTick, endTick) ?? new List<DataPoint>();
        }

        /// <summary>
        /// Gets the latest value for a metric
        /// </summary>
        public double? GetLatestValue(string metricName)
        {
            var series = GetSeries(metricName);
            return series?.GetLatest()?.Value;
        }

        /// <summary>
        /// Gets statistics for a metric over a time range
        /// </summary>
        public MetricStatistics GetStatistics(string metricName, long? startTick = null, long? endTick = null)
        {
            var series = GetSeries(metricName);
            return series?.CalculateStatistics(startTick, endTick);
        }

        /// <summary>
        /// Detects trends in a metric
        /// </summary>
        public TrendAnalysis AnalyzeTrend(string metricName, int windowSize = 100)
        {
            var series = GetSeries(metricName);
            return series?.AnalyzeTrend(windowSize);
        }

        /// <summary>
        /// Exports all metrics to CSV format
        /// </summary>
        public string ExportToCSV()
        {
            if (_series.Count == 0)
                return string.Empty;

            var csv = new StringBuilder();
            var metricNames = _series.Keys.ToList();

            // Header
            csv.Append("Tick");
            foreach (var name in metricNames)
            {
                csv.Append($",{name}");
            }
            csv.AppendLine();

            // Find all unique ticks
            var allTicks = new SortedSet<long>();
            foreach (var series in _series.Values)
            {
                foreach (var point in series.GetAll())
                {
                    allTicks.Add(point.Tick);
                }
            }

            // Data rows
            foreach (var tick in allTicks)
            {
                csv.Append(tick);
                foreach (var name in metricNames)
                {
                    var value = _series[name].GetValueAtTick(tick);
                    csv.Append($",{value?.ToString() ?? ""}");
                }
                csv.AppendLine();
            }

            return csv.ToString();
        }

        /// <summary>
        /// Exports specific metrics to CSV
        /// </summary>
        public string ExportMetricsToCSV(params string[] metricNames)
        {
            var csv = new StringBuilder();

            // Header
            csv.Append("Tick");
            foreach (var name in metricNames)
            {
                csv.Append($",{name}");
            }
            csv.AppendLine();

            // Find all unique ticks from specified metrics
            var allTicks = new SortedSet<long>();
            foreach (var name in metricNames)
            {
                if (_series.ContainsKey(name))
                {
                    foreach (var point in _series[name].GetAll())
                    {
                        allTicks.Add(point.Tick);
                    }
                }
            }

            // Data rows
            foreach (var tick in allTicks)
            {
                csv.Append(tick);
                foreach (var name in metricNames)
                {
                    var value = _series.ContainsKey(name)
                        ? _series[name].GetValueAtTick(tick)
                        : null;
                    csv.Append($",{value?.ToString() ?? ""}");
                }
                csv.AppendLine();
            }

            return csv.ToString();
        }

        /// <summary>
        /// Clears all data
        /// </summary>
        public void Clear()
        {
            foreach (var series in _series.Values)
            {
                series.Clear();
            }
            _currentTick = 0;
        }

        /// <summary>
        /// Clears a specific metric
        /// </summary>
        public void ClearMetric(string metricName)
        {
            if (_series.ContainsKey(metricName))
            {
                _series[metricName].Clear();
            }
        }

        /// <summary>
        /// Removes old data before a certain tick
        /// </summary>
        public void PruneOldData(long beforeTick)
        {
            foreach (var series in _series.Values)
            {
                series.RemoveBefore(beforeTick);
            }
        }

        public override string ToString()
        {
            return $"TimeSeriesTracker({_series.Count} metrics, Tick: {_currentTick})";
        }
    }

    /// <summary>
    /// Represents a single time series for a metric
    /// </summary>
    public class TimeSeries
    {
        private readonly CircularBuffer<DataPoint> _data;
        private readonly string _name;

        public string Name => _name;
        public int Count => _data.Count;
        public int Capacity => _data.Capacity;

        public TimeSeries(string name, int maxDataPoints)
        {
            _name = name;
            _data = new CircularBuffer<DataPoint>(maxDataPoints);
        }

        public void AddDataPoint(long tick, double value)
        {
            _data.Add(new DataPoint(tick, value));
        }

        public DataPoint GetLatest()
        {
            return _data.Count > 0 ? _data.GetLast() : null;
        }

        public List<DataPoint> GetAll()
        {
            return _data.ToList();
        }

        public List<DataPoint> GetRange(long startTick, long endTick)
        {
            return _data.Where(p => p.Tick >= startTick && p.Tick <= endTick).ToList();
        }

        public double? GetValueAtTick(long tick)
        {
            return _data.FirstOrDefault(p => p.Tick == tick)?.Value;
        }

        public MetricStatistics CalculateStatistics(long? startTick = null, long? endTick = null)
        {
            var points = startTick.HasValue || endTick.HasValue
                ? GetRange(startTick ?? long.MinValue, endTick ?? long.MaxValue)
                : GetAll();

            if (points.Count == 0)
                return new MetricStatistics { MetricName = _name };

            var values = points.Select(p => p.Value).ToList();

            return new MetricStatistics
            {
                MetricName = _name,
                Count = values.Count,
                Min = values.Min(),
                Max = values.Max(),
                Average = values.Average(),
                Sum = values.Sum(),
                StandardDeviation = CalculateStdDev(values),
                FirstValue = points.First().Value,
                LastValue = points.Last().Value,
                FirstTick = points.First().Tick,
                LastTick = points.Last().Tick
            };
        }

        public TrendAnalysis AnalyzeTrend(int windowSize = 100)
        {
            var points = GetAll();
            if (points.Count < 2)
                return new TrendAnalysis { Trend = TrendDirection.Stable };

            // Take last N points
            var recent = points.Skip(Math.Max(0, points.Count - windowSize)).ToList();
            if (recent.Count < 2)
                return new TrendAnalysis { Trend = TrendDirection.Stable };

            // Linear regression
            var n = recent.Count;
            var sumX = 0.0;
            var sumY = 0.0;
            var sumXY = 0.0;
            var sumX2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = recent[i].Value;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            // Determine trend
            var trend = Math.Abs(slope) < 0.001
                ? TrendDirection.Stable
                : slope > 0
                    ? TrendDirection.Increasing
                    : TrendDirection.Decreasing;

            return new TrendAnalysis
            {
                Trend = trend,
                Slope = slope,
                Intercept = intercept,
                WindowSize = n,
                ChangeRate = Math.Abs(slope)
            };
        }

        public void RemoveBefore(long tick)
        {
            var toRemove = _data.Where(p => p.Tick < tick).ToList();
            foreach (var point in toRemove)
            {
                _data.Remove(point);
            }
        }

        public void Clear()
        {
            _data.Clear();
        }

        private double CalculateStdDev(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            var avg = values.Average();
            var sumSquaredDiffs = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumSquaredDiffs / values.Count);
        }

        public override string ToString()
        {
            return $"TimeSeries({_name}, {Count} points)";
        }
    }

    /// <summary>
    /// Represents a single data point in a time series
    /// </summary>
    public class DataPoint
    {
        public long Tick { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }

        public DataPoint(long tick, double value)
        {
            Tick = tick;
            Value = value;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[Tick {Tick}: {Value:F2}]";
        }
    }

    /// <summary>
    /// Statistical summary of a metric
    /// </summary>
    public class MetricStatistics
    {
        public string MetricName { get; set; }
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Sum { get; set; }
        public double StandardDeviation { get; set; }
        public double FirstValue { get; set; }
        public double LastValue { get; set; }
        public long FirstTick { get; set; }
        public long LastTick { get; set; }

        public double Range => Max - Min;
        public double TotalChange => LastValue - FirstValue;
        public double PercentChange => FirstValue != 0
            ? (TotalChange / FirstValue) * 100
            : 0;

        public override string ToString()
        {
            return $"{MetricName}: Avg={Average:F2}, Min={Min:F2}, Max={Max:F2}, " +
                   $"StdDev={StandardDeviation:F2}, Change={TotalChange:F2} ({PercentChange:F1}%)";
        }
    }

    /// <summary>
    /// Trend analysis result
    /// </summary>
    public class TrendAnalysis
    {
        public TrendDirection Trend { get; set; }
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public int WindowSize { get; set; }
        public double ChangeRate { get; set; }

        public override string ToString()
        {
            return $"Trend: {Trend}, Slope: {Slope:F4}, Rate: {ChangeRate:F4}";
        }
    }

    /// <summary>
    /// Trend direction
    /// </summary>
    public enum TrendDirection
    {
        Decreasing,
        Stable,
        Increasing
    }

    /// <summary>
    /// Circular buffer for efficient fixed-size storage
    /// </summary>
    internal class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _count;

        public int Capacity { get; }
        public int Count => _count;

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));

            Capacity = capacity;
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public void Add(T item)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % Capacity;

            if (_count < Capacity)
            {
                _count++;
            }
            else
            {
                _head = (_head + 1) % Capacity;
            }
        }

        public T GetLast()
        {
            if (_count == 0)
                return default(T);

            int lastIndex = (_tail - 1 + Capacity) % Capacity;
            return _buffer[lastIndex];
        }

        public List<T> ToList()
        {
            var result = new List<T>(_count);

            for (int i = 0; i < _count; i++)
            {
                int index = (_head + i) % Capacity;
                result.Add(_buffer[index]);
            }

            return result;
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            for (int i = 0; i < _count; i++)
            {
                int index = (_head + i) % Capacity;
                if (predicate(_buffer[index]))
                {
                    yield return _buffer[index];
                }
            }
        }

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            for (int i = 0; i < _count; i++)
            {
                int index = (_head + i) % Capacity;
                if (predicate(_buffer[index]))
                {
                    return _buffer[index];
                }
            }
            return default(T);
        }

        public bool Remove(T item)
        {
            // Note: This is inefficient for circular buffers
            // Consider using RemoveAt or just letting items age out
            var list = ToList();
            bool removed = list.Remove(item);

            if (removed)
            {
                Clear();
                foreach (var element in list)
                {
                    Add(element);
                }
            }

            return removed;
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, Capacity);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}

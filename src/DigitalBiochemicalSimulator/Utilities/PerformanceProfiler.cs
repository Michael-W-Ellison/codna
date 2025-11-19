using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DigitalBiochemicalSimulator.Utilities
{
    /// <summary>
    /// Performance profiling utility for tracking operation timing and identifying bottlenecks.
    /// </summary>
    public class PerformanceProfiler
    {
        private readonly Dictionary<string, OperationStats> _operations;
        private readonly Stack<ProfileScope> _scopeStack;
        private bool _isEnabled;

        public PerformanceProfiler(bool enabled = true)
        {
            _operations = new Dictionary<string, OperationStats>();
            _scopeStack = new Stack<ProfileScope>();
            _isEnabled = enabled;
        }

        /// <summary>
        /// Enable or disable profiling. When disabled, profiling has minimal overhead.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Start profiling an operation. Returns a disposable scope that automatically ends when disposed.
        /// </summary>
        /// <param name="operationName">Name of the operation to profile</param>
        /// <returns>Disposable profiling scope</returns>
        public IDisposable Profile(string operationName)
        {
            if (!_isEnabled)
                return new NullProfileScope();

            var scope = new ProfileScope(operationName, this);
            _scopeStack.Push(scope);
            return scope;
        }

        /// <summary>
        /// Manually record an operation timing
        /// </summary>
        public void Record(string operationName, long milliseconds)
        {
            if (!_isEnabled)
                return;

            if (!_operations.ContainsKey(operationName))
            {
                _operations[operationName] = new OperationStats(operationName);
            }

            _operations[operationName].Record(milliseconds);
        }

        /// <summary>
        /// Get statistics for a specific operation
        /// </summary>
        public OperationStats GetStats(string operationName)
        {
            return _operations.TryGetValue(operationName, out var stats)
                ? stats
                : new OperationStats(operationName);
        }

        /// <summary>
        /// Get all tracked operations sorted by total time
        /// </summary>
        public IReadOnlyList<OperationStats> GetAllStats()
        {
            return _operations.Values
                .OrderByDescending(s => s.TotalMilliseconds)
                .ToList();
        }

        /// <summary>
        /// Reset all profiling data
        /// </summary>
        public void Reset()
        {
            _operations.Clear();
            _scopeStack.Clear();
        }

        /// <summary>
        /// Generate a detailed performance report
        /// </summary>
        public string GenerateReport(bool includeDetails = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Performance Profile Report ===");
            sb.AppendLine();

            if (_operations.Count == 0)
            {
                sb.AppendLine("No profiling data collected.");
                return sb.ToString();
            }

            // Summary
            var totalTime = _operations.Values.Sum(s => s.TotalMilliseconds);
            var totalCalls = _operations.Values.Sum(s => s.CallCount);

            sb.AppendLine($"Total Operations: {_operations.Count}");
            sb.AppendLine($"Total Calls: {totalCalls:N0}");
            sb.AppendLine($"Total Time: {totalTime:N2} ms");
            sb.AppendLine();

            // Top operations by total time
            sb.AppendLine("=== Top Operations by Total Time ===");
            sb.AppendLine();
            sb.AppendLine($"{"Operation",-40} {"Calls",10} {"Total (ms)",12} {"Avg (ms)",10} {"Min (ms)",10} {"Max (ms)",10} {"% Time",8}");
            sb.AppendLine(new string('-', 110));

            foreach (var stat in GetAllStats().Take(20))
            {
                var percentage = totalTime > 0 ? (stat.TotalMilliseconds / totalTime) * 100 : 0;
                sb.AppendLine($"{stat.OperationName,-40} {stat.CallCount,10:N0} {stat.TotalMilliseconds,12:N2} {stat.AverageMilliseconds,10:N2} {stat.MinMilliseconds,10:N2} {stat.MaxMilliseconds,10:N2} {percentage,7:N1}%");
            }

            if (includeDetails)
            {
                sb.AppendLine();
                sb.AppendLine("=== Detailed Statistics ===");
                sb.AppendLine();

                foreach (var stat in GetAllStats())
                {
                    sb.AppendLine(stat.ToString());
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate a CSV report for external analysis
        /// </summary>
        public string GenerateCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Operation,Calls,TotalMs,AvgMs,MinMs,MaxMs,StdDevMs");

            foreach (var stat in GetAllStats())
            {
                sb.AppendLine($"{stat.OperationName},{stat.CallCount},{stat.TotalMilliseconds:F2},{stat.AverageMilliseconds:F2},{stat.MinMilliseconds:F2},{stat.MaxMilliseconds:F2},{stat.StandardDeviation:F2}");
            }

            return sb.ToString();
        }

        private void EndScope(ProfileScope scope)
        {
            if (_scopeStack.Count > 0 && _scopeStack.Peek() == scope)
            {
                _scopeStack.Pop();
            }

            Record(scope.OperationName, scope.ElapsedMilliseconds);
        }

        #region Nested Classes

        /// <summary>
        /// Statistics for a profiled operation
        /// </summary>
        public class OperationStats
        {
            private readonly List<long> _timings;

            public OperationStats(string operationName)
            {
                OperationName = operationName;
                _timings = new List<long>();
            }

            public string OperationName { get; }
            public int CallCount => _timings.Count;
            public long TotalMilliseconds => _timings.Sum();
            public double AverageMilliseconds => _timings.Count > 0 ? _timings.Average() : 0;
            public long MinMilliseconds => _timings.Count > 0 ? _timings.Min() : 0;
            public long MaxMilliseconds => _timings.Count > 0 ? _timings.Max() : 0;

            public double StandardDeviation
            {
                get
                {
                    if (_timings.Count == 0) return 0;
                    var avg = AverageMilliseconds;
                    var sumSquares = _timings.Sum(t => Math.Pow(t - avg, 2));
                    return Math.Sqrt(sumSquares / _timings.Count);
                }
            }

            internal void Record(long milliseconds)
            {
                _timings.Add(milliseconds);
            }

            public override string ToString()
            {
                return $"{OperationName}:\n" +
                       $"  Calls: {CallCount:N0}\n" +
                       $"  Total: {TotalMilliseconds:N2} ms\n" +
                       $"  Average: {AverageMilliseconds:N2} ms\n" +
                       $"  Min: {MinMilliseconds:N2} ms\n" +
                       $"  Max: {MaxMilliseconds:N2} ms\n" +
                       $"  Std Dev: {StandardDeviation:N2} ms";
            }
        }

        private class ProfileScope : IDisposable
        {
            private readonly Stopwatch _stopwatch;
            private readonly PerformanceProfiler _profiler;

            public ProfileScope(string operationName, PerformanceProfiler profiler)
            {
                OperationName = operationName;
                _profiler = profiler;
                _stopwatch = Stopwatch.StartNew();
            }

            public string OperationName { get; }
            public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

            public void Dispose()
            {
                _stopwatch.Stop();
                _profiler.EndScope(this);
            }
        }

        private class NullProfileScope : IDisposable
        {
            public void Dispose()
            {
                // No-op when profiling is disabled
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for convenient profiling
    /// </summary>
    public static class PerformanceProfilerExtensions
    {
        /// <summary>
        /// Profile the execution of an action
        /// </summary>
        public static void ProfileAction(this PerformanceProfiler profiler, string operationName, Action action)
        {
            using (profiler.Profile(operationName))
            {
                action();
            }
        }

        /// <summary>
        /// Profile the execution of a function
        /// </summary>
        public static T ProfileFunc<T>(this PerformanceProfiler profiler, string operationName, Func<T> func)
        {
            using (profiler.Profile(operationName))
            {
                return func();
            }
        }
    }

    /// <summary>
    /// Simulation-specific performance tracker
    /// </summary>
    public class SimulationPerformanceTracker
    {
        private readonly Stopwatch _tickTimer;
        private readonly Queue<long> _recentTickTimes;
        private readonly int _historySize;
        private long _totalTicks;
        private long _totalMilliseconds;

        public SimulationPerformanceTracker(int historySize = 100)
        {
            _historySize = historySize;
            _tickTimer = new Stopwatch();
            _recentTickTimes = new Queue<long>(historySize);
            _totalTicks = 0;
            _totalMilliseconds = 0;
        }

        /// <summary>
        /// Start timing a simulation tick
        /// </summary>
        public void StartTick()
        {
            _tickTimer.Restart();
        }

        /// <summary>
        /// End timing a simulation tick and record the result
        /// </summary>
        public void EndTick()
        {
            _tickTimer.Stop();
            var elapsed = _tickTimer.ElapsedMilliseconds;

            _recentTickTimes.Enqueue(elapsed);
            if (_recentTickTimes.Count > _historySize)
            {
                _recentTickTimes.Dequeue();
            }

            _totalTicks++;
            _totalMilliseconds += elapsed;
        }

        /// <summary>
        /// Average tick time over recent history (milliseconds)
        /// </summary>
        public double AverageTickTime => _recentTickTimes.Count > 0 ? _recentTickTimes.Average() : 0;

        /// <summary>
        /// Maximum tick time in recent history (milliseconds)
        /// </summary>
        public long MaxTickTime => _recentTickTimes.Count > 0 ? _recentTickTimes.Max() : 0;

        /// <summary>
        /// Minimum tick time in recent history (milliseconds)
        /// </summary>
        public long MinTickTime => _recentTickTimes.Count > 0 ? _recentTickTimes.Min() : 0;

        /// <summary>
        /// Estimated ticks per second based on recent average
        /// </summary>
        public double EstimatedTPS => AverageTickTime > 0 ? 1000.0 / AverageTickTime : 0;

        /// <summary>
        /// Total ticks processed
        /// </summary>
        public long TotalTicks => _totalTicks;

        /// <summary>
        /// Total time spent in ticks (milliseconds)
        /// </summary>
        public long TotalMilliseconds => _totalMilliseconds;

        /// <summary>
        /// Overall average tick time since start (milliseconds)
        /// </summary>
        public double OverallAverageTickTime => _totalTicks > 0 ? (double)_totalMilliseconds / _totalTicks : 0;

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void Reset()
        {
            _recentTickTimes.Clear();
            _totalTicks = 0;
            _totalMilliseconds = 0;
        }

        /// <summary>
        /// Generate a performance summary
        /// </summary>
        public string GenerateSummary()
        {
            return $"Performance Summary:\n" +
                   $"  Total Ticks: {TotalTicks:N0}\n" +
                   $"  Recent Avg: {AverageTickTime:F2} ms/tick\n" +
                   $"  Recent Min: {MinTickTime} ms/tick\n" +
                   $"  Recent Max: {MaxTickTime} ms/tick\n" +
                   $"  Estimated TPS: {EstimatedTPS:F1}\n" +
                   $"  Overall Avg: {OverallAverageTickTime:F2} ms/tick\n" +
                   $"  Total Time: {TotalMilliseconds / 1000.0:F1} seconds";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Damage;
using DigitalBiochemicalSimulator.Utilities;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Comprehensive statistics tracking for the simulation.
    /// Tracks tokens, chains, energy, damage, bonds, and performance metrics.
    /// </summary>
    public class SimulationStatistics
    {
        // Tracking collections
        private readonly List<Token> _allTokens;
        private readonly ChainRegistry _chainRegistry;
        private readonly DamageSystem _damageSystem;

        // Time-series data
        private readonly List<StatisticsSnapshot> _history;
        private const int MAX_HISTORY_LENGTH = 1000;

        // Time-series tracker for graphs
        private readonly TimeSeriesTracker _timeSeriesTracker;

        // Performance tracking
        private DateTime _lastUpdateTime;
        private long _lastTickCount;
        private double _currentTicksPerSecond;

        public TimeSeriesTracker TimeSeriesTracker => _timeSeriesTracker;

        public SimulationStatistics(List<Token> allTokens, ChainRegistry chainRegistry, DamageSystem damageSystem)
        {
            _allTokens = allTokens;
            _chainRegistry = chainRegistry;
            _damageSystem = damageSystem;
            _history = new List<StatisticsSnapshot>();
            _lastUpdateTime = DateTime.Now;
            _lastTickCount = 0;
            _timeSeriesTracker = new TimeSeriesTracker(maxDataPoints: 10000);
        }

        /// <summary>
        /// Updates and captures current statistics
        /// </summary>
        public StatisticsSnapshot CaptureSnapshot(long currentTick)
        {
            // Calculate ticks per second
            UpdateTicksPerSecond(currentTick);

            var snapshot = new StatisticsSnapshot
            {
                Tick = currentTick,
                Timestamp = DateTime.Now,

                // Token statistics
                TotalTokens = _allTokens.Count,
                ActiveTokens = _allTokens.Count(t => t.IsActive),
                DamagedTokens = _allTokens.Count(t => t.IsDamaged),
                CriticallyDamagedTokens = _allTokens.Count(t => t.DamageLevel >= 0.8f),
                AverageDamageLevel = _allTokens.Any() ? _allTokens.Average(t => t.DamageLevel) : 0,

                // Energy statistics
                TotalEnergy = _allTokens.Where(t => t.IsActive).Sum(t => t.Energy),
                AverageEnergy = _allTokens.Any(t => t.IsActive)
                    ? _allTokens.Where(t => t.IsActive).Average(t => t.Energy)
                    : 0,
                MinEnergy = _allTokens.Any(t => t.IsActive)
                    ? _allTokens.Where(t => t.IsActive).Min(t => t.Energy)
                    : 0,
                MaxEnergy = _allTokens.Any(t => t.IsActive)
                    ? _allTokens.Where(t => t.IsActive).Max(t => t.Energy)
                    : 0,

                // Bond statistics
                TotalBonds = _allTokens.Where(t => t.IsActive).Sum(t => t.BondedTokens.Count) / 2, // Divide by 2 to avoid double counting
                BondedTokens = _allTokens.Count(t => t.IsActive && t.BondedTokens.Count > 0),

                // Performance
                TicksPerSecond = _currentTicksPerSecond
            };

            // Chain statistics (if registry available)
            if (_chainRegistry != null)
            {
                var chainStats = _chainRegistry.GetStatistics();
                snapshot.TotalChains = chainStats.TotalChains;
                snapshot.StableChains = chainStats.StableChains;
                snapshot.ValidChains = chainStats.ValidChains;
                snapshot.AverageChainLength = chainStats.AverageLength;
                snapshot.LongestChainLength = chainStats.LongestChainLength;
                snapshot.AverageChainStability = chainStats.AverageStability;
                snapshot.TotalTokensInChains = chainStats.TotalTokensInChains;
            }

            // Add to history
            _history.Add(snapshot);
            if (_history.Count > MAX_HISTORY_LENGTH)
            {
                _history.RemoveAt(0);
            }

            // Record time-series data
            RecordTimeSeriesData(snapshot);

            return snapshot;
        }

        /// <summary>
        /// Records snapshot data into time-series tracker for graphing
        /// </summary>
        private void RecordTimeSeriesData(StatisticsSnapshot snapshot)
        {
            var metrics = new Dictionary<string, double>
            {
                // Population metrics
                { "TotalTokens", snapshot.TotalTokens },
                { "ActiveTokens", snapshot.ActiveTokens },
                { "DamagedTokens", snapshot.DamagedTokens },
                { "CriticallyDamagedTokens", snapshot.CriticallyDamagedTokens },
                { "BondedTokens", snapshot.BondedTokens },

                // Energy metrics
                { "TotalEnergy", snapshot.TotalEnergy },
                { "AverageEnergy", snapshot.AverageEnergy },
                { "MinEnergy", snapshot.MinEnergy },
                { "MaxEnergy", snapshot.MaxEnergy },

                // Chain metrics
                { "TotalChains", snapshot.TotalChains },
                { "StableChains", snapshot.StableChains },
                { "ValidChains", snapshot.ValidChains },
                { "LongestChainLength", snapshot.LongestChainLength },
                { "AverageChainLength", snapshot.AverageChainLength },
                { "AverageChainStability", snapshot.AverageChainStability },
                { "TotalTokensInChains", snapshot.TotalTokensInChains },

                // Damage metrics
                { "AverageDamageLevel", snapshot.AverageDamageLevel },

                // Bond metrics
                { "TotalBonds", snapshot.TotalBonds },

                // Performance metrics
                { "TicksPerSecond", snapshot.TicksPerSecond }
            };

            _timeSeriesTracker.RecordBatch(metrics, snapshot.Tick);
        }

        /// <summary>
        /// Updates ticks per second calculation
        /// </summary>
        private void UpdateTicksPerSecond(long currentTick)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastUpdateTime).TotalSeconds;

            if (elapsed >= 1.0) // Update once per second
            {
                var ticksDelta = currentTick - _lastTickCount;
                _currentTicksPerSecond = ticksDelta / elapsed;
                _lastUpdateTime = now;
                _lastTickCount = currentTick;
            }
        }

        /// <summary>
        /// Gets the most recent snapshot
        /// </summary>
        public StatisticsSnapshot GetLatestSnapshot()
        {
            return _history.LastOrDefault();
        }

        /// <summary>
        /// Gets historical snapshots
        /// </summary>
        public List<StatisticsSnapshot> GetHistory(int maxCount = 100)
        {
            int startIndex = Math.Max(0, _history.Count - maxCount);
            return _history.Skip(startIndex).ToList();
        }

        /// <summary>
        /// Gets a summary string of current statistics
        /// </summary>
        public string GetSummaryString()
        {
            var latest = GetLatestSnapshot();
            if (latest == null)
                return "No statistics available";

            return $"Tick {latest.Tick} | Tokens: {latest.ActiveTokens}/{latest.TotalTokens} | " +
                   $"Chains: {latest.TotalChains} ({latest.StableChains} stable) | " +
                   $"Longest: {latest.LongestChainLength} | " +
                   $"Bonds: {latest.TotalBonds} | " +
                   $"Energy: {latest.TotalEnergy} (avg {latest.AverageEnergy:F1}) | " +
                   $"Damage: {latest.DamagedTokens}/{latest.ActiveTokens} ({latest.AverageDamageLevel:F2}) | " +
                   $"TPS: {latest.TicksPerSecond:F1}";
        }

        /// <summary>
        /// Clears all history
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// Gets statistics for specific token types
        /// </summary>
        public Dictionary<TokenType, int> GetTokenTypeDistribution()
        {
            return _allTokens
                .Where(t => t.IsActive)
                .GroupBy(t => t.Type)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets altitude distribution of tokens
        /// </summary>
        public Dictionary<int, int> GetAltitudeDistribution()
        {
            return _allTokens
                .Where(t => t.IsActive)
                .GroupBy(t => t.Position.Y)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Exports statistics to CSV format
        /// </summary>
        public string ExportToCSV()
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Tick,Timestamp,ActiveTokens,TotalChains,StableChains,ValidChains,LongestChain,TotalBonds,TotalEnergy,AverageDamage,TPS");

            foreach (var snapshot in _history)
            {
                csv.AppendLine($"{snapshot.Tick}," +
                              $"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"{snapshot.ActiveTokens}," +
                              $"{snapshot.TotalChains}," +
                              $"{snapshot.StableChains}," +
                              $"{snapshot.ValidChains}," +
                              $"{snapshot.LongestChainLength}," +
                              $"{snapshot.TotalBonds}," +
                              $"{snapshot.TotalEnergy}," +
                              $"{snapshot.AverageDamageLevel:F4}," +
                              $"{snapshot.TicksPerSecond:F2}");
            }

            return csv.ToString();
        }
    }

    /// <summary>
    /// A snapshot of statistics at a specific tick
    /// </summary>
    public class StatisticsSnapshot
    {
        public long Tick { get; set; }
        public DateTime Timestamp { get; set; }

        // Token statistics
        public int TotalTokens { get; set; }
        public int ActiveTokens { get; set; }
        public int DamagedTokens { get; set; }
        public int CriticallyDamagedTokens { get; set; }
        public double AverageDamageLevel { get; set; }

        // Energy statistics
        public int TotalEnergy { get; set; }
        public double AverageEnergy { get; set; }
        public int MinEnergy { get; set; }
        public int MaxEnergy { get; set; }

        // Bond statistics
        public int TotalBonds { get; set; }
        public int BondedTokens { get; set; }

        // Chain statistics
        public int TotalChains { get; set; }
        public int StableChains { get; set; }
        public int ValidChains { get; set; }
        public double AverageChainLength { get; set; }
        public int LongestChainLength { get; set; }
        public double AverageChainStability { get; set; }
        public int TotalTokensInChains { get; set; }

        // Performance
        public double TicksPerSecond { get; set; }

        public override string ToString()
        {
            return $"[Tick {Tick}] Tokens: {ActiveTokens}, Chains: {TotalChains}, Longest: {LongestChainLength}, TPS: {TicksPerSecond:F1}";
        }
    }
}

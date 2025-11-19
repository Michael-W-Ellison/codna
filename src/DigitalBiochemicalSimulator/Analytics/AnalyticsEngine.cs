using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.Utilities;

namespace DigitalBiochemicalSimulator.Analytics
{
    /// <summary>
    /// Comprehensive analytics engine with export capabilities
    /// Provides real-time metrics and historical analysis
    /// </summary>
    public class AnalyticsEngine
    {
        private readonly TimeSeriesTracker _timeSeriesTracker;
        private readonly EvolutionTracker _evolutionTracker;
        private readonly List<SimulationEvent> _events;
        private readonly object _analyticsLock = new object();

        public TimeSeriesTracker TimeSeries => _timeSeriesTracker;
        public EvolutionTracker Evolution => _evolutionTracker;

        public AnalyticsEngine()
        {
            _timeSeriesTracker = new TimeSeriesTracker(maxDataPoints: 50000);
            _evolutionTracker = new EvolutionTracker();
            _events = new List<SimulationEvent>();
        }

        /// <summary>
        /// Records a full simulation snapshot
        /// </summary>
        public void RecordSnapshot(IntegratedSimulationEngine simulation, long tick)
        {
            if (simulation == null)
                return;

            var stats = simulation.GetStatistics();

            // Record time series metrics
            var metrics = new Dictionary<string, double>
            {
                ["active_tokens"] = stats.ActiveTokenCount,
                ["total_generated"] = stats.TotalGenerated,
                ["total_destroyed"] = stats.TotalDestroyed,
                ["damaged_tokens"] = stats.DamagedTokens,
                ["avg_energy"] = stats.AverageEnergy,
                ["total_energy"] = stats.TotalEnergy,
                ["total_bonds"] = stats.TotalBonds,
                ["bonds_formed"] = stats.TotalBondsFormed,
                ["bonds_broken"] = stats.TotalBondsBroken,
                ["total_chains"] = stats.TotalChains,
                ["stable_chains"] = stats.StableChains,
                ["valid_chains"] = stats.ValidChains,
                ["longest_chain"] = stats.LongestChainLength,
                ["avg_chain_length"] = stats.AverageChainLength,
                ["active_cells"] = stats.ActiveCellCount,
                ["tps"] = stats.TicksPerSecond
            };

            _timeSeriesTracker.RecordBatch(metrics, tick);

            // Record chain evolution
            var chains = simulation.ChainRegistry.GetAllChains();
            foreach (var chain in chains)
            {
                _evolutionTracker.RecordChainState(chain, tick);
            }
        }

        /// <summary>
        /// Records a simulation event
        /// </summary>
        public void RecordEvent(string eventType, string description, long tick, Dictionary<string, object> data = null)
        {
            lock (_analyticsLock)
            {
                _events.Add(new SimulationEvent
                {
                    Type = eventType,
                    Description = description,
                    Tick = tick,
                    Timestamp = DateTime.Now,
                    Data = data ?? new Dictionary<string, object>()
                });
            }
        }

        /// <summary>
        /// Exports all analytics data to JSON
        /// </summary>
        public string ExportToJSON()
        {
            lock (_analyticsLock)
            {
                var data = new
                {
                    ExportTime = DateTime.Now,
                    TimeSeries = ExportTimeSeriesJSON(),
                    Evolution = ExportEvolutionJSON(),
                    Events = _events,
                    Summary = GetAnalyticsSummary()
                };

                return JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }

        /// <summary>
        /// Exports time series data to JSON
        /// </summary>
        private object ExportTimeSeriesJSON()
        {
            var series = new Dictionary<string, List<object>>();

            foreach (var metricName in _timeSeriesTracker.MetricNames)
            {
                var timeSeries = _timeSeriesTracker.GetSeries(metricName);
                if (timeSeries != null)
                {
                    var dataPoints = timeSeries.GetAll()
                        .Select(dp => new { Tick = dp.Tick, Value = dp.Value })
                        .ToList();

                    series[metricName] = dataPoints.Cast<object>().ToList();
                }
            }

            return series;
        }

        /// <summary>
        /// Exports evolution data to JSON
        /// </summary>
        private object ExportEvolutionJSON()
        {
            var stats = _evolutionTracker.GetStatistics();
            var topLineages = _evolutionTracker.GetTopLineages(10);
            var patterns = _evolutionTracker.IdentifyCommonPatterns().Take(20).ToList();

            return new
            {
                Statistics = stats,
                TopLineages = topLineages,
                CommonPatterns = patterns.Select(p => new
                {
                    Hash = p.Pattern.Hash,
                    Length = p.Pattern.Length,
                    Count = p.Count,
                    AverageFitness = p.AverageFitness
                })
            };
        }

        /// <summary>
        /// Exports comprehensive CSV report
        /// </summary>
        public string ExportComprehensiveCSV()
        {
            var csv = new StringBuilder();

            // Time Series Section
            csv.AppendLine("=== TIME SERIES DATA ===");
            csv.AppendLine(_timeSeriesTracker.ExportToCSV());
            csv.AppendLine();

            // Evolution Section
            csv.AppendLine("=== EVOLUTION DATA ===");
            csv.AppendLine(_evolutionTracker.ExportToCSV());
            csv.AppendLine();

            // Events Section
            csv.AppendLine("=== SIMULATION EVENTS ===");
            csv.AppendLine("Tick,Type,Description,Timestamp");
            lock (_analyticsLock)
            {
                foreach (var evt in _events.OrderBy(e => e.Tick))
                {
                    csv.AppendLine($"{evt.Tick},\"{evt.Type}\",\"{evt.Description}\",{evt.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }
            }

            return csv.ToString();
        }

        /// <summary>
        /// Gets a comprehensive analytics summary
        /// </summary>
        public AnalyticsSummary GetAnalyticsSummary()
        {
            var evolutionStats = _evolutionTracker.GetStatistics();
            var patterns = _evolutionTracker.IdentifyCommonPatterns();

            return new AnalyticsSummary
            {
                TotalDataPoints = _timeSeriesTracker.MetricNames.Sum(m =>
                    _timeSeriesTracker.GetSeries(m)?.Count ?? 0),
                MetricsTracked = _timeSeriesTracker.SeriesCount,
                TotalLineages = evolutionStats.TotalLineages,
                ActiveLineages = evolutionStats.ActiveLineages,
                UniquePatterns = evolutionStats.UniquePatterns,
                TotalEvents = _events.Count,
                MostCommonPattern = patterns.FirstOrDefault()?.Pattern.Hash ?? "None",
                HighestFitness = patterns.Any() ? patterns.Max(p => p.AverageFitness) : 0
            };
        }

        /// <summary>
        /// Gets real-time dashboard data
        /// </summary>
        public DashboardData GetDashboardData()
        {
            var evolutionStats = _evolutionTracker.GetStatistics();
            var recentEvents = _events.OrderByDescending(e => e.Tick).Take(10).ToList();

            // Get latest values for key metrics
            var dashboard = new DashboardData
            {
                CurrentTick = _timeSeriesTracker.GetLatestValue("active_tokens") != null
                    ? (long)(_timeSeriesTracker.GetSeries("active_tokens")?.GetLatest()?.Tick ?? 0)
                    : 0,
                ActiveTokens = (int)(_timeSeriesTracker.GetLatestValue("active_tokens") ?? 0),
                TotalChains = (int)(_timeSeriesTracker.GetLatestValue("total_chains") ?? 0),
                StableChains = (int)(_timeSeriesTracker.GetLatestValue("stable_chains") ?? 0),
                AverageEnergy = _timeSeriesTracker.GetLatestValue("avg_energy") ?? 0,
                TPS = _timeSeriesTracker.GetLatestValue("tps") ?? 0,
                EvolutionStats = evolutionStats,
                RecentEvents = recentEvents,
                Trends = CalculateTrends()
            };

            return dashboard;
        }

        /// <summary>
        /// Calculates trends for key metrics
        /// </summary>
        private Dictionary<string, TrendData> CalculateTrends()
        {
            var trends = new Dictionary<string, TrendData>();
            var keyMetrics = new[] { "active_tokens", "total_chains", "stable_chains", "avg_energy" };

            foreach (var metric in keyMetrics)
            {
                var trend = _timeSeriesTracker.AnalyzeTrend(metric, windowSize: 100);
                if (trend != null)
                {
                    trends[metric] = new TrendData
                    {
                        Direction = trend.Trend.ToString(),
                        Slope = trend.Slope,
                        ChangeRate = trend.ChangeRate
                    };
                }
            }

            return trends;
        }

        /// <summary>
        /// Exports specific metric to CSV
        /// </summary>
        public string ExportMetricToCSV(string metricName)
        {
            return _timeSeriesTracker.ExportMetricsToCSV(metricName);
        }

        /// <summary>
        /// Gets statistics for a specific metric
        /// </summary>
        public MetricStatistics GetMetricStatistics(string metricName)
        {
            return _timeSeriesTracker.GetStatistics(metricName);
        }

        /// <summary>
        /// Clears all analytics data
        /// </summary>
        public void Clear()
        {
            _timeSeriesTracker.Clear();
            _evolutionTracker.Clear();
            lock (_analyticsLock)
            {
                _events.Clear();
            }
        }
    }

    /// <summary>
    /// Simulation event record
    /// </summary>
    public class SimulationEvent
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public long Tick { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// Analytics summary
    /// </summary>
    public class AnalyticsSummary
    {
        public int TotalDataPoints { get; set; }
        public int MetricsTracked { get; set; }
        public int TotalLineages { get; set; }
        public int ActiveLineages { get; set; }
        public int UniquePatterns { get; set; }
        public int TotalEvents { get; set; }
        public string MostCommonPattern { get; set; }
        public double HighestFitness { get; set; }
    }

    /// <summary>
    /// Real-time dashboard data
    /// </summary>
    public class DashboardData
    {
        public long CurrentTick { get; set; }
        public int ActiveTokens { get; set; }
        public int TotalChains { get; set; }
        public int StableChains { get; set; }
        public double AverageEnergy { get; set; }
        public double TPS { get; set; }
        public EvolutionStatistics EvolutionStats { get; set; }
        public List<SimulationEvent> RecentEvents { get; set; }
        public Dictionary<string, TrendData> Trends { get; set; }
    }

    /// <summary>
    /// Trend data for a metric
    /// </summary>
    public class TrendData
    {
        public string Direction { get; set; }
        public double Slope { get; set; }
        public double ChangeRate { get; set; }
    }
}

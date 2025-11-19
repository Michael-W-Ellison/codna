using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;

namespace DigitalBiochemicalSimulator.Analytics
{
    /// <summary>
    /// Tracks the evolution and lineage of token chains over time
    /// Identifies patterns and calculates fitness scores
    /// </summary>
    public class EvolutionTracker
    {
        private readonly Dictionary<long, ChainLineage> _lineages;
        private readonly List<ChainSnapshot> _history;
        private readonly PatternRecognizer _patternRecognizer;
        private long _generationCounter;
        private readonly object _trackerLock = new object();

        public int LineageCount => _lineages.Count;
        public int SnapshotCount => _history.Count;

        public EvolutionTracker()
        {
            _lineages = new Dictionary<long, ChainLineage>();
            _history = new List<ChainSnapshot>();
            _patternRecognizer = new PatternRecognizer();
            _generationCounter = 0;
        }

        /// <summary>
        /// Records a chain formation event
        /// </summary>
        public void RecordChainFormation(TokenChain chain, long tick)
        {
            if (chain == null || chain.Length < 2)
                return;

            lock (_trackerLock)
            {
                var lineage = new ChainLineage
                {
                    ChainId = chain.Id,
                    Generation = _generationCounter++,
                    BirthTick = tick,
                    Pattern = ExtractPattern(chain),
                    Length = chain.Length,
                    InitialStability = chain.StabilityScore,
                    ParentIds = new List<long>() // Will be populated if chain merges
                };

                _lineages[chain.Id] = lineage;

                // Create snapshot
                var snapshot = new ChainSnapshot
                {
                    ChainId = chain.Id,
                    Tick = tick,
                    Length = chain.Length,
                    Stability = chain.StabilityScore,
                    Pattern = lineage.Pattern,
                    Fitness = CalculateFitness(chain, tick)
                };

                _history.Add(snapshot);
            }
        }

        /// <summary>
        /// Records a chain's current state
        /// </summary>
        public void RecordChainState(TokenChain chain, long tick)
        {
            if (chain == null)
                return;

            lock (_trackerLock)
            {
                var snapshot = new ChainSnapshot
                {
                    ChainId = chain.Id,
                    Tick = tick,
                    Length = chain.Length,
                    Stability = chain.StabilityScore,
                    Pattern = ExtractPattern(chain),
                    Fitness = CalculateFitness(chain, tick)
                };

                _history.Add(snapshot);

                // Update lineage if exists
                if (_lineages.TryGetValue(chain.Id, out var lineage))
                {
                    lineage.DeathTick = tick; // Update last seen
                    lineage.PeakStability = Math.Max(lineage.PeakStability, chain.StabilityScore);
                    lineage.PeakLength = Math.Max(lineage.PeakLength, chain.Length);
                }
            }
        }

        /// <summary>
        /// Records a chain's destruction
        /// </summary>
        public void RecordChainDestruction(long chainId, long tick, string reason)
        {
            lock (_trackerLock)
            {
                if (_lineages.TryGetValue(chainId, out var lineage))
                {
                    lineage.DeathTick = tick;
                    lineage.DeathReason = reason;
                    lineage.Lifespan = tick - lineage.BirthTick;
                }
            }
        }

        /// <summary>
        /// Calculates fitness score for a chain
        /// Higher score = more "successful" evolution
        /// Thread-safe implementation
        /// </summary>
        public double CalculateFitness(TokenChain chain, long currentTick)
        {
            if (chain == null)
                return 0;

            double fitness = 0;

            // Factor 1: Length (longer chains are more complex)
            fitness += chain.Length * 10;

            // Factor 2: Stability (stable chains survive longer)
            fitness += chain.StabilityScore * 100;

            // Factor 3: Age (older chains that survive are fitter)
            lock (_trackerLock)
            {
                if (_lineages.TryGetValue(chain.Id, out var lineage))
                {
                    long age = currentTick - lineage.BirthTick;
                    fitness += Math.Log(age + 1) * 20;
                }
            }

            // Factor 4: Pattern complexity (more diverse tokens = higher fitness)
            var pattern = ExtractPattern(chain);
            fitness += pattern.UniqueTokenTypes * 5;

            // Factor 5: Energy efficiency (total energy / length)
            if (chain.Length > 0)
            {
                fitness += (chain.TotalEnergy / chain.Length) * 2;
            }

            return fitness;
        }

        /// <summary>
        /// Extracts a pattern signature from a chain
        /// </summary>
        private ChainPattern ExtractPattern(TokenChain chain)
        {
            if (chain == null || chain.Tokens == null)
                return new ChainPattern { Hash = "" };

            var tokens = chain.Tokens.ToList();
            var typeSequence = tokens.Select(t => t.Type).ToList();
            var valueSequence = tokens.Select(t => t.Value).ToList();

            return new ChainPattern
            {
                Length = chain.Length,
                TokenTypes = typeSequence,
                TokenValues = valueSequence,
                UniqueTokenTypes = typeSequence.Distinct().Count(),
                Hash = ComputePatternHash(typeSequence)
            };
        }

        /// <summary>
        /// Computes a hash for pattern matching
        /// </summary>
        private string ComputePatternHash(List<TokenType> types)
        {
            return string.Join("-", types.Select(t => ((int)t).ToString()));
        }

        /// <summary>
        /// Gets the lineage of a specific chain
        /// </summary>
        public ChainLineage GetLineage(long chainId)
        {
            lock (_trackerLock)
            {
                return _lineages.TryGetValue(chainId, out var lineage) ? lineage : null;
            }
        }

        /// <summary>
        /// Gets all lineages sorted by fitness
        /// </summary>
        public List<ChainLineage> GetTopLineages(int count)
        {
            lock (_trackerLock)
            {
                return _lineages.Values
                    .OrderByDescending(l => l.PeakStability * l.PeakLength)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets snapshot history for a chain
        /// </summary>
        public List<ChainSnapshot> GetChainHistory(long chainId)
        {
            lock (_trackerLock)
            {
                return _history
                    .Where(s => s.ChainId == chainId)
                    .OrderBy(s => s.Tick)
                    .ToList();
            }
        }

        /// <summary>
        /// Identifies common patterns across all chains
        /// </summary>
        public List<PatternFrequency> IdentifyCommonPatterns()
        {
            lock (_trackerLock)
            {
                var patternCounts = new Dictionary<string, PatternFrequency>();

                foreach (var snapshot in _history)
                {
                    if (snapshot?.Pattern?.Hash == null)
                        continue;

                    var hash = snapshot.Pattern.Hash;
                    if (!patternCounts.ContainsKey(hash))
                    {
                        patternCounts[hash] = new PatternFrequency
                        {
                            Pattern = snapshot.Pattern,
                            Count = 0,
                            AverageFitness = 0,
                            TotalFitness = 0
                        };
                    }

                    patternCounts[hash].Count++;
                    patternCounts[hash].TotalFitness += snapshot.Fitness;
                    patternCounts[hash].AverageFitness =
                        patternCounts[hash].TotalFitness / patternCounts[hash].Count;
                }

                return patternCounts.Values
                    .OrderByDescending(p => p.Count)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets evolution statistics
        /// </summary>
        public EvolutionStatistics GetStatistics()
        {
            lock (_trackerLock)
            {
                var activeLineages = _lineages.Values.Where(l => l.DeathTick == 0).ToList();
                var deadLineages = _lineages.Values.Where(l => l.DeathTick > 0).ToList();

                return new EvolutionStatistics
                {
                    TotalLineages = _lineages.Count,
                    ActiveLineages = activeLineages.Count,
                    ExtinctLineages = deadLineages.Count,
                    TotalGenerations = _generationCounter,
                    AverageLifespan = deadLineages.Any()
                        ? deadLineages.Average(l => l.Lifespan)
                        : 0,
                    LongestLineage = _lineages.Values.Any()
                        ? _lineages.Values.Max(l => l.PeakLength)
                        : 0,
                    HighestStability = _lineages.Values.Any()
                        ? _lineages.Values.Max(l => l.PeakStability)
                        : 0,
                    UniquePatterns = _history
                        .Where(s => s?.Pattern?.Hash != null)
                        .Select(s => s.Pattern.Hash)
                        .Distinct()
                        .Count()
                };
            }
        }

        /// <summary>
        /// Exports evolution data to CSV
        /// </summary>
        public string ExportToCSV()
        {
            lock (_trackerLock)
            {
                var csv = new System.Text.StringBuilder();

                // Header
                csv.AppendLine("ChainId,Tick,Length,Stability,Fitness,Pattern");

                // Data rows
                foreach (var snapshot in _history.OrderBy(s => s.Tick))
                {
                    var patternHash = snapshot?.Pattern?.Hash ?? "";
                    csv.AppendLine($"{snapshot.ChainId},{snapshot.Tick},{snapshot.Length}," +
                                 $"{snapshot.Stability:F2},{snapshot.Fitness:F2}," +
                                 $"\"{patternHash}\"");
                }

                return csv.ToString();
            }
        }

        /// <summary>
        /// Clears all evolution data
        /// </summary>
        public void Clear()
        {
            lock (_trackerLock)
            {
                _lineages.Clear();
                _history.Clear();
                _generationCounter = 0;
                _patternRecognizer.Clear();
            }
        }
    }

    /// <summary>
    /// Represents the lineage of a chain
    /// </summary>
    public class ChainLineage
    {
        public long ChainId { get; set; }
        public long Generation { get; set; }
        public long BirthTick { get; set; }
        public long DeathTick { get; set; }
        public long Lifespan { get; set; }
        public int Length { get; set; }
        public int PeakLength { get; set; }
        public float InitialStability { get; set; }
        public float PeakStability { get; set; }
        public ChainPattern Pattern { get; set; }
        public List<long> ParentIds { get; set; }
        public string DeathReason { get; set; }
    }

    /// <summary>
    /// Snapshot of a chain at a specific tick
    /// </summary>
    public class ChainSnapshot
    {
        public long ChainId { get; set; }
        public long Tick { get; set; }
        public int Length { get; set; }
        public float Stability { get; set; }
        public ChainPattern Pattern { get; set; }
        public double Fitness { get; set; }
    }

    /// <summary>
    /// Pattern representation of a chain
    /// </summary>
    public class ChainPattern
    {
        public int Length { get; set; }
        public List<TokenType> TokenTypes { get; set; }
        public List<string> TokenValues { get; set; }
        public int UniqueTokenTypes { get; set; }
        public string Hash { get; set; }

        public ChainPattern()
        {
            TokenTypes = new List<TokenType>();
            TokenValues = new List<string>();
        }
    }

    /// <summary>
    /// Pattern frequency analysis
    /// </summary>
    public class PatternFrequency
    {
        public ChainPattern Pattern { get; set; }
        public int Count { get; set; }
        public double TotalFitness { get; set; }
        public double AverageFitness { get; set; }
    }

    /// <summary>
    /// Evolution statistics summary
    /// </summary>
    public class EvolutionStatistics
    {
        public int TotalLineages { get; set; }
        public int ActiveLineages { get; set; }
        public int ExtinctLineages { get; set; }
        public long TotalGenerations { get; set; }
        public double AverageLifespan { get; set; }
        public int LongestLineage { get; set; }
        public float HighestStability { get; set; }
        public int UniquePatterns { get; set; }
    }

    /// <summary>
    /// Pattern recognition engine
    /// </summary>
    public class PatternRecognizer
    {
        private readonly Dictionary<string, List<ChainPattern>> _patterns;

        public PatternRecognizer()
        {
            _patterns = new Dictionary<string, List<ChainPattern>>();
        }

        public void RecordPattern(ChainPattern pattern)
        {
            if (pattern == null || string.IsNullOrEmpty(pattern.Hash))
                return;

            if (!_patterns.ContainsKey(pattern.Hash))
            {
                _patterns[pattern.Hash] = new List<ChainPattern>();
            }

            _patterns[pattern.Hash].Add(pattern);
        }

        public int GetPatternCount(string hash)
        {
            return _patterns.TryGetValue(hash, out var patterns) ? patterns.Count : 0;
        }

        public void Clear()
        {
            _patterns.Clear();
        }
    }
}

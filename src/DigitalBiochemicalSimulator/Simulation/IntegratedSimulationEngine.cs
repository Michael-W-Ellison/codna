using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Physics;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Damage;
using DigitalBiochemicalSimulator.Utilities;
using DigitalBiochemicalSimulator.Analytics;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Fully integrated simulation engine with all systems.
    /// Orchestrates physics, chemistry, grammar, bonding, damage, and chains.
    /// Implements IDisposable for proper resource cleanup.
    /// </summary>
    public class IntegratedSimulationEngine : IDisposable
    {
        private bool _disposed = false;
        // Configuration
        public SimulationConfig Config { get; private set; }

        // Core Systems
        public Grid Grid { get; private set; }
        public TickManager TickManager { get; private set; }
        public TokenPool TokenPool { get; private set; }
        public TokenFactory TokenFactory { get; private set; }

        // Thermal Vents
        public List<ThermalVent> ThermalVents { get; private set; }

        // Physics Systems
        public EnergyManager EnergyManager { get; private set; }
        public MotionController MotionController { get; private set; }
        public GravitySimulator GravitySimulator { get; private set; }
        public CollisionDetector CollisionDetector { get; private set; }

        // Chemistry Systems (Phase 3)
        public BondRulesEngine BondRulesEngine { get; private set; }
        public BondStrengthCalculator BondStrengthCalculator { get; private set; }
        public BondingManager BondingManager { get; private set; }
        public RepulsionHandler RepulsionHandler { get; private set; }
        public ChainStabilityCalculator ChainStabilityCalculator { get; private set; }
        public ChainRegistry ChainRegistry { get; private set; }

        // Damage System (Phase 4)
        public DamageSystem DamageSystem { get; private set; }

        // Integration Systems (Phase 5)
        public CellProcessor CellProcessor { get; private set; }
        public SimulationStatistics Statistics { get; private set; }

        // Analytics System
        public AnalyticsEngine Analytics { get; private set; }

        // Chain Analysis System
        public ChainAnalyzer ChainAnalyzer { get; private set; }
        private List<ComprehensiveAnalysisResult> _latestAnalysis;
        private readonly object _analysisLock = new object();

        // State
        public List<Token> ActiveTokens { get; private set; }
        public bool IsRunning { get; private set; }

        // Statistics
        public long TotalTokensGenerated { get; private set; }
        public long TotalTokensDestroyed { get; private set; }
        public long TotalBondsFormed { get; private set; }
        public long TotalBondsBroken { get; private set; }

        public IntegratedSimulationEngine(SimulationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));

            // Validate configuration
            if (!Config.Validate(out var errorMessage))
            {
                throw new ArgumentException($"Invalid configuration: {errorMessage}");
            }

            InitializeSystems();
        }

        /// <summary>
        /// Initializes all simulation systems
        /// </summary>
        private void InitializeSystems()
        {
            // Core systems
            Grid = new Grid(Config.GridWidth, Config.GridHeight, Config.GridDepth, Config.CellCapacity);
            TickManager = new TickManager(Config.TickDuration);
            TokenPool = new TokenPool(100, Config.MaxActiveTokens);
            TokenFactory = new TokenFactory(TokenPool);

            // Physics systems
            EnergyManager = new EnergyManager(Config);
            MotionController = new MotionController(Config, Grid);
            GravitySimulator = new GravitySimulator(Config, Grid);
            CollisionDetector = new CollisionDetector(Config, Grid);

            // Grammar and Chemistry systems
            var grammarRules = GrammarLibrary.GetDefaultGrammar();
            BondRulesEngine = new BondRulesEngine(grammarRules);
            BondStrengthCalculator = new BondStrengthCalculator(
                BondRulesEngine,
                Config.CovalentBondStrength,
                Config.IonicBondStrength,
                Config.VanDerWaalsBondStrength);
            BondingManager = new BondingManager(BondRulesEngine, BondStrengthCalculator, Config, Grid);
            RepulsionHandler = new RepulsionHandler(Grid, BondRulesEngine);
            ChainStabilityCalculator = new ChainStabilityCalculator(BondRulesEngine, BondStrengthCalculator);
            ChainRegistry = new ChainRegistry(ChainStabilityCalculator);

            // Damage system
            DamageSystem = new DamageSystem(Config, BondingManager);

            // Integration systems
            CellProcessor = new CellProcessor(BondingManager, RepulsionHandler, Config);

            // Initialize thermal vents
            ThermalVents = new List<ThermalVent>();
            CreateThermalVents();

            // State
            ActiveTokens = new List<Token>();
            IsRunning = false;

            // Statistics
            TotalTokensGenerated = 0;
            TotalTokensDestroyed = 0;
            TotalBondsFormed = 0;
            TotalBondsBroken = 0;

            // Initialize statistics tracker (after ActiveTokens is initialized)
            Statistics = new SimulationStatistics(ActiveTokens, ChainRegistry, DamageSystem);

            // Initialize analytics engine
            Analytics = new AnalyticsEngine();

            // Initialize chain analyzer
            ChainAnalyzer = new ChainAnalyzer();
            _latestAnalysis = new List<ComprehensiveAnalysisResult>();

            // Update mutation zones
            Grid.UpdateMutationZone(Config.MutationRange);
        }

        /// <summary>
        /// Creates thermal vents based on configuration
        /// </summary>
        private void CreateThermalVents()
        {
            for (int i = 0; i < Config.NumberOfVents; i++)
            {
                var position = GetVentPosition(i);
                var vent = new ThermalVent(position, TokenFactory,
                    Config.VentEmissionRate, Config.InitialTokenEnergy);

                // Set distribution based on config
                if (Config.TokenDistribution == TokenDistributionProfile.ExpressionHeavy)
                {
                    vent.SetExpressionHeavyDistribution();
                }
                else if (Config.TokenDistribution == TokenDistributionProfile.ControlHeavy)
                {
                    vent.SetControlHeavyDistribution();
                }

                ThermalVents.Add(vent);
            }
        }

        /// <summary>
        /// Gets vent position based on distribution pattern
        /// </summary>
        private Vector3Int GetVentPosition(int ventIndex)
        {
            switch (Config.VentDistribution)
            {
                case VentDistribution.Central:
                    return new Vector3Int(Config.GridWidth / 2, 0, Config.GridDepth / 2);

                case VentDistribution.Distributed:
                    int spacing = Config.GridWidth / (Config.NumberOfVents + 1);
                    return new Vector3Int(spacing * (ventIndex + 1), 0, Config.GridDepth / 2);

                case VentDistribution.Random:
                    var random = new Random();
                    return new Vector3Int(
                        random.Next(0, Config.GridWidth),
                        0,
                        random.Next(0, Config.GridDepth)
                    );

                default:
                    return Config.VentPosition;
            }
        }

        /// <summary>
        /// Starts the simulation
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            TickManager.Start();
        }

        /// <summary>
        /// Stops the simulation
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            TickManager.Stop();
        }

        /// <summary>
        /// Pauses or unpauses the simulation
        /// </summary>
        public void SetPaused(bool paused)
        {
            TickManager.IsPaused = paused;
        }

        /// <summary>
        /// Main simulation loop - processes one tick
        /// Fully integrated version with all systems
        /// </summary>
        public void Update()
        {
            if (!IsRunning || !TickManager.ShouldTick())
                return;

            TickManager.Tick();
            long currentTick = TickManager.CurrentTick;

            // === COMPLETE SIMULATION LOOP ===

            // Step 1: Generate new tokens from thermal vents
            GenerateTokens();

            // Step 2: Update all token physics (energy, motion, collision)
            UpdatePhysics();

            // Step 3: Apply altitude-based damage to tokens
            ApplyDamage(currentTick);

            // Step 4: Process bonding opportunities in active cells
            ProcessBonding(currentTick);

            // Step 5: Process repulsion between incompatible tokens
            ProcessRepulsion();

            // Step 6: Update chain stabilities and validate
            UpdateChains(currentTick);

            // Step 7: Apply gravity and redistribution
            ApplyGravityAndRedistribution();

            // Step 8: Remove dead tokens and clean up chains
            RemoveInactiveTokens();

            // Step 9: Enforce token limit
            EnforceTokenLimit();

            // Step 10: Update metrics and statistics
            UpdateMetrics(currentTick);
        }

        /// <summary>
        /// Step 1: Generate new tokens from thermal vents
        /// </summary>
        private void GenerateTokens()
        {
            foreach (var vent in ThermalVents)
            {
                var token = vent.GenerateToken(TickManager.CurrentTick);
                if (token != null)
                {
                    Grid.AddToken(token);
                    ActiveTokens.Add(token);
                    TotalTokensGenerated++;
                }
            }
        }

        /// <summary>
        /// Step 2: Update all token physics
        /// </summary>
        private void UpdatePhysics()
        {
            // Update energy
            EnergyManager.UpdateTokenEnergy(ActiveTokens);

            // Update motion
            MotionController.UpdateTokenMotion(ActiveTokens);

            // Detect collisions
            CollisionDetector.DetectAndHandleCollisions();
        }

        /// <summary>
        /// Step 3: Apply altitude-based damage
        /// </summary>
        private void ApplyDamage(long currentTick)
        {
            DamageSystem.ProcessTokenDamage(ActiveTokens, currentTick);
        }

        /// <summary>
        /// Step 4: Process bonding opportunities
        /// </summary>
        private void ProcessBonding(long currentTick)
        {
            // Process bonding in active cells only (optimization)
            var results = CellProcessor.ProcessActiveCells(Grid, currentTick);

            // Track bonds formed
            foreach (var result in results)
            {
                TotalBondsFormed += result.BondsFormed;
            }
        }

        /// <summary>
        /// Step 5: Process repulsion
        /// </summary>
        private void ProcessRepulsion()
        {
            // Repulsion is handled within CellProcessor.ProcessActiveCells
            // This is a placeholder for future standalone repulsion logic
        }

        /// <summary>
        /// Step 6: Update chain stabilities and validate
        /// </summary>
        private void UpdateChains(long currentTick)
        {
            // Update stability scores for all chains
            ChainRegistry.UpdateAllStabilities(currentTick);

            // Prune stale chains periodically (every 100 ticks)
            if (currentTick % 100 == 0)
            {
                ChainRegistry.PruneStaleChains(currentTick, maxAge: 1000);
            }
        }

        /// <summary>
        /// Step 7: Apply gravity and redistribute overflow
        /// </summary>
        private void ApplyGravityAndRedistribution()
        {
            GravitySimulator.ApplyGravity(ActiveTokens);
            GravitySimulator.HandleOverflow();
        }

        /// <summary>
        /// Step 8: Remove inactive tokens
        /// </summary>
        private void RemoveInactiveTokens()
        {
            var tokensToRemove = ActiveTokens.Where(t => !t.IsActive || t.Position.Y < 0).ToList();

            foreach (var token in tokensToRemove)
            {
                // Remove from bonds
                if (token.BondedTokens.Count > 0)
                {
                    var bondedCopy = new List<Token>(token.BondedTokens);
                    foreach (var bonded in bondedCopy)
                    {
                        BondingManager.BreakBond(token, bonded, TickManager.CurrentTick);
                        TotalBondsBroken++;
                    }
                }

                Grid.RemoveToken(token);
                ActiveTokens.Remove(token);
                TokenPool.ReleaseToken(token);
                TotalTokensDestroyed++;
            }
        }

        /// <summary>
        /// Step 9: Enforces maximum token limit
        /// </summary>
        private void EnforceTokenLimit()
        {
            while (ActiveTokens.Count > Config.MaxActiveTokens)
            {
                // Remove oldest falling tokens first
                var tokenToRemove = ActiveTokens
                    .Where(t => t.Energy == 0)
                    .OrderBy(t => t.Position.Y)
                    .FirstOrDefault();

                if (tokenToRemove != null)
                {
                    Grid.RemoveToken(tokenToRemove);
                    ActiveTokens.Remove(tokenToRemove);
                    TokenPool.ReleaseToken(tokenToRemove);
                    TotalTokensDestroyed++;
                }
                else
                {
                    break; // No more falling tokens to remove
                }
            }
        }

        /// <summary>
        /// Step 10: Update metrics and statistics
        /// </summary>
        private void UpdateMetrics(long currentTick)
        {
            // Capture statistics snapshot periodically (every 10 ticks)
            if (currentTick % 10 == 0)
            {
                Statistics.CaptureSnapshot(currentTick);
                Analytics.RecordSnapshot(this, currentTick);
            }

            // Perform comprehensive chain analysis periodically (every 50 ticks)
            if (currentTick % 50 == 0)
            {
                PerformChainAnalysis();
            }
        }

        /// <summary>
        /// Performs comprehensive AST and semantic analysis on all chains
        /// </summary>
        private void PerformChainAnalysis()
        {
            try
            {
                var allChains = ChainRegistry.GetAllChains();
                if (allChains != null && allChains.Any())
                {
                    var results = ChainAnalyzer.AnalyzeMultipleChains(allChains);
                    lock (_analysisLock)
                    {
                        _latestAnalysis = results;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail to avoid disrupting simulation
                System.Diagnostics.Debug.WriteLine($"Chain analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets the simulation
        /// </summary>
        public void Reset()
        {
            // Clear all tokens
            foreach (var token in ActiveTokens.ToList())
            {
                Grid.RemoveToken(token);
                TokenPool.ReleaseToken(token);
            }
            ActiveTokens.Clear();

            // Reset systems
            TickManager.Reset();
            ChainRegistry.Clear();
            Statistics.ClearHistory();
            Analytics.Clear();

            // Reset counters
            TotalTokensGenerated = 0;
            TotalTokensDestroyed = 0;
            TotalBondsFormed = 0;
            TotalBondsBroken = 0;
        }

        /// <summary>
        /// Gets comprehensive simulation statistics
        /// </summary>
        public IntegratedStatsSummary GetStatistics()
        {
            var snapshot = Statistics.GetLatestSnapshot();
            var chainStats = ChainRegistry.GetStatistics();

            return new IntegratedStatsSummary
            {
                CurrentTick = TickManager.CurrentTick,
                TicksPerSecond = TickManager.ActualTicksPerSecond,

                // Token stats
                ActiveTokenCount = ActiveTokens.Count,
                TotalGenerated = TotalTokensGenerated,
                TotalDestroyed = TotalTokensDestroyed,
                DamagedTokens = snapshot?.DamagedTokens ?? 0,

                // Energy stats
                AverageEnergy = snapshot?.AverageEnergy ?? 0,
                TotalEnergy = snapshot?.TotalEnergy ?? 0,

                // Bond stats
                TotalBonds = snapshot?.TotalBonds ?? 0,
                TotalBondsFormed = TotalBondsFormed,
                TotalBondsBroken = TotalBondsBroken,

                // Chain stats
                TotalChains = chainStats.TotalChains,
                StableChains = chainStats.StableChains,
                ValidChains = chainStats.ValidChains,
                LongestChainLength = chainStats.LongestChainLength,
                AverageChainLength = chainStats.AverageLength,

                // System stats
                ActiveCellCount = Grid.ActiveCells.Count
            };
        }

        /// <summary>
        /// Gets the longest valid chain
        /// </summary>
        public TokenChain GetLongestChain()
        {
            return ChainRegistry.GetLongestChain();
        }

        /// <summary>
        /// Gets the most stable chain
        /// </summary>
        public TokenChain GetMostStableChain()
        {
            return ChainRegistry.GetMostStableChain();
        }

        /// <summary>
        /// Exports statistics to CSV
        /// </summary>
        public string ExportStatisticsToCSV()
        {
            return Statistics.ExportToCSV();
        }

        /// <summary>
        /// Exports comprehensive analytics to JSON
        /// </summary>
        public string ExportAnalyticsToJSON()
        {
            return Analytics.ExportToJSON();
        }

        /// <summary>
        /// Exports comprehensive analytics to CSV
        /// </summary>
        public string ExportAnalyticsToCSV()
        {
            return Analytics.ExportComprehensiveCSV();
        }

        /// <summary>
        /// Saves the current simulation state to a JSON file
        /// </summary>
        public void SaveToFile(string filePath, string description = "")
        {
            var state = CaptureState(description);
            var json = System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Captures the current simulation state without saving to file
        /// </summary>
        public SimulationState CaptureState(string description = "")
        {
            var builder = new SimulationStateBuilder()
                .WithMetadata(TickManager.CurrentTick, description)
                .WithConfiguration(Config)
                .WithTokens(ActiveTokens)
                .WithGrid(Grid);

            // Add chains if available
            var allChains = ChainRegistry.GetAllChains();
            if (allChains != null && allChains.Any())
            {
                builder.WithChains(allChains);
            }

            // Add statistics if available
            var snapshot = Statistics.GetLatestSnapshot();
            if (snapshot != null)
            {
                builder.WithStatistics(snapshot);
            }

            // Add custom metadata
            builder.WithCustomData("TotalTokensGenerated", TotalTokensGenerated.ToString());
            builder.WithCustomData("TotalTokensDestroyed", TotalTokensDestroyed.ToString());
            builder.WithCustomData("TotalBondsFormed", TotalBondsFormed.ToString());
            builder.WithCustomData("TotalBondsBroken", TotalBondsBroken.ToString());

            return builder.Build();
        }

        /// <summary>
        /// Loads a simulation state from a JSON file
        /// NOTE: This creates a new simulation from the saved state.
        /// The current simulation instance should be disposed after loading.
        /// </summary>
        public static IntegratedSimulationEngine LoadFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new System.IO.FileNotFoundException($"Simulation state file not found: {filePath}");
            }

            var json = System.IO.File.ReadAllText(filePath);
            var state = System.Text.Json.JsonSerializer.Deserialize<SimulationState>(json);

            if (state == null || !state.IsValid())
            {
                throw new InvalidOperationException("Invalid simulation state file");
            }

            return RestoreFromState(state);
        }

        /// <summary>
        /// Restores a simulation from a captured state
        /// </summary>
        public static IntegratedSimulationEngine RestoreFromState(SimulationState state)
        {
            // Reconstruct configuration
            var config = ReconstructConfig(state.Configuration);

            // Create new simulation with the restored configuration
            var simulation = new IntegratedSimulationEngine(config);

            // Restore tokens
            if (state.Tokens != null)
            {
                foreach (var tokenState in state.Tokens)
                {
                    var token = simulation.TokenFactory.CreateToken(
                        tokenState.Type,
                        tokenState.Value,
                        new Vector3Int(tokenState.Position.X, tokenState.Position.Y, tokenState.Position.Z)
                    );

                    if (token != null)
                    {
                        token.Energy = tokenState.Energy;
                        token.Velocity = new Vector3Int(tokenState.Velocity.X, tokenState.Velocity.Y, tokenState.Velocity.Z);
                        token.IsActive = tokenState.IsActive;
                        token.IsDamaged = tokenState.IsDamaged;
                        token.DamageLevel = tokenState.DamageLevel;

                        simulation.Grid.AddToken(token);
                        simulation.ActiveTokens.Add(token);
                    }
                }
            }

            // Restore statistics counters from metadata
            if (state.Metadata?.CustomData != null)
            {
                if (state.Metadata.CustomData.TryGetValue("TotalTokensGenerated", out var gen))
                    simulation.TotalTokensGenerated = long.Parse(gen);
                if (state.Metadata.CustomData.TryGetValue("TotalTokensDestroyed", out var dest))
                    simulation.TotalTokensDestroyed = long.Parse(dest);
                if (state.Metadata.CustomData.TryGetValue("TotalBondsFormed", out var formed))
                    simulation.TotalBondsFormed = long.Parse(formed);
                if (state.Metadata.CustomData.TryGetValue("TotalBondsBroken", out var broken))
                    simulation.TotalBondsBroken = long.Parse(broken);
            }

            // Restore tick count
            if (state.Metadata != null)
            {
                simulation.TickManager.SetCurrentTick(state.Metadata.CurrentTick);
            }

            // Note: Bonds/chains will need to be re-established through simulation
            // This is intentional as the bonding state may depend on current positions

            return simulation;
        }

        /// <summary>
        /// Reconstructs a SimulationConfig from saved state
        /// </summary>
        private static SimulationConfig ReconstructConfig(SimulationConfigState configState)
        {
            // Start with a default config and override with saved values
            var config = new SimulationConfig
            {
                GridWidth = configState.GridWidth,
                GridHeight = configState.GridHeight,
                GridDepth = configState.GridDepth,
                CellCapacity = configState.CellCapacity,
                MaxActiveTokens = configState.MaxTokens,
                TicksPerSecond = configState.TicksPerSecond
            };

            return config;
        }

        /// <summary>
        /// Gets real-time dashboard data
        /// </summary>
        public DashboardData GetDashboardData()
        {
            return Analytics.GetDashboardData();
        }

        /// <summary>
        /// Analyzes all current chains with comprehensive AST and semantic validation
        /// </summary>
        public List<ComprehensiveAnalysisResult> AnalyzeAllChains()
        {
            var allChains = ChainRegistry.GetAllChains();
            return ChainAnalyzer.AnalyzeMultipleChains(allChains);
        }

        /// <summary>
        /// Gets only fully valid chains (grammar + AST + semantics)
        /// </summary>
        public List<TokenChain> GetFullyValidChains()
        {
            var allChains = ChainRegistry.GetAllChains();
            return ChainAnalyzer.GetFullyValidChains(allChains);
        }

        /// <summary>
        /// Gets the best quality chains sorted by quality score
        /// </summary>
        public List<ComprehensiveAnalysisResult> GetBestQualityChains(int topN = 10)
        {
            var analysis = AnalyzeAllChains();
            return analysis.Take(topN).ToList();
        }

        /// <summary>
        /// Gets the latest cached analysis results (updated every 50 ticks)
        /// </summary>
        public List<ComprehensiveAnalysisResult> GetLatestAnalysis()
        {
            lock (_analysisLock)
            {
                return new List<ComprehensiveAnalysisResult>(_latestAnalysis);
            }
        }

        public override string ToString()
        {
            return $"IntegratedSimulation(Tick:{TickManager.CurrentTick}, " +
                   $"Tokens:{ActiveTokens.Count}, " +
                   $"Chains:{ChainRegistry.Count}, " +
                   $"Longest:{ChainRegistry.GetLongestChain()?.Length ?? 0})";
        }

        /// <summary>
        /// Disposes of all resources used by the simulation engine
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Stop simulation
                if (IsRunning)
                {
                    Stop();
                }

                // Clear and release all tokens
                if (ActiveTokens != null)
                {
                    foreach (var token in ActiveTokens.ToList())
                    {
                        Grid?.RemoveToken(token);
                        TokenPool?.ReleaseToken(token);
                    }
                    ActiveTokens.Clear();
                }

                // Clear all systems
                TokenPool?.Clear();
                ChainRegistry?.Clear();
                Grid?.Clear();

                // Clear statistics
                Statistics?.ClearHistory();

                // Clear thermal vents
                ThermalVents?.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~IntegratedSimulationEngine()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Comprehensive statistics summary for integrated simulation
    /// </summary>
    public class IntegratedStatsSummary
    {
        // Time
        public long CurrentTick { get; set; }
        public double TicksPerSecond { get; set; }

        // Tokens
        public int ActiveTokenCount { get; set; }
        public long TotalGenerated { get; set; }
        public long TotalDestroyed { get; set; }
        public int DamagedTokens { get; set; }

        // Energy
        public double AverageEnergy { get; set; }
        public int TotalEnergy { get; set; }

        // Bonds
        public int TotalBonds { get; set; }
        public long TotalBondsFormed { get; set; }
        public long TotalBondsBroken { get; set; }

        // Chains
        public int TotalChains { get; set; }
        public int StableChains { get; set; }
        public int ValidChains { get; set; }
        public int LongestChainLength { get; set; }
        public double AverageChainLength { get; set; }

        // System
        public int ActiveCellCount { get; set; }

        public override string ToString()
        {
            return $"[Tick {CurrentTick}] " +
                   $"Tokens: {ActiveTokenCount} ({DamagedTokens} dmg) | " +
                   $"Chains: {TotalChains} ({StableChains} stable, Longest: {LongestChainLength}) | " +
                   $"Bonds: {TotalBonds} ({TotalBondsFormed} formed, {TotalBondsBroken} broken) | " +
                   $"Energy: {TotalEnergy} (avg {AverageEnergy:F1}) | " +
                   $"TPS: {TicksPerSecond:F1}";
        }
    }
}

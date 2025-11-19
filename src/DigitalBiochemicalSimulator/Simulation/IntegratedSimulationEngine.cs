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

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Fully integrated simulation engine with all systems.
    /// Orchestrates physics, chemistry, grammar, bonding, damage, and chains.
    /// </summary>
    public class IntegratedSimulationEngine
    {
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
            BondStrengthCalculator = new BondStrengthCalculator(BondRulesEngine);
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

        public override string ToString()
        {
            return $"IntegratedSimulation(Tick:{TickManager.CurrentTick}, " +
                   $"Tokens:{ActiveTokens.Count}, " +
                   $"Chains:{ChainRegistry.Count}, " +
                   $"Longest:{ChainRegistry.GetLongestChain()?.Length ?? 0})";
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

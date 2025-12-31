using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Physics;
using DigitalBiochemicalSimulator.Utilities;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Main simulation engine that orchestrates all subsystems.
    /// Based on section 4.1 main simulation loop of the design specification.
    /// Implements IDisposable for proper resource cleanup.
    /// </summary>
    public class SimulationEngine : IDisposable
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

        // State
        public List<Token> ActiveTokens { get; private set; }
        public bool IsRunning { get; private set; }

        // Statistics
        public long TotalTokensGenerated { get; private set; }
        public long TotalTokensDestroyed { get; private set; }

        public SimulationEngine(SimulationConfig config)
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

            // Initialize thermal vents
            ThermalVents = new List<ThermalVent>();
            CreateThermalVents();

            // State
            ActiveTokens = new List<Token>();
            IsRunning = false;

            // Statistics
            TotalTokensGenerated = 0;
            TotalTokensDestroyed = 0;

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
                    return new Vector3Int(Config.GridWidth / 2, Config.GridHeight / 2, 0);

                case VentDistribution.Distributed:
                    int spacing = Config.GridWidth / (Config.NumberOfVents + 1);
                    return new Vector3Int(spacing * (ventIndex + 1), Config.GridHeight / 2, 0);

                case VentDistribution.Random:
                    var random = new Random();
                    return new Vector3Int(
                        random.Next(0, Config.GridWidth),
                        random.Next(0, Config.GridHeight),
                        0
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
        /// Based on section 4.1 simulationLoop algorithm
        /// </summary>
        public void Update()
        {
            if (!IsRunning || !TickManager.ShouldTick())
                return;

            TickManager.Tick();

            // Step 1: Generate new tokens from thermal vents
            GenerateTokens();

            // Step 2: Update all token physics
            UpdatePhysics();

            // Step 3: Process bonding opportunities (placeholder for Phase 3)
            // ProcessBonding();

            // Step 4: Process repulsion (placeholder for Phase 3)
            // ProcessRepulsion();

            // Step 5: Validate stable chains (placeholder for Phase 3)
            // ValidateChains();

            // Step 6: Apply gravity and redistribution
            ApplyGravityAndRedistribution();

            // Step 7: Remove dead tokens
            RemoveInactiveTokens();

            // Step 8: Update statistics (placeholder)
            // UpdateMetrics();

            // Limit active tokens
            EnforceTokenLimit();
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
        /// Step 6: Apply gravity and redistribute overflow
        /// </summary>
        private void ApplyGravityAndRedistribution()
        {
            GravitySimulator.ApplyGravity(ActiveTokens);
            GravitySimulator.HandleOverflow();
        }

        /// <summary>
        /// Step 7: Remove inactive tokens
        /// </summary>
        private void RemoveInactiveTokens()
        {
            var tokensToRemove = ActiveTokens.Where(t => !t.IsActive || t.Position.Z < 0).ToList();

            foreach (var token in tokensToRemove)
            {
                Grid.RemoveToken(token);
                ActiveTokens.Remove(token);
                TokenPool.ReleaseToken(token);
                TotalTokensDestroyed++;
            }
        }

        /// <summary>
        /// Enforces maximum token limit
        /// </summary>
        private void EnforceTokenLimit()
        {
            while (ActiveTokens.Count > Config.MaxActiveTokens)
            {
                // Remove oldest falling tokens first
                var tokenToRemove = ActiveTokens
                    .Where(t => t.IsFalling)
                    .OrderBy(t => t.Position.Z)
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
            TotalTokensGenerated = 0;
            TotalTokensDestroyed = 0;

            // Reset thermal vents
            foreach (var vent in ThermalVents)
            {
                // Vents will reset their internal counters
            }
        }

        /// <summary>
        /// Gets simulation statistics
        /// </summary>
        public SimulationStatsSummary GetStatistics()
        {
            return new SimulationStatsSummary
            {
                CurrentTick = TickManager.CurrentTick,
                TicksPerSecond = TickManager.ActualTicksPerSecond,
                ActiveTokenCount = ActiveTokens.Count,
                TotalGenerated = TotalTokensGenerated,
                TotalDestroyed = TotalTokensDestroyed,
                AverageEnergy = EnergyManager.GetAverageEnergy(ActiveTokens),
                ActiveCellCount = Grid.ActiveCells.Count
            };
        }

        public override string ToString()
        {
            return $"Simulation(Tick:{TickManager.CurrentTick}, Tokens:{ActiveTokens.Count}/{Config.MaxActiveTokens})";
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
                Grid?.Clear();

                // Clear thermal vents
                ThermalVents?.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~SimulationEngine()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Summary of simulation statistics
    /// </summary>
    public class SimulationStatsSummary
    {
        public long CurrentTick { get; set; }
        public double TicksPerSecond { get; set; }
        public int ActiveTokenCount { get; set; }
        public long TotalGenerated { get; set; }
        public long TotalDestroyed { get; set; }
        public float AverageEnergy { get; set; }
        public int ActiveCellCount { get; set; }

        public override string ToString()
        {
            return $"Tick {CurrentTick} | Tokens: {ActiveTokenCount} | Generated: {TotalGenerated} | " +
                   $"Destroyed: {TotalDestroyed} | Avg Energy: {AverageEnergy:F1} | TPS: {TicksPerSecond:F1}";
        }
    }
}

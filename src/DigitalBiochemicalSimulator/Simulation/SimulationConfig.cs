using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Configuration parameters for the simulation.
    /// Based on section 5.1 of the design specification.
    /// </summary>
    public class SimulationConfig
    {
        // Grid Configuration
        public int GridWidth { get; set; } = 50;
        public int GridHeight { get; set; } = 50;
        public int GridDepth { get; set; } = 50;
        public int CellCapacity { get; set; } = 1000;

        // Physics Configuration
        public int InitialTokenEnergy { get; set; } = 50;
        public int EnergyPerTick { get; set; } = 1;
        public int RiseRate { get; set; } = 1;  // cells per tick
        public int FallRate { get; set; } = 1;  // cells per tick
        public bool GravityEnabled { get; set; } = true;
        public int EnvironmentViscosity { get; set; } = 1;  // energy needed to move up

        // Thermal Vent Configuration
        public int VentEmissionRate { get; set; } = 10;  // ticks per token
        public Vector3Int VentPosition { get; set; } = new Vector3Int(25, 25, 0);
        public int NumberOfVents { get; set; } = 1;
        public VentDistribution VentDistribution { get; set; } = VentDistribution.Central;

        // Bonding Configuration
        public float MinBondStrength { get; set; } = 0.3f;
        public int BondingRadius { get; set; } = 0;  // same cell only
        public int EnergyPerBond { get; set; } = 1;  // per token in chain minus 1

        // Bond strength multipliers (1.0 = default from grammar)
        public float CovalentBondStrength { get; set; } = 1.0f;
        public float IonicBondStrength { get; set; } = 1.0f;
        public float VanDerWaalsBondStrength { get; set; } = 1.0f;

        // Damage Configuration
        public float BaseDamageRate { get; set; } = 0.01f;
        public float DamageExponent { get; set; } = 2.0f;
        public float CriticalDamageThreshold { get; set; } = 0.8f;
        public float DamageIncrement { get; set; } = 0.1f;
        public int MutationRange { get; set; } = 10;  // cells from top
        public int MutationRate { get; set; } = 1;  // attempts per tick
        public MutationFalloff MutationFalloff { get; set; } = MutationFalloff.Gradual;

        // Validation Configuration
        public int MinValidationLength { get; set; } = 3;
        public int StabilityThreshold { get; set; } = 10;  // ticks without bonding
        public int ValidationFrequency { get; set; } = 5;  // check every N ticks

        // Performance Configuration
        public int MaxActiveTokens { get; set; } = 1000;
        public int MaxChains { get; set; } = 200;

        // At MaxActiveTokens, vents replace the oldest unbonded token instead of stopping,
        // so new material keeps arriving
        public bool RecycleIdleTokens { get; set; } = true;

        // Replication: each tick, a chain that has passed validation and is at least StabilityThreshold
        // ticks old copies itself with this probability, from free tokens within ReplicationRange cells
        // (0 disables replication)
        public float ReplicationRate { get; set; } = 0.05f;
        public int ReplicationRange { get; set; } = 2;

        // Probability that a copy takes a token of a different type at one position (copying error)
        public float ReplicationErrorRate { get; set; } = 0.05f;

        // Fitter chains (ChainFitness) replicate proportionally more often; the average rate stays ReplicationRate
        public bool FitnessWeightedReplication { get; set; } = true;

        // Chain turnover: chance per tick that a chain breaks down and returns its tokens (0 = never;
        // 0.0005 gives an average chain life of about 2000 ticks)
        public float ChainDecayRate { get; set; } = 0.0005f;

        // Fitter chains (ChainFitness) decay proportionally more slowly; an average chain decays at ChainDecayRate
        public bool FitnessWeightedDecay { get; set; } = true;

        // Ticks a copy may spend gathering tokens before it is abandoned and its tokens released
        public int ReplicationTimeout { get; set; } = 100;
        public int TicksPerSecond { get; set; } = 10;
        public int TickDuration => TicksPerSecond > 0 ? 1000 / TicksPerSecond : 1000;  // milliseconds, from TicksPerSecond

        /// <summary>
        /// Alias for MaxActiveTokens
        /// </summary>
        public int MaxTokens
        {
            get => MaxActiveTokens;
            set => MaxActiveTokens = value;
        }

        // Tokens each vent emits per emission (every VentEmissionRate ticks)
        public int TokenGenerationRate { get; set; } = 1;

        // Multiplier on FallRate for falling tokens (fractional cells per tick are applied probabilistically)
        public float GravityStrength { get; set; } = 1.0f;

        // Euclidean distance in cells within which tokens in different cells can bond (0 = same cell only)
        public float BondingRange { get; set; } = 0.0f;

        // Multiplier on BaseDamageRate
        public float DamageMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Bonding distance used by the simulation: the larger of BondingRadius and BondingRange
        /// </summary>
        public float EffectiveBondingRange => Math.Max(BondingRadius, BondingRange);

        // Token Distribution (probabilities for each token category)
        public TokenDistributionProfile TokenDistribution { get; set; } =
            TokenDistributionProfile.Balanced;

        // Simulation State
        public long CurrentTick { get; set; } = 0;
        public bool IsPaused { get; set; } = false;

        /// <summary>
        /// Creates a configuration with default values
        /// </summary>
        public SimulationConfig()
        {
        }

        /// <summary>
        /// Creates a configuration with the given grid dimensions and default values
        /// </summary>
        public SimulationConfig(int gridWidth, int gridHeight, int gridDepth)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            GridDepth = gridDepth;
        }

        /// <summary>
        /// Creates a configuration from the Standard preset
        /// </summary>
        public static SimulationConfig CreateStandard() => SimulationPresets.Standard;

        /// <summary>
        /// Creates a configuration from the Minimal preset
        /// </summary>
        public static SimulationConfig CreateMinimal() => SimulationPresets.Minimal;

        /// <summary>
        /// Creates a copy of this configuration
        /// </summary>
        public SimulationConfig Clone()
        {
            return (SimulationConfig)this.MemberwiseClone();
        }

        /// <summary>
        /// Validates the configuration parameters
        /// </summary>
        public bool Validate(out string? errorMessage)
        {
            // Validate positive dimensions
            if (GridWidth <= 0 || GridHeight <= 0 || GridDepth <= 0)
            {
                errorMessage = $"Grid width, height, and depth must be positive (current: {GridWidth}×{GridHeight}×{GridDepth})";
                return false;
            }

            // Validate maximum individual dimensions
            const int MAX_DIMENSION = 1000;
            if (GridWidth > MAX_DIMENSION || GridHeight > MAX_DIMENSION || GridDepth > MAX_DIMENSION)
            {
                errorMessage = $"Grid is too large: dimensions cannot exceed {MAX_DIMENSION} (current: {GridWidth}×{GridHeight}×{GridDepth})";
                return false;
            }

            // Validate total grid size
            const long MAX_CELLS = 10_000_000; // 10 million cells
            long totalCells = (long)GridWidth * GridHeight * GridDepth;
            if (totalCells > MAX_CELLS)
            {
                errorMessage = $"Total grid cells ({totalCells:N0}) exceeds maximum ({MAX_CELLS:N0}). " +
                              $"Consider using a smaller grid or implementing sparse grid optimization.";
                return false;
            }

            // Validate estimated memory usage
            const long MAX_MEMORY_MB = 8192; // 8 GB
            long estimatedMemoryMB = (totalCells * 400) / (1024 * 1024); // ~400 bytes per cell
            if (estimatedMemoryMB > MAX_MEMORY_MB)
            {
                errorMessage = $"Grid size would require approximately {estimatedMemoryMB:N0} MB of memory " +
                              $"(maximum: {MAX_MEMORY_MB:N0} MB). Please reduce grid size.";
                return false;
            }

            if (CellCapacity <= 0)
            {
                errorMessage = "Cell capacity must be positive";
                return false;
            }

            if (InitialTokenEnergy < 0)
            {
                errorMessage = "Initial token energy cannot be negative";
                return false;
            }

            if (MinBondStrength < 0.0f || MinBondStrength > 1.0f)
            {
                errorMessage = "Min bond strength must be between 0.0 and 1.0";
                return false;
            }

            if (BaseDamageRate < 0.0f || BaseDamageRate > 1.0f)
            {
                errorMessage = "Base damage rate must be between 0.0 and 1.0";
                return false;
            }

            if (CriticalDamageThreshold < 0.0f || CriticalDamageThreshold > 1.0f)
            {
                errorMessage = "Critical damage threshold must be between 0.0 and 1.0";
                return false;
            }

            if (MaxActiveTokens < 0)
            {
                errorMessage = "Max active tokens cannot be negative";
                return false;
            }

            if (TicksPerSecond <= 0)
            {
                errorMessage = "Ticks per second must be positive";
                return false;
            }

            if (NumberOfVents <= 0)
            {
                errorMessage = "Number of vents must be positive";
                return false;
            }

            if (NumberOfVents > 100)
            {
                errorMessage = "Number of vents cannot exceed 100 (performance limit)";
                return false;
            }

            if (VentEmissionRate <= 0)
            {
                errorMessage = "Vent emission rate must be positive";
                return false;
            }

            if (EnergyPerTick < 0)
            {
                errorMessage = "Energy per tick cannot be negative";
                return false;
            }

            if (EnergyPerBond < 0)
            {
                errorMessage = "Energy per bond cannot be negative";
                return false;
            }

            if (RiseRate <= 0)
            {
                errorMessage = "Rise rate must be positive";
                return false;
            }

            if (FallRate <= 0)
            {
                errorMessage = "Fall rate must be positive";
                return false;
            }

            if (MutationRange < 0)
            {
                errorMessage = "Mutation range cannot be negative";
                return false;
            }

            if (MutationRate < 0)
            {
                errorMessage = "Mutation rate cannot be negative";
                return false;
            }

            if (DamageExponent < 0)
            {
                errorMessage = "Damage exponent cannot be negative";
                return false;
            }

            if (MaxChains < 0)
            {
                errorMessage = "Max chains cannot be negative";
                return false;
            }

            if (TokenGenerationRate <= 0)
            {
                errorMessage = "Token generation rate must be positive";
                return false;
            }

            if (GravityStrength < 0.0f)
            {
                errorMessage = "Gravity strength cannot be negative";
                return false;
            }

            if (BondingRadius < 0 || BondingRange < 0.0f)
            {
                errorMessage = "Bonding radius and range cannot be negative";
                return false;
            }

            if (DamageMultiplier < 0.0f)
            {
                errorMessage = "Damage multiplier cannot be negative";
                return false;
            }

            if (DamageIncrement < 0.0f || DamageIncrement > 1.0f)
            {
                errorMessage = "Damage increment must be between 0.0 and 1.0";
                return false;
            }

            if (ReplicationRate < 0.0f || ReplicationRate > 1.0f ||
                ReplicationErrorRate < 0.0f || ReplicationErrorRate > 1.0f)
            {
                errorMessage = "Replication rate and replication error rate must be between 0.0 and 1.0";
                return false;
            }

            if (ChainDecayRate < 0.0f || ChainDecayRate > 1.0f)
            {
                errorMessage = "Chain decay rate must be between 0.0 and 1.0";
                return false;
            }

            if (ReplicationTimeout < 1)
            {
                errorMessage = "Replication timeout must be at least 1 tick";
                return false;
            }

            if (ReplicationRange < 0 || ReplicationRange > 10)
            {
                errorMessage = "Replication range must be between 0 and 10";
                return false;
            }

            if (MinValidationLength < 0 || StabilityThreshold < 0 || ValidationFrequency < 0)
            {
                errorMessage = "Validation settings cannot be negative";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }

    /// <summary>
    /// Distribution pattern for thermal vents
    /// </summary>
    public enum VentDistribution
    {
        Central,      // Single vent in center
        Distributed,  // Evenly distributed across grid
        Distant,      // Vents at grid corners
        Clustered,    // Vents in clusters
        Random,       // Random positions
        Manual        // User-specified positions
    }

    /// <summary>
    /// How mutation rate changes with altitude
    /// </summary>
    public enum MutationFalloff
    {
        Gradual,  // Rate decreases gradually from top to bottom
        Abrupt    // Rate stays constant until mutation range limit
    }

    /// <summary>
    /// Token distribution profiles for different simulation goals
    /// </summary>
    public enum TokenDistributionProfile
    {
        Balanced,          // Equal distribution
        ExpressionHeavy,   // More operators and literals
        ControlHeavy,      // More keywords and control structures
        Custom             // User-defined distribution
    }

    /// <summary>
    /// Preset configurations for different simulation scenarios
    /// Based on section 5.3 of the design specification
    /// </summary>
    public static class SimulationPresets
    {
        public static SimulationConfig Minimal => new SimulationConfig
        {
            GridWidth = 10,
            GridHeight = 10,
            GridDepth = 10,
            VentEmissionRate = 20,
            MaxActiveTokens = 100,
            TokenDistribution = TokenDistributionProfile.ExpressionHeavy
        };

        public static SimulationConfig Standard => new SimulationConfig
        {
            GridWidth = 50,
            GridHeight = 50,
            GridDepth = 50,
            VentEmissionRate = 10,
            MaxActiveTokens = 500,
            TokenDistribution = TokenDistributionProfile.Balanced
        };

        public static SimulationConfig Complex => new SimulationConfig
        {
            GridWidth = 100,
            GridHeight = 100,
            GridDepth = 100,
            VentEmissionRate = 5,
            MaxActiveTokens = 2000,
            TokenDistribution = TokenDistributionProfile.ControlHeavy
        };

        public static SimulationConfig ExpressionEvolution => new SimulationConfig
        {
            TokenDistribution = TokenDistributionProfile.ExpressionHeavy,
            DamageExponent = 1.5f,  // gentler damage
            CovalentBondStrength = 1.2f,
            IonicBondStrength = 1.2f
        };

        public static SimulationConfig HarshSelection => new SimulationConfig
        {
            DamageExponent = 3.0f,
            CriticalDamageThreshold = 0.5f,
            VentEmissionRate = 20,  // high competition
            BaseDamageRate = 0.02f
        };
    }
}

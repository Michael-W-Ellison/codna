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
        public int TickDuration { get; set; } = 100;  // milliseconds
        public int TicksPerSecond { get; set; } = 10;

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
            if (GridWidth <= 0 || GridHeight <= 0 || GridDepth <= 0)
            {
                errorMessage = "Grid dimensions must be positive";
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

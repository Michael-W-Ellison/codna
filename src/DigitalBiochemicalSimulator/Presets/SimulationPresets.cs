using System;
using System.Collections.Generic;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Presets
{
    /// <summary>
    /// Pre-configured simulation presets showcasing different emergent behaviors.
    /// Each preset is tuned for specific characteristics and phenomena.
    /// </summary>
    public static class SimulationPresets
    {
        /// <summary>
        /// Gets all available preset configurations
        /// </summary>
        public static Dictionary<string, SimulationConfig> GetAllPresets()
        {
            return new Dictionary<string, SimulationConfig>
            {
                { "RapidEvolution", RapidEvolution() },
                { "StableFormation", StableFormation() },
                { "HighPressure", HighPressure() },
                { "SparseExploration", SparseExploration() },
                { "MicroEvolution", MicroEvolution() },
                { "ChaoticDynamics", ChaoticDynamics() },
                { "CompetitiveSelection", CompetitiveSelection() },
                { "CooperativeBuilding", CooperativeBuilding() },
                { "MinimalistSimulation", MinimalistSimulation() },
                { "MaximumComplexity", MaximumComplexity() }
            };
        }

        /// <summary>
        /// Rapid Evolution Preset
        ///
        /// Characteristics:
        /// - Fast-paced dynamics
        /// - High energy environment
        /// - Rapid chain formation and destruction
        /// - High mutation rate
        /// - Short-lived structures
        ///
        /// Best for:
        /// - Quick experiments
        /// - Testing grammar rules
        /// - Observing rapid dynamics
        /// - Short demo sessions
        /// </summary>
        public static SimulationConfig RapidEvolution()
        {
            return new SimulationConfig(30, 30, 30)
            {
                MaxTokens = 200,
                TokenGenerationRate = 10,
                GravityStrength = 0.5f,
                CellCapacity = 20,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// Stable Formation Preset
        ///
        /// Characteristics:
        /// - Slow, deliberate evolution
        /// - Low energy input
        /// - Long-lived stable chains
        /// - Bottom-heavy distribution
        /// - Strong selection for stability
        ///
        /// Best for:
        /// - Observing stable structures
        /// - Long-term simulations
        /// - Studying chain persistence
        /// - Publication-quality results
        /// </summary>
        public static SimulationConfig StableFormation()
        {
            return new SimulationConfig(60, 60, 60)
            {
                MaxTokens = 300,
                TokenGenerationRate = 2,
                GravityStrength = 1.5f,
                CellCapacity = 8,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// High Pressure Preset
        ///
        /// Characteristics:
        /// - Extreme altitude damage gradient
        /// - Strong evolutionary pressure
        /// - Only robust chains survive
        /// - Concentrated evolution at bottom
        /// - Natural selection showcase
        ///
        /// Best for:
        /// - Studying adaptation
        /// - Observing Darwinian selection
        /// - Finding robust structures
        /// - Evolutionary dynamics research
        /// </summary>
        public static SimulationConfig HighPressure()
        {
            return new SimulationConfig(40, 100, 40)
            {
                MaxTokens = 500,
                TokenGenerationRate = 5,
                GravityStrength = 2.0f,
                CellCapacity = 10,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// Sparse Exploration Preset
        ///
        /// Characteristics:
        /// - Large spatial volume
        /// - Low token density
        /// - Isolated evolution pockets
        /// - Diverse parallel evolution
        /// - Spatial clustering
        ///
        /// Best for:
        /// - Studying spatial patterns
        /// - Parallel evolution
        /// - Cluster formation
        /// - Geographic isolation effects
        /// </summary>
        public static SimulationConfig SparseExploration()
        {
            return new SimulationConfig(100, 50, 100)
            {
                MaxTokens = 1000,
                TokenGenerationRate = 3,
                GravityStrength = 0.8f,
                CellCapacity = 5,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// Micro Evolution Preset
        ///
        /// Characteristics:
        /// - Very small scale
        /// - Easy to track individuals
        /// - Slow motion for observation
        /// - Detailed dynamics visible
        /// - Educational focus
        ///
        /// Best for:
        /// - Demonstrations
        /// - Teaching
        /// - Understanding mechanics
        /// - Debugging
        /// - Step-by-step observation
        /// </summary>
        public static SimulationConfig MicroEvolution()
        {
            return new SimulationConfig(20, 20, 20)
            {
                MaxTokens = 50,
                TokenGenerationRate = 1,
                GravityStrength = 1.0f,
                CellCapacity = 10,
                TicksPerSecond = 10  // Slow motion
            };
        }

        /// <summary>
        /// Chaotic Dynamics Preset
        ///
        /// Characteristics:
        /// - Maximum chaos and unpredictability
        /// - Very high energy input
        /// - Constant flux
        /// - No stable equilibrium
        /// - Emergent complexity from chaos
        ///
        /// Best for:
        /// - Studying chaos theory
        /// - Observing emergence from disorder
        /// - Stress testing
        /// - Demonstrating self-organization
        /// </summary>
        public static SimulationConfig ChaoticDynamics()
        {
            return new SimulationConfig(40, 40, 40)
            {
                MaxTokens = 600,
                TokenGenerationRate = 20,
                GravityStrength = 0.3f,
                CellCapacity = 30,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// Competitive Selection Preset
        ///
        /// Characteristics:
        /// - Strong competition for resources
        /// - High damage pressure
        /// - Survival of the fittest
        /// - Tournament-style evolution
        /// - Winner-take-all dynamics
        ///
        /// Best for:
        /// - Studying competition
        /// - Observing arms races
        /// - Finding optimal structures
        /// - Game theory research
        /// </summary>
        public static SimulationConfig CompetitiveSelection()
        {
            return new SimulationConfig(50, 120, 50)
            {
                MaxTokens = 600,
                TokenGenerationRate = 8,
                GravityStrength = 2.5f,
                CellCapacity = 10,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// Cooperative Building Preset
        ///
        /// Characteristics:
        /// - Encourages token clustering
        /// - Low competitive pressure
        /// - Enables large chain formation
        /// - Cooperative bonding favored
        /// - Gentle evolutionary landscape
        ///
        /// Best for:
        /// - Studying cooperation
        /// - Building complex structures
        /// - Long chain formation
        /// - Symbiotic relationships
        /// </summary>
        public static SimulationConfig CooperativeBuilding()
        {
            return new SimulationConfig(50, 50, 50)
            {
                MaxTokens = 500,
                TokenGenerationRate = 5,
                GravityStrength = 1.0f,
                CellCapacity = 25,
                TicksPerSecond = 60
            };
        }

        /// <summary>
        /// Minimalist Simulation Preset
        ///
        /// Characteristics:
        /// - Smallest viable simulation
        /// - Ultra-fast performance
        /// - Simple dynamics
        /// - Easy to understand
        /// - Quick iterations
        ///
        /// Best for:
        /// - Testing
        /// - Quick prototyping
        /// - Performance benchmarking
        /// - Regression testing
        /// - CI/CD pipelines
        /// </summary>
        public static SimulationConfig MinimalistSimulation()
        {
            return new SimulationConfig(20, 20, 20)
            {
                MaxTokens = 50,
                TokenGenerationRate = 2,
                GravityStrength = 1.0f,
                CellCapacity = 5,
                TicksPerSecond = 120
            };
        }

        /// <summary>
        /// Maximum Complexity Preset
        ///
        /// Characteristics:
        /// - Largest viable simulation
        /// - Maximum emergent complexity
        /// - Rich interaction networks
        /// - High computational demand
        /// - Research-grade complexity
        ///
        /// Best for:
        /// - Final experiments
        /// - Publication results
        /// - Maximum realism
        /// - Complex emergence
        /// - High-end hardware only
        /// </summary>
        public static SimulationConfig MaximumComplexity()
        {
            return new SimulationConfig(100, 100, 100)
            {
                MaxTokens = 2000,
                TokenGenerationRate = 10,
                GravityStrength = 1.0f,
                CellCapacity = 15,
                TicksPerSecond = 30  // Lower target for stability
            };
        }

        /// <summary>
        /// Gets preset description
        /// </summary>
        public static string GetPresetDescription(string presetName)
        {
            return presetName switch
            {
                "RapidEvolution" => "Fast-paced dynamics with rapid chain formation and destruction",
                "StableFormation" => "Slow, deliberate evolution producing stable long-lived chains",
                "HighPressure" => "Extreme selection pressure for robust structures",
                "SparseExploration" => "Large spatial volume with isolated evolution pockets",
                "MicroEvolution" => "Small-scale slow-motion for detailed observation",
                "ChaoticDynamics" => "Maximum chaos and emergent self-organization",
                "CompetitiveSelection" => "Strong competition and survival of the fittest",
                "CooperativeBuilding" => "Encourages cooperation and large structure formation",
                "MinimalistSimulation" => "Smallest viable simulation for quick testing",
                "MaximumComplexity" => "Largest simulation with maximum emergent complexity",
                _ => "Unknown preset"
            };
        }

        /// <summary>
        /// Gets recommended run duration for preset (in ticks)
        /// </summary>
        public static int GetRecommendedDuration(string presetName)
        {
            return presetName switch
            {
                "RapidEvolution" => 2000,
                "StableFormation" => 10000,
                "HighPressure" => 5000,
                "SparseExploration" => 8000,
                "MicroEvolution" => 1000,
                "ChaoticDynamics" => 3000,
                "CompetitiveSelection" => 5000,
                "CooperativeBuilding" => 7000,
                "MinimalistSimulation" => 1000,
                "MaximumComplexity" => 5000,
                _ => 5000
            };
        }

        /// <summary>
        /// Gets expected performance (TPS) for preset on typical hardware
        /// </summary>
        public static string GetExpectedPerformance(string presetName)
        {
            return presetName switch
            {
                "RapidEvolution" => "80-120 TPS",
                "StableFormation" => "60-100 TPS",
                "HighPressure" => "50-80 TPS",
                "SparseExploration" => "30-60 TPS",
                "MicroEvolution" => "120+ TPS",
                "ChaoticDynamics" => "40-70 TPS",
                "CompetitiveSelection" => "50-80 TPS",
                "CooperativeBuilding" => "60-90 TPS",
                "MinimalistSimulation" => "150+ TPS",
                "MaximumComplexity" => "10-30 TPS",
                _ => "Unknown"
            };
        }
    }
}

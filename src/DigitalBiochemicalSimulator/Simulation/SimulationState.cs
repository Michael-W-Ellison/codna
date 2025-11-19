using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Represents the complete state of a simulation at a point in time.
    /// Can be serialized to JSON for saving/loading.
    /// </summary>
    public class SimulationState
    {
        public StateMetadata Metadata { get; set; }
        public SimulationConfigState Configuration { get; set; }
        public List<TokenState> Tokens { get; set; }
        public List<ChainState> Chains { get; set; }
        public GridState Grid { get; set; }
        public StatisticsState Statistics { get; set; }

        public SimulationState()
        {
            Metadata = new StateMetadata();
            Tokens = new List<TokenState>();
            Chains = new List<ChainState>();
        }

        /// <summary>
        /// Validates the state for completeness
        /// </summary>
        public bool IsValid()
        {
            return Metadata != null &&
                   Configuration != null &&
                   Tokens != null &&
                   Grid != null;
        }

        public override string ToString()
        {
            return $"SimulationState(Tick: {Metadata?.CurrentTick}, Tokens: {Tokens?.Count}, Chains: {Chains?.Count})";
        }
    }

    /// <summary>
    /// Metadata about the saved state
    /// </summary>
    public class StateMetadata
    {
        public string Version { get; set; } = "1.0";
        public DateTime SavedAt { get; set; } = DateTime.Now;
        public long CurrentTick { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> CustomData { get; set; }

        public StateMetadata()
        {
            CustomData = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Serializable version of SimulationConfig
    /// </summary>
    public class SimulationConfigState
    {
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int GridDepth { get; set; }
        public int CellCapacity { get; set; }
        public int MaxTokens { get; set; }
        public int TokenGenerationRate { get; set; }
        public int TicksPerSecond { get; set; }
        public float GravityStrength { get; set; }
        public Dictionary<string, object> AdditionalSettings { get; set; }

        public SimulationConfigState()
        {
            AdditionalSettings = new Dictionary<string, object>();
        }

        public static SimulationConfigState FromConfig(SimulationConfig config)
        {
            return new SimulationConfigState
            {
                GridWidth = config.GridWidth,
                GridHeight = config.GridHeight,
                GridDepth = config.GridDepth,
                CellCapacity = config.CellCapacity,
                MaxTokens = config.MaxTokens,
                TokenGenerationRate = config.TokenGenerationRate,
                TicksPerSecond = config.TicksPerSecond,
                GravityStrength = config.GravityStrength
            };
        }
    }

    /// <summary>
    /// Serializable version of Token
    /// </summary>
    public class TokenState
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public Vector3IntState Position { get; set; }
        public Vector3IntState Velocity { get; set; }
        public int Energy { get; set; }
        public int Mass { get; set; }
        public bool IsActive { get; set; }
        public float DamageLevel { get; set; }
        public List<long> BondedTokenIds { get; set; }
        public TokenMetadataState Metadata { get; set; }
        public long ChainId { get; set; }
        public int ChainPosition { get; set; }

        public TokenState()
        {
            BondedTokenIds = new List<long>();
        }

        public static TokenState FromToken(Token token)
        {
            return new TokenState
            {
                Id = token.Id,
                Type = token.Type.ToString(),
                Value = token.Value,
                Position = new Vector3IntState { X = token.Position.X, Y = token.Position.Y, Z = token.Position.Z },
                Velocity = new Vector3IntState { X = token.Velocity.X, Y = token.Velocity.Y, Z = token.Velocity.Z },
                Energy = token.Energy,
                Mass = token.Mass,
                IsActive = token.IsActive,
                DamageLevel = token.DamageLevel,
                BondedTokenIds = token.BondedTokens.Select(t => t.Id).ToList(),
                Metadata = token.Metadata != null ? TokenMetadataState.FromMetadata(token.Metadata) : null,
                ChainId = token.ChainId,
                ChainPosition = token.ChainPosition
            };
        }
    }

    /// <summary>
    /// Serializable version of TokenMetadata
    /// </summary>
    public class TokenMetadataState
    {
        public string Category { get; set; }
        public string Subtype { get; set; }
        public string Role { get; set; }
        public float Electronegativity { get; set; }
        public int BondCapacity { get; set; }

        public static TokenMetadataState FromMetadata(TokenMetadata metadata)
        {
            return new TokenMetadataState
            {
                Category = metadata.Category,
                Subtype = metadata.Subtype,
                Role = metadata.Role,
                Electronegativity = metadata.Electronegativity,
                BondCapacity = metadata.BondCapacity
            };
        }
    }

    /// <summary>
    /// Serializable version of Vector3Int
    /// </summary>
    public class Vector3IntState
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(X, Y, Z);
        }
    }

    /// <summary>
    /// Serializable version of TokenChain
    /// </summary>
    public class ChainState
    {
        public long Id { get; set; }
        public List<long> TokenIds { get; set; }
        public long HeadTokenId { get; set; }
        public long TailTokenId { get; set; }
        public int Length { get; set; }
        public float StabilityScore { get; set; }
        public float AverageBondStrength { get; set; }
        public bool IsValid { get; set; }
        public string BondType { get; set; }
        public long CreatedAt { get; set; }
        public long LastModifiedAt { get; set; }

        public ChainState()
        {
            TokenIds = new List<long>();
        }

        public static ChainState FromChain(TokenChain chain)
        {
            return new ChainState
            {
                Id = chain.Id,
                TokenIds = chain.Tokens.Select(t => t.Id).ToList(),
                HeadTokenId = chain.Head?.Id ?? 0,
                TailTokenId = chain.Tail?.Id ?? 0,
                Length = chain.Length,
                StabilityScore = chain.StabilityScore,
                AverageBondStrength = chain.AverageBondStrength,
                IsValid = chain.IsValid,
                BondType = chain.BondType.ToString(),
                CreatedAt = chain.CreatedAt,
                LastModifiedAt = chain.LastModifiedAt
            };
        }
    }

    /// <summary>
    /// Serializable version of Grid state
    /// </summary>
    public class GridState
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public int Capacity { get; set; }
        public List<CellState> OccupiedCells { get; set; }

        public GridState()
        {
            OccupiedCells = new List<CellState>();
        }
    }

    /// <summary>
    /// Serializable version of Cell
    /// </summary>
    public class CellState
    {
        public Vector3IntState Position { get; set; }
        public List<long> TokenIds { get; set; }
        public bool IsActive { get; set; }

        public CellState()
        {
            TokenIds = new List<long>();
        }
    }

    /// <summary>
    /// Serializable statistics snapshot
    /// </summary>
    public class StatisticsState
    {
        public long CurrentTick { get; set; }
        public int TotalTokens { get; set; }
        public int ActiveTokens { get; set; }
        public int TotalChains { get; set; }
        public int StableChains { get; set; }
        public int ValidChains { get; set; }
        public float AverageEnergy { get; set; }
        public float AverageStability { get; set; }
        public double TicksPerSecond { get; set; }

        public static StatisticsState FromSnapshot(StatisticsSnapshot snapshot)
        {
            return new StatisticsState
            {
                CurrentTick = snapshot.Tick,
                TotalTokens = snapshot.TotalTokens,
                ActiveTokens = snapshot.ActiveTokens,
                TotalChains = snapshot.TotalChains,
                StableChains = snapshot.StableChains,
                ValidChains = snapshot.ValidChains,
                AverageEnergy = (float)snapshot.AverageEnergy,
                AverageStability = snapshot.AverageChainStability,
                TicksPerSecond = snapshot.TicksPerSecond
            };
        }
    }

    /// <summary>
    /// Builder for creating SimulationState from running simulation
    /// </summary>
    public class SimulationStateBuilder
    {
        private SimulationState _state;

        public SimulationStateBuilder()
        {
            _state = new SimulationState();
        }

        public SimulationStateBuilder WithMetadata(long currentTick, string description = "")
        {
            _state.Metadata = new StateMetadata
            {
                CurrentTick = currentTick,
                SavedAt = DateTime.Now,
                Description = description
            };
            return this;
        }

        public SimulationStateBuilder WithConfiguration(SimulationConfig config)
        {
            _state.Configuration = SimulationConfigState.FromConfig(config);
            return this;
        }

        public SimulationStateBuilder WithTokens(List<Token> tokens)
        {
            _state.Tokens = tokens.Select(TokenState.FromToken).ToList();
            return this;
        }

        public SimulationStateBuilder WithChains(List<TokenChain> chains)
        {
            _state.Chains = chains.Select(ChainState.FromChain).ToList();
            return this;
        }

        public SimulationStateBuilder WithGrid(Grid grid)
        {
            _state.Grid = new GridState
            {
                Width = grid.Width,
                Height = grid.Height,
                Depth = grid.Depth,
                Capacity = grid.CellCapacity
            };

            // Only save occupied cells to reduce file size
            var occupiedCells = new List<CellState>();
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    for (int z = 0; z < grid.Depth; z++)
                    {
                        var cell = grid.GetCell(new Vector3Int(x, y, z));
                        if (cell != null && cell.Tokens.Count > 0)
                        {
                            occupiedCells.Add(new CellState
                            {
                                Position = new Vector3IntState { X = x, Y = y, Z = z },
                                TokenIds = cell.Tokens.Select(t => t.Id).ToList(),
                                IsActive = cell.IsActive
                            });
                        }
                    }
                }
            }

            _state.Grid.OccupiedCells = occupiedCells;
            return this;
        }

        public SimulationStateBuilder WithStatistics(StatisticsSnapshot snapshot)
        {
            _state.Statistics = StatisticsState.FromSnapshot(snapshot);
            return this;
        }

        public SimulationStateBuilder WithCustomData(string key, string value)
        {
            _state.Metadata.CustomData[key] = value;
            return this;
        }

        public SimulationState Build()
        {
            if (!_state.IsValid())
            {
                throw new InvalidOperationException("SimulationState is incomplete. Ensure all required components are set.");
            }
            return _state;
        }
    }
}

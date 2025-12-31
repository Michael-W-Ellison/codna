# Getting Started with Digital Biochemical Simulator

Welcome to the Digital Biochemical Simulator! This guide will help you set up, configure, and run your first simulation of emergent code formation.

## Table of Contents

1. [Quick Start](#quick-start)
2. [System Requirements](#system-requirements)
3. [Installation](#installation)
4. [Your First Simulation](#your-first-simulation)
5. [Understanding the Simulation](#understanding-the-simulation)
6. [Monitoring and Analysis](#monitoring-and-analysis)
7. [Saving and Loading](#saving-and-loading)
8. [Next Steps](#next-steps)

## Quick Start

```csharp
// Create configuration
var config = new SimulationConfig(
    gridWidth: 50,
    gridHeight: 50,
    gridDepth: 50
);

// Create and run simulation
var simulation = new IntegratedSimulationEngine(config);
simulation.Start();

// Run for 1000 ticks
for (int i = 0; i < 1000; i++)
{
    simulation.Update();
}

// Get statistics
var stats = simulation.Statistics.CaptureSnapshot(simulation.TickManager.CurrentTick);
Console.WriteLine($"Active tokens: {stats.ActiveTokens}");
Console.WriteLine($"Chains formed: {stats.TotalChains}");
```

## System Requirements

### Minimum Requirements
- **OS:** Windows 10 or later
- **.NET:** .NET 6.0 SDK or later
- **RAM:** 4 GB
- **Storage:** 100 MB

### Recommended Requirements
- **OS:** Windows 11
- **.NET:** .NET 6.0 or later
- **RAM:** 8 GB or more
- **Storage:** 500 MB
- **CPU:** Multi-core processor for better performance

## Installation

### From Source

1. **Clone the repository:**
```bash
git clone https://github.com/yourusername/codna.git
cd codna
```

2. **Build the project:**
```bash
cd src/DigitalBiochemicalSimulator
dotnet build
```

3. **Run tests (optional):**
```bash
cd ../DigitalBiochemicalSimulator.Tests
dotnet test
```

### Using NuGet (Future)

```bash
dotnet add package DigitalBiochemicalSimulator
```

## Your First Simulation

### Step 1: Create Configuration

The `SimulationConfig` class controls all simulation parameters:

```csharp
var config = new SimulationConfig(
    gridWidth: 50,      // 3D grid X dimension
    gridHeight: 50,     // 3D grid Y dimension (vertical)
    gridDepth: 50       // 3D grid Z dimension
)
{
    MaxTokens = 500,              // Maximum number of tokens
    TokenGenerationRate = 5,       // Tokens spawned per tick
    TicksPerSecond = 60,          // Simulation speed
    CellCapacity = 10,            // Max tokens per cell
    GravityStrength = 1.0f        // Downward force
};
```

### Step 2: Initialize Simulation

```csharp
using DigitalBiochemicalSimulator.Simulation;

var simulation = new IntegratedSimulationEngine(config);
```

### Step 3: Start Simulation

```csharp
simulation.Start();

// Simulation loop
while (simulation.IsRunning)
{
    simulation.Update();

    // Optional: Check if target reached
    if (simulation.TickManager.CurrentTick >= 1000)
    {
        simulation.Stop();
    }
}
```

### Step 4: Monitor Progress

```csharp
// Capture current statistics
var stats = simulation.Statistics.CaptureSnapshot(
    simulation.TickManager.CurrentTick
);

// Display metrics
Console.WriteLine($"Tick: {stats.Tick}");
Console.WriteLine($"Total Tokens: {stats.TotalTokens}");
Console.WriteLine($"Active Tokens: {stats.ActiveTokens}");
Console.WriteLine($"Total Chains: {stats.TotalChains}");
Console.WriteLine($"Stable Chains: {stats.StableChains}");
Console.WriteLine($"Valid Chains: {stats.ValidChains}");
Console.WriteLine($"Average Energy: {stats.AverageEnergy:F2}");
Console.WriteLine($"TPS: {stats.TicksPerSecond:F1}");
```

## Understanding the Simulation

### Core Concepts

#### 1. **Tokens**
Programming tokens (literals, operators, keywords) with physical properties:
- **Position:** 3D location in grid
- **Velocity:** Movement vector
- **Energy:** Powers movement and bonding
- **Mass:** Affects motion
- **Damage:** Accumulates at high altitude

#### 2. **Thermal Vents**
Located at bottom of grid (Y=0), they:
- Generate new tokens periodically
- Provide initial upward energy
- Create evolutionary pressure

#### 3. **Bonding**
Tokens form chemical-like bonds based on:
- **Grammar rules:** Syntax compatibility
- **Electronegativity:** Chemical affinity
- **Energy cost:** Requires sufficient energy
- **Spatial proximity:** Tokens must be nearby

#### 4. **Chains**
Bonded tokens form chains that:
- Represent code structures (expressions, statements)
- Have stability scores
- Can be validated for syntax correctness
- May split or merge

#### 5. **Damage System**
Altitude-based damage creates evolutionary pressure:
- Higher altitude = more damage
- Damaged tokens may lose bonds
- Critically damaged tokens destroyed
- Encourages stable structures at bottom

### Simulation Phases

Each tick processes these steps in order:

1. **Token Generation:** Vents create new tokens
2. **Physics Update:** Energy decay, motion, collisions
3. **Damage Application:** Altitude-based corruption
4. **Bonding:** Attempt new bonds in active cells
5. **Repulsion:** Push incompatible tokens apart
6. **Chain Updates:** Recalculate stability scores
7. **Gravity:** Apply downward force
8. **Cleanup:** Remove inactive tokens
9. **Limit Enforcement:** Respect MaxTokens
10. **Statistics:** Capture metrics

## Monitoring and Analysis

### Real-Time Statistics

Access current state at any time:

```csharp
var currentStats = simulation.Statistics.CaptureSnapshot(
    simulation.TickManager.CurrentTick
);

// Population metrics
Console.WriteLine($"Population: {currentStats.ActiveTokens}/{currentStats.TotalTokens}");
Console.WriteLine($"Damaged: {currentStats.DamagedTokens}");

// Chain metrics
Console.WriteLine($"Chains: {currentStats.TotalChains}");
Console.WriteLine($"Valid: {currentStats.ValidChains}");
Console.WriteLine($"Longest: {currentStats.LongestChainLength}");
```

### Time-Series Tracking

Analyze trends over time:

```csharp
var tracker = simulation.Statistics.TimeSeriesTracker;

// Get statistics for a metric
var popStats = tracker.GetStatistics("ActiveTokens");
Console.WriteLine($"Population - Avg: {popStats.Average:F1}, Min: {popStats.Min}, Max: {popStats.Max}");

// Analyze trends
var trend = tracker.AnalyzeTrend("TotalChains", windowSize: 100);
Console.WriteLine($"Chain formation trend: {trend.Trend}");
Console.WriteLine($"Growth rate: {trend.Slope:F4}");
```

### Export Data for Analysis

```csharp
// Export all metrics to CSV
var csv = tracker.ExportToCSV();
File.WriteAllText("simulation_data.csv", csv);

// Or export specific metrics
var specificCsv = tracker.ExportMetricsToCSV(
    "ActiveTokens",
    "TotalChains",
    "AverageStability"
);
File.WriteAllText("key_metrics.csv", specificCsv);
```

### Query Specific Chains

```csharp
// Get all chains
var chains = simulation.ChainRegistry.GetAllChains();

// Get longest chain
var longest = simulation.ChainRegistry.GetLongestChain();
if (longest != null)
{
    Console.WriteLine($"Longest chain: {longest.ToCodeString()}");
    Console.WriteLine($"  Length: {longest.Length}");
    Console.WriteLine($"  Stability: {longest.StabilityScore:F2}");
    Console.WriteLine($"  Valid: {longest.IsValid}");
}

// Get most stable chains
var stable = simulation.ChainRegistry.GetMostStableChains(10);
foreach (var chain in stable)
{
    Console.WriteLine($"Stable chain: {chain.ToCodeString()} ({chain.StabilityScore:F2})");
}

// Get valid (syntactically correct) chains
var valid = simulation.ChainRegistry.GetValidChains();
Console.WriteLine($"Found {valid.Count} syntactically valid chains");
```

## Saving and Loading

### Saving Simulation State

```csharp
using DigitalBiochemicalSimulator.Utilities;

var saveManager = new SaveLoadManager();

// Capture current state
var state = new SimulationStateBuilder()
    .WithMetadata(
        simulation.TickManager.CurrentTick,
        "Checkpoint before mutation event"
    )
    .WithConfiguration(simulation.Config)
    .WithTokens(simulation.AllTokens)
    .WithChains(simulation.ChainRegistry.GetAllChains())
    .WithGrid(simulation.Grid)
    .WithStatistics(simulation.Statistics.CaptureSnapshot(
        simulation.TickManager.CurrentTick
    ))
    .Build();

// Save to file
var result = saveManager.Save(state, "checkpoint_1000");
Console.WriteLine(result.ToString());
```

### Loading Simulation State

```csharp
// Load saved state
var loadResult = saveManager.Load("checkpoint_1000");

if (loadResult.Success)
{
    var loadedState = loadResult.State;
    Console.WriteLine($"Loaded state from tick {loadedState.Metadata.CurrentTick}");
    Console.WriteLine($"Contains {loadedState.Tokens.Count} tokens");
    Console.WriteLine($"Contains {loadedState.Chains.Count} chains");

    // Reconstruct simulation from state
    // (requires custom reconstruction logic)
}
```

### List Available Saves

```csharp
var saves = saveManager.ListSaves();
Console.WriteLine($"Found {saves.Length} save files:");

foreach (var saveName in saves)
{
    var info = saveManager.GetSaveInfo(saveName);
    Console.WriteLine($"  {info.FileName}:");
    Console.WriteLine($"    Tick: {info.CurrentTick}");
    Console.WriteLine($"    Tokens: {info.TokenCount}");
    Console.WriteLine($"    Chains: {info.ChainCount}");
    Console.WriteLine($"    Size: {info.FileSizeFormatted}");
}
```

## Next Steps

### Learn More

- **[Parameter Tuning Guide](ParameterTuning.md):** Configure for different emergent behaviors
- **[API Reference](API.md):** Detailed class and method documentation
- **[Architecture Overview](Architecture.md):** System design and components

### Experiment

Try these experiments:

1. **High Energy:**
   - Increase `TokenGenerationRate`
   - Observe rapid chain formation

2. **Low Gravity:**
   - Decrease `GravityStrength`
   - Tokens stay aloft longer

3. **Large Grid:**
   - Increase grid dimensions to 100x100x100
   - Observe emergent clustering

4. **Long Runs:**
   - Run for 100,000+ ticks
   - Analyze stability trends

### Get Help

- **Issues:** Report bugs on GitHub Issues
- **Discussions:** Ask questions on GitHub Discussions
- **Contributing:** See CONTRIBUTING.md

## Common Issues

### Low Performance

**Symptom:** Simulation runs slower than expected

**Solutions:**
- Reduce `MaxTokens` (try 200-500)
- Decrease grid size (try 30x30x30)
- Lower `TokenGenerationRate`
- Check performance tests: `dotnet test --filter Performance`

### No Chain Formation

**Symptom:** Chains don't form or break immediately

**Solutions:**
- Increase token energy (modify `EnergyManager`)
- Reduce damage rate (lower altitude exposure)
- Check grammar rules are configured
- Verify bond strength calculations

### Memory Issues

**Symptom:** High memory usage or out-of-memory errors

**Solutions:**
- Reduce `MaxTokens`
- Enable periodic cleanup
- Check for memory leaks with stress tests
- Monitor with: `GC.GetTotalMemory(false)`

## Example: Complete Simulation

Here's a complete example that runs a simulation and analyzes results:

```csharp
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.Utilities;

// Configure simulation
var config = new SimulationConfig(40, 40, 40)
{
    MaxTokens = 300,
    TokenGenerationRate = 3,
    TicksPerSecond = 60
};

// Create simulation
var sim = new IntegratedSimulationEngine(config);
sim.Start();

// Run simulation
Console.WriteLine("Running simulation...");
for (int i = 0; i < 5000; i++)
{
    sim.Update();

    // Log every 1000 ticks
    if (i % 1000 == 0)
    {
        var stats = sim.Statistics.CaptureSnapshot(sim.TickManager.CurrentTick);
        Console.WriteLine($"Tick {i}: {stats.ActiveTokens} tokens, {stats.TotalChains} chains");
    }
}

// Final analysis
var finalStats = sim.Statistics.CaptureSnapshot(sim.TickManager.CurrentTick);
Console.WriteLine("\n=== Final Results ===");
Console.WriteLine($"Total ticks: {finalStats.Tick}");
Console.WriteLine($"Active tokens: {finalStats.ActiveTokens}");
Console.WriteLine($"Total chains: {finalStats.TotalChains}");
Console.WriteLine($"Valid chains: {finalStats.ValidChains}");
Console.WriteLine($"Average stability: {finalStats.AverageChainStability:F2}");

// Find interesting chains
var validChains = sim.ChainRegistry.GetValidChains();
Console.WriteLine($"\nFound {validChains.Count} syntactically valid chains:");
foreach (var chain in validChains.Take(5))
{
    Console.WriteLine($"  {chain.ToCodeString()}");
}

// Export data
var csv = sim.Statistics.TimeSeriesTracker.ExportToCSV();
File.WriteAllText("results.csv", csv);
Console.WriteLine("\nData exported to results.csv");

// Save final state
var saveManager = new SaveLoadManager();
var state = new SimulationStateBuilder()
    .WithMetadata(sim.TickManager.CurrentTick, "Final state")
    .WithConfiguration(config)
    .WithTokens(sim.AllTokens)
    .WithChains(sim.ChainRegistry.GetAllChains())
    .WithGrid(sim.Grid)
    .Build();

var saveResult = saveManager.Save(state, "final_state");
Console.WriteLine($"State saved: {saveResult.FileSizeFormatted}");
```

---

**Ready to explore emergent code formation? Start experimenting!**

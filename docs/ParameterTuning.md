# Parameter Tuning Guide

This guide explains how to configure the Digital Biochemical Simulator to achieve different emergent behaviors, from rapid chaotic evolution to slow stable code formation.

## Table of Contents

1. [Core Parameters](#core-parameters)
2. [Preset Configurations](#preset-configurations)
3. [Tuning for Specific Behaviors](#tuning-for-specific-behaviors)
4. [Advanced Tuning](#advanced-tuning)
5. [Performance vs. Behavior Trade-offs](#performance-vs-behavior-trade-offs)
6. [Troubleshooting](#troubleshooting)

## Core Parameters

### Grid Parameters

#### `GridWidth`, `GridHeight`, `GridDepth`

**Default:** 50x50x50

Defines the 3D simulation space.

**Effects:**
- **Larger grids** → More spatial diversity, slower collision rate
- **Smaller grids** → Higher collision rate, faster bonding
- **Tall grids** (high Y) → More altitude-based damage gradient
- **Flat grids** (low Y) → Less evolutionary pressure

**Recommendations:**
```csharp
// Rapid evolution
var config = new SimulationConfig(30, 20, 30);  // Compact, low height

// Slow, stable evolution
var config = new SimulationConfig(100, 100, 100);  // Spacious

// High pressure environment
var config = new SimulationConfig(50, 100, 50);  // Tall, strong gradient
```

### Population Parameters

#### `MaxTokens`

**Default:** 500

Maximum number of tokens allowed in simulation.

**Effects:**
- **Higher values** → More interactions, slower performance
- **Lower values** → Fewer interactions, faster performance
- Affects density and collision frequency

**Recommendations:**
```csharp
// Fast testing
config.MaxTokens = 100;

// Standard simulation
config.MaxTokens = 500;

// Complex emergence
config.MaxTokens = 2000;  // Requires good hardware
```

#### `TokenGenerationRate`

**Default:** 5

Number of tokens spawned per tick at thermal vents.

**Effects:**
- **Higher rate** → Rapid population growth, high energy environment
- **Lower rate** → Slow population growth, selective pressure
- Interacts with `MaxTokens` to determine equilibrium

**Recommendations:**
```csharp
// Slow, deliberate formation
config.TokenGenerationRate = 1;

// Standard pace
config.TokenGenerationRate = 5;

// Rapid chaos
config.TokenGenerationRate = 20;
```

### Physics Parameters

#### `GravityStrength`

**Default:** 1.0f

Strength of downward force applied to tokens.

**Effects:**
- **Higher gravity** → Tokens fall faster, cluster at bottom
- **Lower gravity** → Tokens float longer, distributed vertically
- Affects time available for high-altitude bonding

**Recommendations:**
```csharp
// Floating tokens (more time to bond)
config.GravityStrength = 0.3f;

// Standard gravity
config.GravityStrength = 1.0f;

// Rapid settling (bottom-heavy)
config.GravityStrength = 3.0f;
```

#### `CellCapacity`

**Default:** 10

Maximum number of tokens allowed per grid cell.

**Effects:**
- **Higher capacity** → Dense clusters possible
- **Lower capacity** → Forces spatial distribution
- Affects collision detection and bonding opportunities

**Recommendations:**
```csharp
// Sparse distribution
config.CellCapacity = 5;

// Standard clustering
config.CellCapacity = 10;

// Dense packing
config.CellCapacity = 50;
```

### Performance Parameters

#### `TicksPerSecond`

**Default:** 60

Target simulation speed (ticks per second).

**Effects:**
- **Higher TPS** → Faster simulation time, more CPU usage
- **Lower TPS** → Slower simulation, more time per tick
- Actual TPS depends on hardware and token count

**Recommendations:**
```csharp
// Slow motion (easier to observe)
config.TicksPerSecond = 10;

// Real-time
config.TicksPerSecond = 60;

// Fast-forward (if hardware allows)
config.TicksPerSecond = 120;
```

## Preset Configurations

### Rapid Evolution

Fast-paced, chaotic environment with quick chain formation and destruction.

```csharp
var config = new SimulationConfig(30, 30, 30)
{
    MaxTokens = 200,
    TokenGenerationRate = 10,
    GravityStrength = 0.5f,
    CellCapacity = 20,
    TicksPerSecond = 60
};
```

**Characteristics:**
- Dense token population
- High energy availability
- Rapid bonding/unbonding
- Short-lived chains
- High mutation rate

**Best for:**
- Quick experiments
- Testing grammar rules
- Observing rapid dynamics

### Stable Formation

Slow, stable environment favoring long-term chain stability.

```csharp
var config = new SimulationConfig(60, 60, 60)
{
    MaxTokens = 300,
    TokenGenerationRate = 2,
    GravityStrength = 1.5f,
    CellCapacity = 8,
    TicksPerSecond = 60
};
```

**Characteristics:**
- Moderate token population
- Limited new energy input
- Selective bonding
- Long-lived stable chains
- Bottom-heavy distribution

**Best for:**
- Stable code structures
- Long simulations
- Studying chain evolution

### High Pressure

Extreme damage gradient creating strong evolutionary pressure.

```csharp
var config = new SimulationConfig(40, 100, 40)
{
    MaxTokens = 500,
    TokenGenerationRate = 5,
    GravityStrength = 2.0f,
    CellCapacity = 10,
    TicksPerSecond = 60
};
```

**Characteristics:**
- Tall grid with strong altitude gradient
- High damage at top
- Only stable chains survive
- Strong selection pressure
- Concentrated evolution at bottom

**Best for:**
- Studying natural selection
- Observing adaptation
- Finding robust structures

### Sparse Exploration

Large, sparse environment for distributed evolution.

```csharp
var config = new SimulationConfig(100, 50, 100)
{
    MaxTokens = 1000,
    TokenGenerationRate = 3,
    GravityStrength = 0.8f,
    CellCapacity = 5,
    TicksPerSecond = 60
};
```

**Characteristics:**
- Large spatial volume
- Low density
- Isolated evolution pockets
- Diverse structures
- Reduced competition

**Best for:**
- Parallel evolution
- Spatial clustering studies
- Large-scale patterns

### Micro Evolution

Small, controlled environment for detailed observation.

```csharp
var config = new SimulationConfig(20, 20, 20)
{
    MaxTokens = 50,
    TokenGenerationRate = 1,
    GravityStrength = 1.0f,
    CellCapacity = 10,
    TicksPerSecond = 10  // Slow motion
};
```

**Characteristics:**
- Very small scale
- Easy to track individual tokens
- Slow motion for observation
- Detailed dynamics
- Educational demos

**Best for:**
- Demonstrations
- Understanding mechanics
- Debugging
- Teaching

## Tuning for Specific Behaviors

### Goal: Maximum Chain Formation

Optimize for creating many chains quickly.

```csharp
var config = new SimulationConfig(40, 40, 40)
{
    MaxTokens = 800,           // High population
    TokenGenerationRate = 15,   // Rapid input
    GravityStrength = 0.5f,     // Slow falling
    CellCapacity = 25           // Dense clustering
};
```

**Additional tuning:**
- Increase token energy (modify `EnergyManager`)
- Reduce bond energy cost
- Increase bonding range

### Goal: Valid Code Structures

Optimize for syntactically correct chains.

```csharp
var config = new SimulationConfig(50, 80, 50)
{
    MaxTokens = 400,
    TokenGenerationRate = 3,    // Moderate input
    GravityStrength = 1.5f,     // Strong settling
    CellCapacity = 10
};
```

**Additional tuning:**
- Strengthen grammar bonding rules
- Increase stability bonus for valid chains
- Reduce damage for valid structures

### Goal: Long Chain Evolution

Optimize for creating long token chains.

```csharp
var config = new SimulationConfig(50, 50, 50)
{
    MaxTokens = 500,
    TokenGenerationRate = 5,
    GravityStrength = 1.0f,
    CellCapacity = 15           // Allow chain clustering
};
```

**Additional tuning:**
- Increase chain stability bonuses
- Reduce damage for bonded tokens
- Strengthen bond resilience

### Goal: Competitive Evolution

Optimize for Darwinian selection of best chains.

```csharp
var config = new SimulationConfig(50, 120, 50)
{
    MaxTokens = 600,
    TokenGenerationRate = 8,     // High energy input
    GravityStrength = 2.5f,      // Strong gravity
    CellCapacity = 10
};
```

**Additional tuning:**
- Increase damage rate at altitude
- Reduce energy for unbonded tokens
- Increase stability rewards

## Advanced Tuning

### Energy System

While not directly configurable via `SimulationConfig`, you can modify:

**EnergyManager.cs:**
```csharp
// Default energy decay
private const float BASE_DECAY_RATE = 1.0f;

// Modify for different behaviors:
// - Lower (0.5f): Tokens live longer, more bonding time
// - Higher (2.0f): Fast energy depletion, survival pressure
```

### Damage System

**DamageSystem.cs:**
```csharp
// Altitude-based damage scaling
private const float BASE_DAMAGE_RATE = 0.01f;
private const float DAMAGE_EXPONENT = 2.0f;

// Modify for tuning:
// - Higher DAMAGE_RATE: Stronger selection pressure
// - Higher EXPONENT: Steeper altitude gradient
// - Lower values: More forgiving environment
```

### Bond Strength

**BondStrengthCalculator.cs:**
```csharp
// Electronegativity difference threshold
private const float IONIC_THRESHOLD = 0.4f;

// Modify for tuning:
// - Lower threshold: More ionic bonds (weaker)
// - Higher threshold: More covalent bonds (stronger)
```

### Chain Stability

**ChainStabilityCalculator.cs:**
```csharp
// Grammar validity multipliers
private const float VALID_GRAMMAR_BONUS = 1.2f;
private const float INVALID_GRAMMAR_PENALTY = 0.5f;

// Modify for tuning:
// - Higher bonus: Strong preference for valid syntax
// - Lower penalty: More permissive of invalid structures
```

## Performance vs. Behavior Trade-offs

### High Performance Configuration

Maximize simulation speed at the cost of complexity:

```csharp
var config = new SimulationConfig(30, 30, 30)  // Small grid
{
    MaxTokens = 100,              // Low population
    TokenGenerationRate = 2,       // Controlled growth
    TicksPerSecond = 120          // Fast simulation
};
```

**Performance:** 100+ TPS
**Complexity:** Limited emergent behavior

### Balanced Configuration

Balance between performance and interesting behavior:

```csharp
var config = new SimulationConfig(50, 50, 50)
{
    MaxTokens = 500,
    TokenGenerationRate = 5,
    TicksPerSecond = 60
};
```

**Performance:** 30-60 TPS
**Complexity:** Rich emergent behavior

### High Complexity Configuration

Maximum emergent complexity, performance secondary:

```csharp
var config = new SimulationConfig(100, 100, 100)  // Large grid
{
    MaxTokens = 2000,             // High population
    TokenGenerationRate = 10,      // High energy
    TicksPerSecond = 30           // Lower target TPS
};
```

**Performance:** 10-30 TPS (hardware dependent)
**Complexity:** Very rich emergent behavior

## Troubleshooting

### Problem: Chains Form but Immediately Break

**Symptoms:**
- Bonds form but don't persist
- Chain count fluctuates wildly
- Low stability scores

**Solutions:**
```csharp
// Increase energy availability
config.TokenGenerationRate = 10;  // More tokens = more energy

// Reduce gravity
config.GravityStrength = 0.5f;  // More time to stabilize

// Increase cell capacity
config.CellCapacity = 20;  // Allow dense clusters
```

**Code modifications:**
- Reduce energy cost of bonding
- Increase stability bonuses
- Reduce damage rate

### Problem: No Chain Formation

**Symptoms:**
- Bonds rarely form
- Mostly single tokens
- Very low chain count

**Solutions:**
```csharp
// Increase population density
config = new SimulationConfig(30, 30, 30);  // Smaller grid
config.MaxTokens = 500;  // More tokens
config.CellCapacity = 25;  // Dense packing
```

**Code modifications:**
- Reduce bonding distance threshold
- Lower bond strength requirements
- Increase bonding success probability

### Problem: All Tokens Destroyed Quickly

**Symptoms:**
- Population crashes to zero
- Tokens don't survive long
- High damage levels

**Solutions:**
```csharp
// Reduce damage pressure
config.GravityStrength = 0.5f;  // Slower falling
config = new SimulationConfig(50, 30, 50);  // Lower height

// Increase energy
config.TokenGenerationRate = 10;  // More tokens/energy
```

**Code modifications:**
- Reduce `BASE_DAMAGE_RATE`
- Lower `DAMAGE_EXPONENT`
- Increase energy per token

### Problem: Stagnation (No New Chains)

**Symptoms:**
- Same chains persist indefinitely
- No new formations
- Low diversity

**Solutions:**
```csharp
// Increase churn
config.TokenGenerationRate = 15;  // More new tokens
config.MaxTokens = 1000;  // Higher capacity

// Add pressure
config.GravityStrength = 2.0f;  // Force settling
```

**Code modifications:**
- Increase mutation rate
- Add time-based chain decay
- Implement predation (chain removal)

### Problem: Poor Performance

**Symptoms:**
- Low TPS (< 10)
- Lag and stuttering
- High CPU usage

**Solutions:**
```csharp
// Reduce load
config = new SimulationConfig(40, 40, 40);  // Smaller grid
config.MaxTokens = 300;  // Fewer tokens
config.TokenGenerationRate = 3;  // Slower growth

// Lower target
config.TicksPerSecond = 30;  // Reduce target TPS
```

**Optimizations:**
- Use spatial indexing (Octree)
- Enable periodic cleanup
- Reduce statistics capture frequency

## Example Tuning Session

Here's a complete example of iterative tuning to achieve specific behavior:

**Goal:** Create stable, long chains that represent valid expressions

**Iteration 1: Baseline**
```csharp
var config = new SimulationConfig(50, 50, 50)
{
    MaxTokens = 500,
    TokenGenerationRate = 5
};
// Result: Chains form but break quickly
```

**Iteration 2: Increase Stability**
```csharp
config.GravityStrength = 1.5f;  // Stronger settling
config.CellCapacity = 15;        // Better clustering
// Result: Better, but still unstable at high altitude
```

**Iteration 3: Add Selection Pressure**
```csharp
config = new SimulationConfig(50, 80, 50);  // Taller grid
config.GravityStrength = 2.0f;               // Strong settling
// Result: Stable chains at bottom, strong selection
```

**Iteration 4: Optimize Population**
```csharp
config.MaxTokens = 400;          // Moderate population
config.TokenGenerationRate = 4;   // Controlled input
// Result: Stable, valid chains forming consistently
```

**Final Configuration:**
```csharp
var optimizedConfig = new SimulationConfig(50, 80, 50)
{
    MaxTokens = 400,
    TokenGenerationRate = 4,
    GravityStrength = 2.0f,
    CellCapacity = 15,
    TicksPerSecond = 60
};
```

---

**Remember:** The best configuration depends on your goals. Experiment, measure, and iterate!

See [Getting Started](GettingStarted.md) for basic usage and [API Reference](API.md) for detailed parameter descriptions.

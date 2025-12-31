# Performance Optimization Guide

This guide covers performance optimization strategies, profiling techniques, and best practices for the Digital Biochemical Simulator.

## Table of Contents

1. [Performance Overview](#performance-overview)
2. [Profiling Tools](#profiling-tools)
3. [Optimization Strategies](#optimization-strategies)
4. [Common Bottlenecks](#common-bottlenecks)
5. [Benchmarking](#benchmarking)
6. [Memory Optimization](#memory-optimization)
7. [Best Practices](#best-practices)

## Performance Overview

### Current Performance Characteristics

Based on performance tests, the simulator achieves:

**Small Simulations (50 tokens):**
- 100+ TPS (Ticks Per Second)
- < 10ms per tick
- Memory: ~50 MB

**Medium Simulations (500 tokens):**
- 60-100 TPS
- ~10-15ms per tick
- Memory: ~100-200 MB

**Large Simulations (1000+ tokens):**
- 30-60 TPS
- ~20-30ms per tick
- Memory: ~200-500 MB

**Very Large Simulations (2000+ tokens):**
- 10-30 TPS
- ~30-100ms per tick
- Memory: ~500 MB - 1 GB

### Performance Targets

**Minimum Acceptable:**
- 30 TPS for medium simulations (500 tokens)
- 10 TPS for large simulations (1000+ tokens)
- < 1 GB memory usage for typical workloads

**Optimal:**
- 60+ TPS for medium simulations
- 30+ TPS for large simulations
- Efficient memory usage with no leaks

## Profiling Tools

### Built-in Performance Tests

The project includes comprehensive performance tests:

```bash
cd src/DigitalBiochemicalSimulator.Tests
dotnet test --filter Performance
```

**Available Tests:**
- `Performance_1000Tokens_CompletesInReasonableTime`
- `Performance_LargeGrid_HandlesEfficiently`
- `Performance_OctreeQueries_AreFast`
- `Performance_LongRunningSimulation_MaintainsStability`
- `Performance_MemoryUsage_StaysReasonable`

### .NET Profiling Tools

#### dotnet-counters (Real-time Monitoring)

```bash
# Install
dotnet tool install --global dotnet-counters

# Run simulation and get process ID
dotnet run &
PID=$!

# Monitor performance
dotnet-counters monitor --process-id $PID
```

**Key Metrics:**
- CPU Usage
- Memory (Working Set)
- GC Collections (Gen 0, 1, 2)
- Exceptions per second

#### dotnet-trace (Event Tracing)

```bash
# Install
dotnet tool install --global dotnet-trace

# Collect trace
dotnet-trace collect --process-id $PID --providers Microsoft-DotNETCore-SampleProfiler

# Open in Visual Studio or PerfView
```

#### BenchmarkDotNet (Micro-benchmarking)

For detailed micro-benchmarks, use BenchmarkDotNet:

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class SimulationBenchmarks
{
    private IntegratedSimulationEngine _simulation;

    [GlobalSetup]
    public void Setup()
    {
        var config = new SimulationConfig(30, 30, 30);
        _simulation = new IntegratedSimulationEngine(config);
        _simulation.Start();
    }

    [Benchmark]
    public void SingleTick()
    {
        _simulation.Update();
    }
}

// Run with:
// dotnet run -c Release
BenchmarkRunner.Run<SimulationBenchmarks>();
```

### Visual Studio Profiler

1. Open solution in Visual Studio
2. Debug → Performance Profiler
3. Select profiling tools:
   - CPU Usage
   - Memory Usage
   - .NET Object Allocation
4. Start profiling
5. Analyze results

## Optimization Strategies

### 1. Spatial Indexing (Implemented)

**Problem:** O(n²) collision detection and bonding checks

**Solution:** Octree spatial indexing

**Benefits:**
- O(log n) queries instead of O(n)
- 10-100x faster for large simulations
- Efficient neighbor finding

**Implementation:**
```csharp
// Slow: Linear search
foreach (var token in allTokens)
{
    foreach (var other in allTokens)
    {
        if (IsNearby(token, other))
            TryBond(token, other);
    }
}

// Fast: Spatial index
var candidates = spatialIndex.FindTokensInRange(token.Position, bondingRange);
foreach (var other in candidates)
{
    TryBond(token, other);
}
```

### 2. Object Pooling

**Problem:** Frequent token allocation/deallocation causes GC pressure

**Solution:** Implement token pool

**Implementation:**
```csharp
public class TokenPool
{
    private readonly Stack<Token> _availableTokens = new();
    private readonly int _maxSize;

    public TokenPool(int maxSize = 1000)
    {
        _maxSize = maxSize;
    }

    public Token Rent(long id, TokenType type, string value, Vector3Int position)
    {
        Token token;
        if (_availableTokens.Count > 0)
        {
            token = _availableTokens.Pop();
            token.Reset(id, type, value, position);
        }
        else
        {
            token = new Token(id, type, value, position);
        }
        return token;
    }

    public void Return(Token token)
    {
        if (_availableTokens.Count < _maxSize)
        {
            token.Cleanup();
            _availableTokens.Push(token);
        }
    }
}
```

**Benefits:**
- Reduced GC allocations
- Lower GC pause times
- Better memory locality

### 3. Batch Processing

**Problem:** Processing tokens one-by-one has overhead

**Solution:** Process in batches

**Implementation:**
```csharp
// Process tokens in batches
const int batchSize = 100;
var activeCells = grid.GetActiveCells();

Parallel.ForEach(
    activeCells.Batch(batchSize),
    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    batch =>
    {
        foreach (var cell in batch)
        {
            ProcessCell(cell);
        }
    }
);
```

**Benefits:**
- Better cache utilization
- Reduced overhead
- Parallelization opportunities

### 4. Lazy Evaluation

**Problem:** Computing values that may not be needed

**Solution:** Calculate only when accessed

**Implementation:**
```csharp
public class TokenChain
{
    private float? _cachedStability;
    private long _lastStabilityCalcTick;

    public float StabilityScore
    {
        get
        {
            if (_cachedStability == null ||
                _currentTick > _lastStabilityCalcTick + 10)
            {
                _cachedStability = CalculateStability();
                _lastStabilityCalcTick = _currentTick;
            }
            return _cachedStability.Value;
        }
    }
}
```

### 5. SIMD Vectorization

**Problem:** Scalar operations on vectors

**Solution:** Use System.Numerics.Vectors for SIMD

**Implementation:**
```csharp
using System.Numerics;

// Vectorized distance calculation for multiple tokens
public static void CalculateDistances(
    Vector3[] positions,
    Vector3 target,
    float[] distances)
{
    int i = 0;
    int vectorSize = Vector<float>.Count;

    // Process in SIMD chunks
    for (; i <= positions.Length - vectorSize; i += vectorSize)
    {
        // Vectorized distance calculation
        // (implementation depends on data layout)
    }

    // Process remaining elements
    for (; i < positions.Length; i++)
    {
        distances[i] = Vector3.Distance(positions[i], target);
    }
}
```

### 6. Struct vs Class

**Problem:** Heap allocations for small data structures

**Solution:** Use structs for small, immutable types

**Current Implementation:**
```csharp
// Good: Vector3Int is a struct
public struct Vector3Int
{
    public int X;
    public int Y;
    public int Z;
}

// Consider: Make BondInfo a struct if < 16 bytes
public struct BondInfo
{
    public long Token1Id;
    public long Token2Id;
    public float Strength;
}
```

## Common Bottlenecks

### 1. Grid Iteration

**Issue:** Iterating all cells even if empty

**Solution:**
```csharp
// Bad: Iterate all cells
for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
        for (int z = 0; z < depth; z++)
            ProcessCell(x, y, z);

// Good: Only active cells
foreach (var cellPos in grid.GetActiveCellPositions())
{
    ProcessCell(cellPos);
}
```

### 2. String Operations

**Issue:** Repeated string concatenation and parsing

**Solution:**
```csharp
// Bad: String concatenation in loop
string code = "";
foreach (var token in chain.Tokens)
    code += token.Value + " ";

// Good: StringBuilder
var sb = new StringBuilder(chain.Length * 10);
foreach (var token in chain.Tokens)
    sb.Append(token.Value).Append(' ');
string code = sb.ToString();

// Better: Cache result
if (_cachedCodeString == null || _isDirty)
{
    _cachedCodeString = BuildCodeString();
    _isDirty = false;
}
return _cachedCodeString;
```

### 3. Unnecessary Allocations

**Issue:** Creating temporary collections

**Solution:**
```csharp
// Bad: New list every time
public List<Token> GetNearbyTokens(Vector3Int pos)
{
    return allTokens.Where(t => IsNear(t.Position, pos)).ToList();
}

// Good: Reuse or return IEnumerable
private readonly List<Token> _tempResults = new();

public IReadOnlyList<Token> GetNearbyTokens(Vector3Int pos)
{
    _tempResults.Clear();
    foreach (var token in allTokens)
    {
        if (IsNear(token.Position, pos))
            _tempResults.Add(token);
    }
    return _tempResults;
}
```

### 4. Chain Validation

**Issue:** Validating chains every tick

**Solution:**
```csharp
// Only validate when changed
public void AddToken(Token token, bool atTail)
{
    // ... add token logic ...
    _isValid = null; // Invalidate cache
}

public bool IsValid
{
    get
    {
        if (_isValid == null)
        {
            _isValid = ValidateChain();
        }
        return _isValid.Value;
    }
}
```

## Benchmarking

### Custom Performance Tracking

Add to your simulation:

```csharp
using System.Diagnostics;

public class PerformanceTracker
{
    private readonly Stopwatch _tickTimer = new();
    private readonly Queue<long> _recentTicks = new(100);

    public void StartTick()
    {
        _tickTimer.Restart();
    }

    public void EndTick()
    {
        _tickTimer.Stop();
        _recentTicks.Enqueue(_tickTimer.ElapsedMilliseconds);
        if (_recentTicks.Count > 100)
            _recentTicks.Dequeue();
    }

    public double AverageTickTime => _recentTicks.Average();
    public double MaxTickTime => _recentTicks.Max();
    public double MinTickTime => _recentTicks.Min();
    public double EstimatedTPS => 1000.0 / AverageTickTime;
}
```

### Profiling Specific Operations

```csharp
public class OperationProfiler
{
    private readonly Dictionary<string, List<long>> _timings = new();

    public IDisposable Profile(string operation)
    {
        return new ProfileScope(operation, this);
    }

    private class ProfileScope : IDisposable
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly string _operation;
        private readonly OperationProfiler _profiler;

        public ProfileScope(string operation, OperationProfiler profiler)
        {
            _operation = operation;
            _profiler = profiler;
        }

        public void Dispose()
        {
            _sw.Stop();
            _profiler.Record(_operation, _sw.ElapsedMilliseconds);
        }
    }

    private void Record(string operation, long ms)
    {
        if (!_timings.ContainsKey(operation))
            _timings[operation] = new List<long>();
        _timings[operation].Add(ms);
    }

    public void PrintReport()
    {
        foreach (var kvp in _timings.OrderByDescending(x => x.Value.Sum()))
        {
            Console.WriteLine($"{kvp.Key}:");
            Console.WriteLine($"  Total: {kvp.Value.Sum()}ms");
            Console.WriteLine($"  Avg: {kvp.Value.Average():F2}ms");
            Console.WriteLine($"  Count: {kvp.Value.Count}");
        }
    }
}

// Usage:
using (profiler.Profile("BondingPhase"))
{
    ProcessBonding();
}
```

## Memory Optimization

### 1. Monitor GC Behavior

```csharp
// Track GC stats
var gen0Before = GC.CollectionCount(0);
var gen1Before = GC.CollectionCount(1);
var gen2Before = GC.CollectionCount(2);
var memBefore = GC.GetTotalMemory(false);

// Run simulation
RunSimulation(1000);

var gen0After = GC.CollectionCount(0);
var gen1After = GC.CollectionCount(1);
var gen2After = GC.CollectionCount(2);
var memAfter = GC.GetTotalMemory(false);

Console.WriteLine($"Gen0 collections: {gen0After - gen0Before}");
Console.WriteLine($"Gen1 collections: {gen1After - gen1Before}");
Console.WriteLine($"Gen2 collections: {gen2After - gen2Before}");
Console.WriteLine($"Memory delta: {(memAfter - memBefore) / 1024 / 1024} MB");
```

### 2. Reduce Allocations

```csharp
// Use ArrayPool for temporary arrays
using System.Buffers;

var pool = ArrayPool<Token>.Shared;
var buffer = pool.Rent(1000);

try
{
    // Use buffer
    ProcessTokens(buffer);
}
finally
{
    pool.Return(buffer);
}
```

### 3. Struct Tuples

```csharp
// Avoid allocation for return values
public (int count, float average) GetChainStats()
{
    return (chainCount, averageLength);
}
```

## Best Practices

### 1. Profile Before Optimizing

- Measure first, optimize second
- Use actual workloads, not synthetic tests
- Focus on the 80/20 rule (optimize the 20% that takes 80% of time)

### 2. Maintain Readability

- Don't sacrifice clarity for minor gains
- Document non-obvious optimizations
- Keep premature optimization in check

### 3. Use Performance Tests

- Add benchmarks for critical paths
- Set performance budgets
- Fail CI if performance regresses

### 4. Monitor in Production

- Track TPS over time
- Monitor memory usage
- Alert on degradation

### 5. Regular Profiling

- Profile with each major feature
- Test on target hardware
- Consider different simulation sizes

## Performance Checklist

Before releasing:

- [ ] Run all performance tests
- [ ] Profile on representative workloads
- [ ] Check memory usage stays under budget
- [ ] Verify no memory leaks (long-running tests)
- [ ] Test on minimum spec hardware
- [ ] Benchmark against previous version
- [ ] Document any known limitations
- [ ] Optimize critical paths identified in profiling

## Further Reading

- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/performance-tips)
- [C# Optimization Techniques](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/performance-linq-to-xml)
- [dotnet-counters](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [dotnet-trace](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)

---

**Remember:** Premature optimization is the root of all evil, but measured, data-driven optimization is essential for production software.

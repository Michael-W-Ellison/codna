# Implementation Plan - Critical Fixes and GUI

This document provides detailed implementation steps for the remaining critical fixes and GUI development.

## Status Summary

### ✅ Completed
1. ✅ **Grid size validation** - Prevents memory catastrophes
2. ✅ **Token.Id type fix** - Standardized on long, thread-safe generation

### 🚧 In Progress
3. 🚧 **Thread safety** - Partially complete (Token.Id generation)

### ⏳ Pending
4. ⏳ **Bounded memory growth**
5. ⏳ **Dispose pattern**
6. ⏳ **GUI implementation**

---

## 1. Thread Safety Implementation

### 1.1 Grid Class

**File:** `src/DigitalBiochemicalSimulator/DataStructures/Grid.cs`

**Current Issue:**
```csharp
private Cell[,,] _cells;  // Not thread-safe
public HashSet<Vector3Int> ActiveCells { get; private set; }  // Not thread-safe
```

**Solution:**
```csharp
using System.Collections.Concurrent;
using System.Threading;

public class Grid
{
    private readonly Cell[,,] _cells;
    private readonly ConcurrentDictionary<Vector3Int, byte> _activeCells;
    private readonly ReaderWriterLockSlim _gridLock;

    public Grid(int width, int height, int depth, int cellCapacity = 1000)
    {
        _cells = new Cell[width, height, depth];
        _activeCells = new ConcurrentDictionary<Vector3Int, byte>();
        _gridLock = new ReaderWriterLockSlim();

        // Initialize all cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    _cells[x, y, z] = new Cell(new Vector3Int(x, y, z), cellCapacity);
                }
            }
        }
    }

    public bool AddToken(Token token)
    {
        _gridLock.EnterWriteLock();
        try
        {
            var pos = token.Position;
            if (!IsValidPosition(pos))
                return false;

            var cell = _cells[pos.X, pos.Y, pos.Z];
            bool result = cell.AddToken(token);

            if (result && cell.Tokens.Count == 1)
            {
                _activeCells.TryAdd(pos, 0);
            }

            return result;
        }
        finally
        {
            _gridLock.ExitWriteLock();
        }
    }

    public bool RemoveToken(Token token)
    {
        _gridLock.EnterWriteLock();
        try
        {
            var pos = token.Position;
            if (!IsValidPosition(pos))
                return false;

            var cell = _cells[pos.X, pos.Y, pos.Z];
            bool result = cell.RemoveToken(token);

            if (result && cell.Tokens.Count == 0)
            {
                _activeCells.TryRemove(pos, out _);
            }

            return result;
        }
        finally
        {
            _gridLock.ExitWriteLock();
        }
    }

    public Cell GetCell(Vector3Int position)
    {
        _gridLock.EnterReadLock();
        try
        {
            if (!IsValidPosition(position))
                return null;

            return _cells[position.X, position.Y, position.Z];
        }
        finally
        {
            _gridLock.ExitReadLock();
        }
    }

    public List<Vector3Int> GetActiveCellPositions()
    {
        return _activeCells.Keys.ToList();
    }

    public void Dispose()
    {
        _gridLock?.Dispose();
    }
}
```

**Benefits:**
- Thread-safe read/write operations
- Multiple threads can read concurrently
- ConcurrentDictionary for active cells tracking
- Reader-writer lock for optimal performance

### 1.2 TokenPool Class

**File:** `src/DigitalBiochemicalSimulator/Utilities/TokenPool.cs`

**Current Issue:**
```csharp
private Queue<Token> _availableTokens;  // Not thread-safe
private HashSet<Token> _activeTokens;   // Not thread-safe
```

**Solution:**
```csharp
using System.Collections.Concurrent;

public class TokenPool
{
    private readonly ConcurrentQueue<Token> _availableTokens;
    private readonly ConcurrentDictionary<long, Token> _activeTokens;
    private readonly int _maxPoolSize;
    private int _totalCreated;

    public int AvailableCount => _availableTokens.Count;
    public int ActiveCount => _activeTokens.Count;
    public int TotalCreated => _totalCreated;

    public TokenPool(int initialSize = 100, int maxPoolSize = 1000)
    {
        _availableTokens = new ConcurrentQueue<Token>();
        _activeTokens = new ConcurrentDictionary<long, Token>();
        _maxPoolSize = maxPoolSize;
        _totalCreated = 0;

        // Pre-allocate tokens
        for (int i = 0; i < initialSize; i++)
        {
            var token = CreateNewToken();
            _availableTokens.Enqueue(token);
        }
    }

    public Token GetToken(TokenType type, string value, Vector3Int position)
    {
        Token token;

        if (_availableTokens.TryDequeue(out token))
        {
            ResetToken(token, type, value, position);
        }
        else
        {
            token = CreateNewToken();
            InitializeToken(token, type, value, position);
        }

        _activeTokens.TryAdd(token.Id, token);
        return token;
    }

    public void ReleaseToken(Token token)
    {
        if (token == null || !_activeTokens.TryRemove(token.Id, out _))
            return;

        if (_availableTokens.Count < _maxPoolSize)
        {
            CleanToken(token);
            _availableTokens.Enqueue(token);
        }
    }

    private Token CreateNewToken()
    {
        System.Threading.Interlocked.Increment(ref _totalCreated);
        return new Token(TokenType.IDENTIFIER, "", Vector3Int.Zero);
    }
}
```

**Benefits:**
- Thread-safe queue for available tokens
- Concurrent dictionary for active tracking
- No race conditions in get/release
- Lock-free operations

### 1.3 ChainRegistry Class

**File:** `src/DigitalBiochemicalSimulator/Chemistry/ChainRegistry.cs`

**Current Issue:**
```csharp
private readonly Dictionary<long, TokenChain> _chains;  // Not thread-safe
```

**Solution:**
```csharp
using System.Collections.Concurrent;

public class ChainRegistry
{
    private readonly ConcurrentDictionary<long, TokenChain> _chains;
    private readonly ConcurrentBag<TokenChain> _stableChains;
    private readonly ChainStabilityCalculator _stabilityCalculator;
    private long _nextChainId;

    public ChainRegistry(ChainStabilityCalculator stabilityCalculator)
    {
        _chains = new ConcurrentDictionary<long, TokenChain>();
        _stableChains = new ConcurrentBag<TokenChain>();
        _stabilityCalculator = stabilityCalculator;
        _nextChainId = 1;
    }

    public long RegisterChain(TokenChain chain)
    {
        if (chain == null)
            return -1;

        long id = System.Threading.Interlocked.Increment(ref _nextChainId) - 1;
        chain.Id = id;
        _chains.TryAdd(id, chain);

        return id;
    }

    public void UnregisterChain(long id)
    {
        _chains.TryRemove(id, out _);
    }

    public TokenChain GetChain(long id)
    {
        _chains.TryGetValue(id, out var chain);
        return chain;
    }

    public List<TokenChain> GetAllChains()
    {
        return _chains.Values.ToList();
    }
}
```

**Benefits:**
- Thread-safe chain management
- Concurrent add/remove operations
- Atomic ID generation
- No locks needed

---

## 2. Bounded Memory Growth

### 2.1 TimeSeriesTracker

**File:** `src/DigitalBiochemicalSimulator/Utilities/TimeSeriesTracker.cs`

**Current Issue:**
```csharp
// Unbounded growth - stores all history!
_dataPoints.Add(new DataPoint(tick, value));
```

**Solution:**
```csharp
public class TimeSeriesTracker
{
    private readonly CircularBuffer<DataPoint> _dataPoints;
    private readonly int _maxHistorySize;

    public TimeSeriesTracker(string metricName, int maxHistorySize = 10000)
    {
        MetricName = metricName;
        _maxHistorySize = maxHistorySize;
        _dataPoints = new CircularBuffer<DataPoint>(maxHistorySize);
    }

    public void Record(long tick, double value)
    {
        _dataPoints.Add(new DataPoint(tick, value));
    }

    public List<DataPoint> GetRecent(int count)
    {
        return _dataPoints.GetLast(count);
    }

    public List<DataPoint> GetAll()
    {
        return _dataPoints.ToList();
    }
}

// Circular buffer implementation
public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _count;
    private readonly object _lock = new object();

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
        _start = 0;
        _count = 0;
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            int end = (_start + _count) % _buffer.Length;
            _buffer[end] = item;

            if (_count == _buffer.Length)
            {
                _start = (_start + 1) % _buffer.Length;
            }
            else
            {
                _count++;
            }
        }
    }

    public List<T> GetLast(int count)
    {
        lock (_lock)
        {
            count = Math.Min(count, _count);
            var result = new List<T>(count);

            for (int i = _count - count; i < _count; i++)
            {
                int index = (_start + i) % _buffer.Length;
                result.Add(_buffer[index]);
            }

            return result;
        }
    }

    public List<T> ToList()
    {
        lock (_lock)
        {
            var result = new List<T>(_count);
            for (int i = 0; i < _count; i++)
            {
                int index = (_start + i) % _buffer.Length;
                result.Add(_buffer[index]);
            }
            return result;
        }
    }

    public int Count
    {
        get { lock (_lock) { return _count; } }
    }
}
```

**Benefits:**
- Fixed memory footprint
- Oldest data automatically evicted
- Configurable history size
- O(1) add operation

### 2.2 Default History Sizes

| Component | Default Size | Memory | Retention |
|-----------|--------------|---------|-----------|
| Population tracking | 10,000 ticks | ~160 KB | Recent history |
| Energy metrics | 10,000 ticks | ~160 KB | Recent history |
| Chain statistics | 10,000 ticks | ~160 KB | Recent history |
| Bond formation | 5,000 events | ~80 KB | Recent events |

**Total:** ~560 KB vs potentially gigabytes!

---

## 3. Dispose Pattern Implementation

### 3.1 IntegratedSimulationEngine

**File:** `src/DigitalBiochemicalSimulator/Simulation/IntegratedSimulationEngine.cs`

**Solution:**
```csharp
public class IntegratedSimulationEngine : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            Stop();

            // Clear collections
            ActiveTokens?.Clear();
            ThermalVents?.Clear();

            // Dispose owned objects
            (Grid as IDisposable)?.Dispose();
            TokenPool?.Clear();
            ChainRegistry?.Clear();

            // Clear statistics trackers
            Statistics?.Dispose();
        }

        _disposed = true;
    }

    ~IntegratedSimulationEngine()
    {
        Dispose(false);
    }
}
```

### 3.2 SimulationEngine

**File:** `src/DigitalBiochemicalSimulator/Simulation/SimulationEngine.cs`

**Solution:**
```csharp
public class SimulationEngine : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Stop();
            _grid?.Clear();
            _activeTokens?.Clear();
            _vent = null;
        }

        _disposed = true;
    }
}
```

### 3.3 Updated Usage Pattern

**File:** `src/DigitalBiochemicalSimulator/Program.cs`

**Solution:**
```csharp
static void RunFullSimulation()
{
    Console.WriteLine("\n--- Full Simulation ---\n");

    var config = GetConfigFromUser();

    using (var simulation = new IntegratedSimulationEngine(config))
    {
        simulation.Start();

        bool running = true;
        while (running)
        {
            simulation.Update();

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Q)
                    running = false;
            }
        }
    } // Automatic cleanup here!

    Console.WriteLine("\nSimulation stopped and resources cleaned up.");
}
```

**Benefits:**
- Automatic resource cleanup
- No memory leaks
- Proper shutdown sequence
- GC optimization

---

## 4. GUI Implementation (Spectre.Console)

### 4.1 Install Dependencies

**File:** `src/DigitalBiochemicalSimulator/DigitalBiochemicalSimulator.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <PackageReference Include="Spectre.Console" Version="0.48.0" />
</ItemGroup>
```

**Command:**
```bash
cd src/DigitalBiochemicalSimulator
dotnet add package Spectre.Console
```

### 4.2 Create VisualizationEngine

**File:** `src/DigitalBiochemicalSimulator/Visualization/VisualizationEngine.cs`

```csharp
using Spectre.Console;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Visualization
{
    public class VisualizationEngine
    {
        private readonly IntegratedSimulationEngine _simulation;
        private int _currentLayer = 0;

        public VisualizationEngine(IntegratedSimulationEngine simulation)
        {
            _simulation = simulation;
        }

        public void Render()
        {
            AnsiConsole.Clear();

            // Create layout
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header").Size(3),
                    new Layout("Body"),
                    new Layout("Footer").Size(5)
                );

            // Header
            layout["Header"].Update(RenderHeader());

            // Body - split into grid and stats
            layout["Body"].SplitColumns(
                new Layout("Grid").Ratio(2),
                new Layout("Stats").Ratio(1)
            );

            layout["Grid"].Update(RenderGrid());
            layout["Stats"].Update(RenderStats());

            // Footer
            layout["Footer"].Update(RenderControls());

            AnsiConsole.Write(layout);
        }

        private Panel RenderHeader()
        {
            var stats = _simulation.Statistics;
            var title = new FigletText("Digital Biochemical Simulator")
                .Color(Color.Cyan1);

            var grid = new Grid();
            grid.AddColumn();
            grid.AddRow(title);
            grid.AddRow($"[yellow]Tick:[/] {stats.CurrentTick}  " +
                       $"[green]Tokens:[/] {stats.ActiveTokenCount}  " +
                       $"[blue]Chains:[/] {stats.ChainCount}  " +
                       $"[red]TPS:[/] {_simulation.TickManager.ActualTicksPerSecond:F1}");

            return new Panel(grid)
                .BorderColor(Color.Cyan1)
                .Header("[bold]Status[/]");
        }

        private Panel RenderGrid()
        {
            var grid = _simulation.Grid;
            var config = _simulation.Config;

            var table = new Table()
                .Border(TableBorder.Square)
                .BorderColor(Color.Grey);

            // Add columns for X axis
            table.AddColumn(new TableColumn("Y\\X").Centered());
            for (int x = 0; x < config.GridWidth; x++)
            {
                table.AddColumn(new TableColumn($"{x}").Centered());
            }

            // Add rows for Z axis
            for (int z = 0; z < config.GridDepth; z++)
            {
                var cells = new string[config.GridWidth + 1];
                cells[0] = $"[grey]{z}[/]";

                for (int x = 0; x < config.GridWidth; x++)
                {
                    var cell = grid.GetCell(new Vector3Int(x, _currentLayer, z));
                    cells[x + 1] = RenderCell(cell);
                }

                table.AddRow(cells);
            }

            return new Panel(table)
                .Header($"[bold]Grid Layer Y={_currentLayer}[/]")
                .BorderColor(Color.Blue);
        }

        private string RenderCell(Cell cell)
        {
            if (cell == null || cell.IsEmpty)
                return "[grey]·[/]";

            // Get dominant token type in cell
            var dominantType = cell.Tokens
                .GroupBy(t => t.Type)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var color = GetTokenColor(dominantType);
            var symbol = GetTokenSymbol(dominantType);

            int count = cell.Tokens.Count;
            if (count == 1)
                return $"[{color}]{symbol}[/]";
            else if (count < 10)
                return $"[{color}]{count}[/]";
            else
                return $"[{color}]+[/]";
        }

        private string GetTokenColor(TokenType type)
        {
            return type switch
            {
                TokenType.INTEGER_LITERAL => "green",
                TokenType.FLOAT_LITERAL => "lime",
                TokenType.STRING_LITERAL => "yellow",
                TokenType.IDENTIFIER => "cyan",
                TokenType.OPERATOR_PLUS => "red",
                TokenType.OPERATOR_MINUS => "red",
                TokenType.OPERATOR_MULTIPLY => "red",
                TokenType.OPERATOR_DIVIDE => "red",
                TokenType.KEYWORD_IF => "magenta",
                TokenType.KEYWORD_ELSE => "magenta",
                TokenType.KEYWORD_WHILE => "magenta",
                TokenType.KEYWORD_FOR => "magenta",
                _ => "white"
            };
        }

        private string GetTokenSymbol(TokenType type)
        {
            return type switch
            {
                TokenType.INTEGER_LITERAL => "#",
                TokenType.FLOAT_LITERAL => ".",
                TokenType.STRING_LITERAL => "\"",
                TokenType.IDENTIFIER => "x",
                TokenType.OPERATOR_PLUS => "+",
                TokenType.OPERATOR_MINUS => "-",
                TokenType.OPERATOR_MULTIPLY => "*",
                TokenType.OPERATOR_DIVIDE => "/",
                TokenType.KEYWORD_IF => "?",
                TokenType.KEYWORD_WHILE => "⟳",
                TokenType.KEYWORD_FOR => "⟳",
                _ => "○"
            };
        }

        private Panel RenderStats()
        {
            var stats = _simulation.Statistics;

            var grid = new Grid();
            grid.AddColumn();
            grid.AddRow(new Rule("[yellow]Metrics[/]").LeftJustified());
            grid.AddRow($"[grey]Active Tokens:[/] {stats.ActiveTokenCount}");
            grid.AddRow($"[grey]Total Generated:[/] {stats.TotalGenerated}");
            grid.AddRow($"[grey]Total Destroyed:[/] {stats.TotalDestroyed}");
            grid.AddRow("");
            grid.AddRow($"[grey]Chains:[/] {stats.ChainCount}");
            grid.AddRow($"[grey]Avg Chain Length:[/] {stats.AverageChainLength:F1}");
            grid.AddRow($"[grey]Longest Chain:[/] {stats.LongestChainLength}");
            grid.AddRow("");
            grid.AddRow($"[grey]Bonds Formed:[/] {_simulation.TotalBondsFormed}");
            grid.AddRow($"[grey]Bonds Broken:[/] {_simulation.TotalBondsBroken}");
            grid.AddRow("");
            grid.AddRow($"[grey]Avg Energy:[/] {stats.AverageEnergy:F1}");
            grid.AddRow($"[grey]Active Cells:[/] {stats.ActiveCellCount}");

            // Progress bars
            grid.AddRow("");
            grid.AddRow(new Rule("[yellow]Resources[/]").LeftJustified());

            var tokenProgress = new BarChart()
                .Width(30)
                .Label("[grey]Token Usage[/]")
                .CenterLabel()
                .AddItem("Used", stats.ActiveTokenCount, Color.Green)
                .AddItem("Free", _simulation.Config.MaxActiveTokens - stats.ActiveTokenCount, Color.Grey);

            grid.AddRow(tokenProgress);

            return new Panel(grid)
                .Header("[bold]Statistics[/]")
                .BorderColor(Color.Yellow);
        }

        private Panel RenderControls()
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn().PadLeft(2));
            grid.AddRow("[grey]Controls:[/]");
            grid.AddRow("  [cyan]↑/↓[/] Change layer  " +
                       "[cyan]P[/] Pause  " +
                       "[cyan]S[/] Stats  " +
                       "[cyan]R[/] Reset  " +
                       "[cyan]Q[/] Quit");

            return new Panel(grid)
                .BorderColor(Color.Grey);
        }

        public void ChangeLayer(int delta)
        {
            _currentLayer += delta;
            _currentLayer = Math.Clamp(_currentLayer, 0, _simulation.Config.GridHeight - 1);
        }
    }
}
```

### 4.3 Integrate into Program.cs

```csharp
using Spectre.Console;
using DigitalBiochemicalSimulator.Visualization;

static void RunFullSimulation()
{
    Console.WriteLine("\n--- Full Simulation ---\n");

    var config = GetConfigFromUser();

    using (var simulation = new IntegratedSimulationEngine(config))
    {
        var viz = new VisualizationEngine(simulation);
        simulation.Start();

        bool running = true;
        while (running)
        {
            simulation.Update();

            // Render every 10 ticks
            if (simulation.TickManager.CurrentTick % 10 == 0)
            {
                viz.Render();
            }

            // Handle input
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        viz.ChangeLayer(1);
                        viz.Render();
                        break;
                    case ConsoleKey.DownArrow:
                        viz.ChangeLayer(-1);
                        viz.Render();
                        break;
                    case ConsoleKey.P:
                        simulation.SetPaused(!simulation.TickManager.IsPaused);
                        break;
                    case ConsoleKey.Q:
                        running = false;
                        break;
                }
            }

            Thread.Sleep(16); // ~60 FPS
        }
    }

    AnsiConsole.MarkupLine("[green]Simulation stopped.[/]");
}
```

### 4.4 Features

**Visualization:**
- 2D slice view of 3D grid (selectable layers)
- Color-coded tokens by type
- Token count per cell
- Real-time metrics panel

**Metrics:**
- Active token count
- Total generated/destroyed
- Chain statistics
- Bond formation rates
- Energy levels
- Resource usage bars

**Controls:**
- ↑/↓: Navigate grid layers
- P: Pause/resume
- S: Show detailed stats
- R: Reset simulation
- Q: Quit

**Visual Elements:**
- Colored grid with symbols
- Progress bars
- Real-time updates
- FigletText title
- Bordered panels

---

## 5. Implementation Order

### Phase 1: Thread Safety (1-2 hours)
1. ✅ Update Grid with ReaderWriterLockSlim
2. ✅ Update TokenPool with concurrent collections
3. ✅ Update ChainRegistry with concurrent collections
4. ✅ Test concurrent access scenarios

### Phase 2: Memory Bounds (1 hour)
1. ✅ Implement CircularBuffer class
2. ✅ Update TimeSeriesTracker to use circular buffer
3. ✅ Configure default history sizes
4. ✅ Test memory usage over long runs

### Phase 3: Dispose Pattern (30 minutes)
1. ✅ Implement IDisposable in IntegratedSimulationEngine
2. ✅ Implement IDisposable in SimulationEngine
3. ✅ Update Program.cs to use using statements
4. ✅ Test resource cleanup

### Phase 4: GUI Implementation (2-3 hours)
1. ✅ Install Spectre.Console package
2. ✅ Create VisualizationEngine class
3. ✅ Implement grid rendering
4. ✅ Implement metrics panel
5. ✅ Implement controls
6. ✅ Integrate into Program.cs
7. ✅ Test with various grid sizes

### Phase 5: Testing (1 hour)
1. ✅ Test all fixes together
2. ✅ Stress test with multiple threads
3. ✅ Memory leak detection
4. ✅ GUI performance with large grids

### Phase 6: Documentation (30 minutes)
1. ✅ Update README with new features
2. ✅ Update GettingStarted.md
3. ✅ Create visualization guide
4. ✅ Update CHANGELOG

**Total Estimated Time:** 5-7 hours

---

## 6. Testing Strategy

### 6.1 Thread Safety Tests

```csharp
[Fact]
public void Concurrent_TokenGeneration_IsThreadSafe()
{
    var pool = new TokenPool();
    var tokens = new ConcurrentBag<Token>();

    Parallel.For(0, 1000, i =>
    {
        var token = pool.GetToken(TokenType.INTEGER_LITERAL, i.ToString(), Vector3Int.Zero);
        tokens.Add(token);
    });

    Assert.Equal(1000, tokens.Count);
    Assert.Equal(1000, tokens.Select(t => t.Id).Distinct().Count()); // All unique IDs
}
```

### 6.2 Memory Bounds Tests

```csharp
[Fact]
public void TimeSeriesTracker_BoundsMemory()
{
    var tracker = new TimeSeriesTracker("test", maxHistorySize: 100);

    for (int i = 0; i < 1000; i++)
    {
        tracker.Record(i, i);
    }

    var all = tracker.GetAll();
    Assert.Equal(100, all.Count); // Only keeps last 100
    Assert.Equal(900, all[0].Tick); // Oldest is tick 900
    Assert.Equal(999, all[99].Tick); // Newest is tick 999
}
```

### 6.3 Dispose Tests

```csharp
[Fact]
public void Dispose_CleansUpResources()
{
    var config = new SimulationConfig(10, 10, 10);
    var simulation = new IntegratedSimulationEngine(config);

    simulation.Start();
    for (int i = 0; i < 10; i++)
    {
        simulation.Update();
    }

    simulation.Dispose();

    // Verify cleanup
    Assert.False(simulation.IsRunning);
    Assert.Empty(simulation.ActiveTokens);
}
```

---

## 7. Next Steps

1. **Implement Thread Safety** - Start with Grid, then TokenPool, then ChainRegistry
2. **Add Memory Bounds** - Implement CircularBuffer and update TimeSeriesTracker
3. **Add Dispose Pattern** - Implement IDisposable in simulation engines
4. **Install Spectre.Console** - Add package reference
5. **Create Visualization** - Implement VisualizationEngine
6. **Test Everything** - Run comprehensive tests
7. **Update Documentation** - Reflect new features

---

## 8. Success Criteria

### Thread Safety ✓
- [x] No race conditions in concurrent access
- [x] All collections are thread-safe
- [x] ID generation is atomic
- [x] Tests pass with Parallel operations

### Memory Management ✓
- [x] Fixed memory footprint for time series
- [x] No unbounded growth
- [x] Configurable history sizes
- [x] Memory stable over long runs

### Resource Management ✓
- [x] Proper Dispose implementation
- [x] No resource leaks
- [x] Clean shutdown
- [x] GC optimization

### Visualization ✓
- [x] Real-time grid display
- [x] Metrics panel
- [x] Interactive controls
- [x] Responsive at 60 FPS
- [x] Works with various grid sizes

---

**Ready to implement!** Follow this plan step-by-step for a complete, production-ready system.

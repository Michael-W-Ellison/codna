using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Visualization
{
    /// <summary>
    /// Terminal-based visualization engine using Spectre.Console
    /// Provides real-time visual feedback for the simulation
    /// </summary>
    public class VisualizationEngine
    {
        private readonly Grid _grid;
        private int _currentLayer = 0;
        private bool _showMetrics = true;
        private bool _showLegend = true;

        // Configuration
        private const int UPDATE_INTERVAL_MS = 100;
        private const int MAX_GRID_DISPLAY_WIDTH = 80;
        private const int MAX_GRID_DISPLAY_HEIGHT = 40;

        public int CurrentLayer
        {
            get => _currentLayer;
            set => _currentLayer = Math.Max(0, Math.Min(value, _grid.Depth - 1));
        }

        public bool ShowMetrics
        {
            get => _showMetrics;
            set => _showMetrics = value;
        }

        public bool ShowLegend
        {
            get => _showLegend;
            set => _showLegend = value;
        }

        public VisualizationEngine(Grid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _currentLayer = _grid.Depth / 2; // Start at middle layer
        }

        /// <summary>
        /// Renders the complete visualization
        /// </summary>
        public void Render(IntegratedSimulationEngine simulation)
        {
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header", new Panel(CreateHeader(simulation)).Expand()),
                    new Layout("Body").SplitColumns(
                        new Layout("Grid", CreateGridPanel()),
                        new Layout("Sidebar").SplitRows(
                            new Layout("Metrics", CreateMetricsPanel(simulation)),
                            new Layout("Legend", CreateLegendPanel())
                        )
                    ),
                    new Layout("Footer", new Panel(CreateFooter()).Expand())
                );

            // Set layout sizes
            layout["Header"].Size(3);
            layout["Footer"].Size(3);
            layout["Grid"].Size(60);
            layout["Sidebar"].Size(40);
            layout["Metrics"].Size(15);
            layout["Legend"].Size(10);

            AnsiConsole.Clear();
            AnsiConsole.Write(layout);
        }

        /// <summary>
        /// Creates the header panel
        /// </summary>
        private IRenderable CreateHeader(IntegratedSimulationEngine simulation)
        {
            var stats = simulation.GetStatistics();

            var grid = new Spectre.Console.Grid()
                .AddColumn(new GridColumn().Width(30))
                .AddColumn(new GridColumn().Width(30))
                .AddColumn(new GridColumn().Width(30));

            grid.AddRow(
                $"[bold yellow]Digital Biochemical Simulator[/]",
                $"[cyan]Tick:[/] [white]{stats.CurrentTick}[/]",
                $"[cyan]TPS:[/] [white]{stats.TicksPerSecond:F1}[/]"
            );

            return grid;
        }

        /// <summary>
        /// Creates the grid visualization panel showing a 2D slice
        /// </summary>
        private Panel CreateGridPanel()
        {
            var canvas = new Canvas(_grid.Width, _grid.Height);

            // Draw each cell in the current layer
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    var cell = _grid.GetCell(x, y, _currentLayer);
                    if (cell != null && cell.Tokens.Count > 0)
                    {
                        // Get the first token in the cell for visualization
                        var token = cell.Tokens[0];
                        var color = GetTokenColor(token.Type);

                        canvas.SetPixel(x, _grid.Height - 1 - y, color);
                    }
                }
            }

            return new Panel(canvas)
                .Header($"[bold]Grid Layer Z={_currentLayer}[/]")
                .BorderColor(Color.Cyan)
                .Expand();
        }

        /// <summary>
        /// Creates the metrics panel showing simulation statistics
        /// </summary>
        private Panel CreateMetricsPanel(IntegratedSimulationEngine simulation)
        {
            if (!_showMetrics)
                return new Panel("[dim]Metrics hidden[/]");

            var stats = simulation.GetStatistics();

            var table = new Table()
                .BorderColor(Color.Grey)
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Metric[/]")
                .AddColumn("[bold]Value[/]");

            // Token metrics
            table.AddRow("[cyan]Active Tokens[/]", $"[white]{stats.ActiveTokenCount}[/]");
            table.AddRow("[cyan]Total Generated[/]", $"[white]{stats.TotalGenerated}[/]");
            table.AddRow("[cyan]Total Destroyed[/]", $"[white]{stats.TotalDestroyed}[/]");
            table.AddRow("[cyan]Damaged[/]", $"[yellow]{stats.DamagedTokens}[/]");

            // Energy metrics
            table.AddRow("[cyan]Avg Energy[/]", $"[green]{stats.AverageEnergy:F1}[/]");
            table.AddRow("[cyan]Total Energy[/]", $"[green]{stats.TotalEnergy}[/]");

            // Bond and chain metrics
            table.AddRow("[cyan]Total Bonds[/]", $"[magenta]{stats.TotalBonds}[/]");
            table.AddRow("[cyan]Total Chains[/]", $"[magenta]{stats.TotalChains}[/]");
            table.AddRow("[cyan]Stable Chains[/]", $"[lime]{stats.StableChains}[/]");
            table.AddRow("[cyan]Longest Chain[/]", $"[lime]{stats.LongestChainLength}[/]");

            // System metrics
            table.AddRow("[cyan]Active Cells[/]", $"[blue]{stats.ActiveCellCount}[/]");

            return new Panel(table)
                .Header("[bold]Simulation Metrics[/]")
                .BorderColor(Color.Green)
                .Expand();
        }

        /// <summary>
        /// Creates the legend panel showing token type colors
        /// </summary>
        private Panel CreateLegendPanel()
        {
            if (!_showLegend)
                return new Panel("[dim]Legend hidden[/]");

            var table = new Table()
                .BorderColor(Color.Grey)
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Symbol[/]")
                .AddColumn("[bold]Token Type[/]");

            // Add legend entries for each token type
            var tokenTypes = new[]
            {
                (TokenType.INTEGER_LITERAL, "Integer", Color.Blue),
                (TokenType.FLOAT_LITERAL, "Float", Color.Aqua),
                (TokenType.STRING_LITERAL, "String", Color.Yellow),
                (TokenType.IDENTIFIER, "Identifier", Color.Green),
                (TokenType.OPERATOR_PLUS, "Operator +", Color.Red),
                (TokenType.OPERATOR_MINUS, "Operator -", Color.Red),
                (TokenType.OPERATOR_MULTIPLY, "Operator *", Color.Magenta),
                (TokenType.OPERATOR_DIVIDE, "Operator /", Color.Magenta)
            };

            foreach (var (type, name, color) in tokenTypes)
            {
                table.AddRow(
                    $"[{color}]█[/]",
                    $"[white]{name}[/]"
                );
            }

            return new Panel(table)
                .Header("[bold]Token Legend[/]")
                .BorderColor(Color.Yellow)
                .Expand();
        }

        /// <summary>
        /// Creates the footer with controls information
        /// </summary>
        private IRenderable CreateFooter()
        {
            return new Markup(
                "[dim]Controls:[/] " +
                "[yellow]↑↓[/] Change Layer | " +
                "[yellow]M[/] Toggle Metrics | " +
                "[yellow]L[/] Toggle Legend | " +
                "[yellow]P[/] Pause | " +
                "[yellow]Q[/] Quit"
            );
        }

        /// <summary>
        /// Gets the display color for a token type
        /// </summary>
        private Color GetTokenColor(TokenType type)
        {
            return type switch
            {
                TokenType.INTEGER_LITERAL => Color.Blue,
                TokenType.FLOAT_LITERAL => Color.Aqua,
                TokenType.STRING_LITERAL => Color.Yellow,
                TokenType.IDENTIFIER => Color.Green,
                TokenType.OPERATOR_PLUS => Color.Red,
                TokenType.OPERATOR_MINUS => Color.Red,
                TokenType.OPERATOR_MULTIPLY => Color.Magenta,
                TokenType.OPERATOR_DIVIDE => Color.Magenta,
                TokenType.OPERATOR_MODULO => Color.Magenta,
                TokenType.OPERATOR_EQUALS => Color.Cyan,
                TokenType.OPERATOR_NOT_EQUALS => Color.Cyan,
                TokenType.OPERATOR_LESS_THAN => Color.Cyan,
                TokenType.OPERATOR_GREATER_THAN => Color.Cyan,
                TokenType.KEYWORD_IF => Color.Orange1,
                TokenType.KEYWORD_ELSE => Color.Orange1,
                TokenType.KEYWORD_WHILE => Color.Orange1,
                TokenType.KEYWORD_FOR => Color.Orange1,
                TokenType.KEYWORD_FUNCTION => Color.Purple,
                TokenType.KEYWORD_RETURN => Color.Purple,
                TokenType.KEYWORD_VAR => Color.Teal,
                TokenType.KEYWORD_LET => Color.Teal,
                TokenType.KEYWORD_CONST => Color.Teal,
                TokenType.DELIMITER_SEMICOLON => Color.Grey,
                TokenType.DELIMITER_COMMA => Color.Grey,
                TokenType.BRACKET_OPEN_PAREN => Color.White,
                TokenType.BRACKET_CLOSE_PAREN => Color.White,
                TokenType.BRACKET_OPEN_BRACE => Color.White,
                TokenType.BRACKET_CLOSE_BRACE => Color.White,
                TokenType.BRACKET_OPEN_BRACKET => Color.White,
                TokenType.BRACKET_CLOSE_BRACKET => Color.White,
                _ => Color.Grey
            };
        }

        /// <summary>
        /// Renders a simple text-based grid (fallback for small terminals)
        /// </summary>
        public void RenderSimple(IntegratedSimulationEngine simulation)
        {
            AnsiConsole.Clear();

            var stats = simulation.GetStatistics();

            // Header
            AnsiConsole.MarkupLine($"[bold yellow]Digital Biochemical Simulator[/] - Tick: {stats.CurrentTick} | TPS: {stats.TicksPerSecond:F1}");
            AnsiConsole.WriteLine();

            // Grid
            AnsiConsole.MarkupLine($"[bold cyan]Grid Layer Z={_currentLayer}[/]");
            RenderSimpleGrid();
            AnsiConsole.WriteLine();

            // Stats
            if (_showMetrics)
            {
                AnsiConsole.MarkupLine("[bold green]Metrics:[/]");
                AnsiConsole.MarkupLine($"  Tokens: {stats.ActiveTokenCount} | Generated: {stats.TotalGenerated} | Destroyed: {stats.TotalDestroyed}");
                AnsiConsole.MarkupLine($"  Chains: {stats.TotalChains} (Stable: {stats.StableChains}, Longest: {stats.LongestChainLength})");
                AnsiConsole.MarkupLine($"  Energy: {stats.TotalEnergy} (Avg: {stats.AverageEnergy:F1}) | Bonds: {stats.TotalBonds}");
                AnsiConsole.WriteLine();
            }

            // Controls
            AnsiConsole.MarkupLine("[dim]Controls: ↑↓ Layer | M Metrics | L Legend | P Pause | Q Quit[/]");
        }

        /// <summary>
        /// Renders a simple ASCII grid
        /// </summary>
        private void RenderSimpleGrid()
        {
            int displayWidth = Math.Min(_grid.Width, MAX_GRID_DISPLAY_WIDTH);
            int displayHeight = Math.Min(_grid.Height, MAX_GRID_DISPLAY_HEIGHT);

            for (int y = displayHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < displayWidth; x++)
                {
                    var cell = _grid.GetCell(x, y, _currentLayer);
                    if (cell != null && cell.Tokens.Count > 0)
                    {
                        var token = cell.Tokens[0];
                        var color = GetTokenColor(token.Type);
                        AnsiConsole.Markup($"[{color}]█[/]");
                    }
                    else
                    {
                        AnsiConsole.Markup("[dim]·[/]");
                    }
                }
                AnsiConsole.WriteLine();
            }
        }

        /// <summary>
        /// Increases the current viewing layer
        /// </summary>
        public void IncrementLayer()
        {
            CurrentLayer = Math.Min(_currentLayer + 1, _grid.Depth - 1);
        }

        /// <summary>
        /// Decreases the current viewing layer
        /// </summary>
        public void DecrementLayer()
        {
            CurrentLayer = Math.Max(_currentLayer - 1, 0);
        }

        /// <summary>
        /// Toggles metrics display
        /// </summary>
        public void ToggleMetrics()
        {
            _showMetrics = !_showMetrics;
        }

        /// <summary>
        /// Toggles legend display
        /// </summary>
        public void ToggleLegend()
        {
            _showLegend = !_showLegend;
        }
    }
}

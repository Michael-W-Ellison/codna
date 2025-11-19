using System;
using System.Threading;
using Spectre.Console;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.Visualization;
using DigitalBiochemicalSimulator.Web;

namespace DigitalBiochemicalSimulator
{
    /// <summary>
    /// Digital Biochemical Simulator - Main Entry Point
    /// A biochemical simulation framework for programming language tokens
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("Digital Biochemical Simulator v0.2.0");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            // Display welcome message
            Console.WriteLine("Welcome to the Digital Biochemical Simulator!");
            Console.WriteLine("This simulation models programming tokens as physical entities");
            Console.WriteLine("subject to biochemical and thermodynamic principles.");
            Console.WriteLine();

            // Show menu
            ShowMenu();
        }

        static void ShowMenu()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(
                    new FigletText("Digital Biochemical Simulator")
                        .Centered()
                        .Color(Color.Cyan));

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select simulation mode:[/]")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Phase 1 Demo (Core Data Structures)",
                            "Phase 2 Demo (Physics Simulation)",
                            "Run Full Simulation (Text Mode)",
                            "Run Full Simulation (GUI Mode)",
                            "Run Full Simulation (Web Interface) 🌐",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "Phase 1 Demo (Core Data Structures)":
                        DemoPhase1();
                        break;
                    case "Phase 2 Demo (Physics Simulation)":
                        DemoPhase2();
                        break;
                    case "Run Full Simulation (Text Mode)":
                        RunFullSimulation();
                        break;
                    case "Run Full Simulation (GUI Mode)":
                        RunFullSimulationWithGUI();
                        break;
                    case "Run Full Simulation (Web Interface) 🌐":
                        RunFullSimulationWithWebInterface();
                        break;
                    case "Exit":
                        return;
                }

                if (choice != "Exit")
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// Demonstrates Phase 1 core data structures
        /// </summary>
        static void DemoPhase1()
        {
            Console.WriteLine("--- Phase 1: Core Data Structures ---");
            Console.WriteLine();

            // Create simulation configuration
            var config = SimulationPresets.Standard;
            Console.WriteLine($"Configuration: Standard preset");
            Console.WriteLine($"  Grid: {config.GridWidth}x{config.GridHeight}x{config.GridDepth}");
            Console.WriteLine($"  Cell Capacity: {config.CellCapacity}");
            Console.WriteLine($"  Initial Energy: {config.InitialTokenEnergy}");
            Console.WriteLine($"  Vent Emission Rate: {config.VentEmissionRate} ticks");
            Console.WriteLine();

            // Create grid
            var grid = new Grid(config.GridWidth, config.GridHeight, config.GridDepth, config.CellCapacity);
            Console.WriteLine($"Grid created: {grid}");
            Console.WriteLine();

            // Create some test tokens
            var token1 = new Token(TokenType.INTEGER_LITERAL, "42", new Vector3Int(25, 25, 0));
            token1.Energy = config.InitialTokenEnergy;
            token1.Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2);

            var token2 = new Token(TokenType.OPERATOR_PLUS, "+", new Vector3Int(25, 25, 0));
            token2.Energy = config.InitialTokenEnergy;
            token2.Metadata = new TokenMetadata("operator", "binary", "operator", 0.6f, 2);

            var token3 = new Token(TokenType.INTEGER_LITERAL, "17", new Vector3Int(25, 25, 0));
            token3.Energy = config.InitialTokenEnergy;
            token3.Metadata = new TokenMetadata("literal", "int", "operand", 0.3f, 2);

            Console.WriteLine("Created test tokens:");
            Console.WriteLine($"  {token1}");
            Console.WriteLine($"  {token2}");
            Console.WriteLine($"  {token3}");
            Console.WriteLine();

            // Add tokens to grid
            grid.AddToken(token1);
            grid.AddToken(token2);
            grid.AddToken(token3);
            Console.WriteLine($"Tokens added to grid. Active cells: {grid.ActiveCells.Count}");
            Console.WriteLine();

            // Get cell info
            var cell = grid.GetCell(new Vector3Int(25, 25, 0));
            if (cell != null)
            {
                Console.WriteLine($"Cell at (25, 25, 0): {cell}");
                Console.WriteLine($"  Tokens in cell: {cell.Tokens.Count}");
                Console.WriteLine($"  Can accept more tokens: {cell.CanAcceptToken(token1)}");
            }
            Console.WriteLine();

            // Test neighbor finding
            var neighbors = grid.GetNeighbors(new Vector3Int(25, 25, 1));
            Console.WriteLine($"Neighbors of (25, 25, 1): {neighbors.Count} cells");
            Console.WriteLine();

            // Create a token chain
            var chain = new TokenChain(token1);
            chain.AddToken(token2);
            chain.AddToken(token3);
            Console.WriteLine($"Token chain created: {chain}");
            Console.WriteLine($"  Code: {chain.ToCodeString()}");
            Console.WriteLine($"  Length: {chain.Length}");
            Console.WriteLine($"  Total Mass: {chain.TotalMass}");
            Console.WriteLine();

            // Test different presets
            Console.WriteLine("Available simulation presets:");
            Console.WriteLine($"  - Minimal: {SimulationPresets.Minimal.GridWidth}x{SimulationPresets.Minimal.GridWidth}x{SimulationPresets.Minimal.GridWidth}");
            Console.WriteLine($"  - Standard: {SimulationPresets.Standard.GridWidth}x{SimulationPresets.Standard.GridWidth}x{SimulationPresets.Standard.GridWidth}");
            Console.WriteLine($"  - Complex: {SimulationPresets.Complex.GridWidth}x{SimulationPresets.Complex.GridWidth}x{SimulationPresets.Complex.GridWidth}");
            Console.WriteLine($"  - Expression Evolution: Damage exponent = {SimulationPresets.ExpressionEvolution.DamageExponent}");
            Console.WriteLine($"  - Harsh Selection: Damage exponent = {SimulationPresets.HarshSelection.DamageExponent}");
            Console.WriteLine();

            Console.WriteLine("Phase 1 implementation complete!");
            Console.WriteLine("Core data structures are working correctly.");
        }

        /// <summary>
        /// Demonstrates Phase 2 physics simulation
        /// </summary>
        static void DemoPhase2()
        {
            Console.WriteLine("\n--- Phase 2: Physics Simulation Demo ---\n");

            // Use minimal configuration for faster demo
            var config = SimulationPresets.Minimal;
            config.VentEmissionRate = 5; // Faster token generation
            config.InitialTokenEnergy = 20; // Lower energy for faster cycles

            Console.WriteLine("Creating simulation with Minimal preset...");
            Console.WriteLine($"  Grid: {config.GridWidth}x{config.GridHeight}x{config.GridDepth}");
            Console.WriteLine($"  Vent Emission Rate: every {config.VentEmissionRate} ticks");
            Console.WriteLine($"  Initial Token Energy: {config.InitialTokenEnergy}");
            Console.WriteLine($"  Max Active Tokens: {config.MaxActiveTokens}");
            Console.WriteLine();

            var simulation = new SimulationEngine(config);
            simulation.Start();

            Console.WriteLine("Simulation started! Running for 50 ticks...");
            Console.WriteLine("Watch tokens being generated, rising, and falling!\n");

            // Run for 50 ticks
            for (int i = 0; i < 50; i++)
            {
                simulation.Update();

                // Display stats every 10 ticks
                if (i % 10 == 0)
                {
                    var stats = simulation.GetStatistics();
                    Console.WriteLine($"[Tick {stats.CurrentTick}] " +
                                    $"Tokens: {stats.ActiveTokenCount} | " +
                                    $"Generated: {stats.TotalGenerated} | " +
                                    $"Destroyed: {stats.TotalDestroyed} | " +
                                    $"Avg Energy: {stats.AverageEnergy:F1}");
                }

                Thread.Sleep(50); // Small delay for readability
            }

            Console.WriteLine("\nFinal Statistics:");
            var finalStats = simulation.GetStatistics();
            Console.WriteLine($"  Total Tokens Generated: {finalStats.TotalGenerated}");
            Console.WriteLine($"  Total Tokens Destroyed: {finalStats.TotalDestroyed}");
            Console.WriteLine($"  Active Tokens: {finalStats.ActiveTokenCount}");
            Console.WriteLine($"  Active Cells: {finalStats.ActiveCellCount}");
            Console.WriteLine($"  Average Energy: {finalStats.AverageEnergy:F2}");

            simulation.Stop();
            Console.WriteLine("\nPhase 2 demo complete!");
        }

        /// <summary>
        /// Runs full simulation with interactive controls
        /// </summary>
        static void RunFullSimulation()
        {
            Console.WriteLine("\n--- Full Simulation ---\n");

            Console.WriteLine("Select preset:");
            Console.WriteLine("1. Minimal (10x10x10, fast)");
            Console.WriteLine("2. Standard (50x50x50, balanced)");
            Console.WriteLine("3. Expression Evolution (optimized for math)");
            Console.WriteLine("4. Harsh Selection (strong evolutionary pressure)");
            Console.Write("\nChoice: ");

            SimulationConfig config = Console.ReadLine() switch
            {
                "1" => SimulationPresets.Minimal,
                "2" => SimulationPresets.Standard,
                "3" => SimulationPresets.ExpressionEvolution,
                "4" => SimulationPresets.HarshSelection,
                _ => SimulationPresets.Standard
            };

            Console.WriteLine($"\nStarting simulation with {config.GridWidth}x{config.GridHeight}x{config.GridDepth} grid...");
            Console.WriteLine("Press 'P' to pause/unpause, 'Q' to quit, 'S' for stats");
            Console.WriteLine();

            var simulation = new SimulationEngine(config);
            simulation.Start();

            bool running = true;
            int ticksSinceLastDisplay = 0;

            while (running)
            {
                // Update simulation
                simulation.Update();
                ticksSinceLastDisplay++;

                // Display stats every 100 ticks
                if (ticksSinceLastDisplay >= 100)
                {
                    var stats = simulation.GetStatistics();
                    Console.WriteLine(stats.ToString());
                    ticksSinceLastDisplay = 0;
                }

                // Check for user input (non-blocking)
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.P:
                            simulation.SetPaused(!simulation.TickManager.IsPaused);
                            Console.WriteLine(simulation.TickManager.IsPaused ? "PAUSED" : "RESUMED");
                            break;
                        case ConsoleKey.S:
                            var stats = simulation.GetStatistics();
                            Console.WriteLine($"\n{stats}");
                            Console.WriteLine($"  TPS: {simulation.TickManager.ActualTicksPerSecond:F2}");
                            break;
                        case ConsoleKey.Q:
                            running = false;
                            break;
                    }
                }

                Thread.Sleep(10); // Small delay
            }

            simulation.Stop();
            Console.WriteLine("\nSimulation stopped.");

            var finalStats = simulation.GetStatistics();
            Console.WriteLine("\nFinal Statistics:");
            Console.WriteLine(finalStats.ToString());
        }

        /// <summary>
        /// Runs full integrated simulation with GUI visualization
        /// </summary>
        static void RunFullSimulationWithGUI()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold yellow]Full Integrated Simulation (GUI Mode)[/]\n");

            var config = AnsiConsole.Prompt(
                new SelectionPrompt<SimulationConfig>()
                    .Title("[green]Select preset configuration:[/]")
                    .UseConverter(c => $"{c.GridWidth}x{c.GridHeight}x{c.GridDepth} - {GetPresetName(c)}")
                    .AddChoices(new[] {
                        SimulationPresets.Minimal,
                        SimulationPresets.Standard,
                        SimulationPresets.ExpressionEvolution,
                        SimulationPresets.HarshSelection,
                        SimulationPresets.RapidEvolution
                    }));

            AnsiConsole.Clear();
            AnsiConsole.Status()
                .Start("Initializing simulation...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.Status("Loading systems...");
                    Thread.Sleep(500);
                });

            using (var simulation = new IntegratedSimulationEngine(config))
            {
                var visualizer = new VisualizationEngine(simulation.Grid);

                simulation.Start();

                AnsiConsole.MarkupLine("[green]Simulation started![/]");
                AnsiConsole.MarkupLine("[dim]Use keyboard controls to interact with the visualization[/]");
                Thread.Sleep(1000);

                bool running = true;
                int frameCount = 0;

                while (running)
                {
                    // Update simulation
                    simulation.Update();

                    // Render GUI every few frames to reduce overhead
                    if (frameCount % 3 == 0)
                    {
                        try
                        {
                            visualizer.Render(simulation);
                        }
                        catch
                        {
                            // Fallback to simple rendering if layout fails
                            visualizer.RenderSimple(simulation);
                        }
                    }

                    frameCount++;

                    // Check for user input (non-blocking)
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        switch (key)
                        {
                            case ConsoleKey.UpArrow:
                                visualizer.IncrementLayer();
                                break;
                            case ConsoleKey.DownArrow:
                                visualizer.DecrementLayer();
                                break;
                            case ConsoleKey.M:
                                visualizer.ToggleMetrics();
                                break;
                            case ConsoleKey.L:
                                visualizer.ToggleLegend();
                                break;
                            case ConsoleKey.P:
                                simulation.SetPaused(!simulation.TickManager.IsPaused);
                                break;
                            case ConsoleKey.Q:
                                running = false;
                                break;
                        }
                    }

                    Thread.Sleep(16); // ~60 FPS target
                }

                simulation.Stop();

                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[yellow]Simulation stopped.[/]\n");

                var finalStats = simulation.GetStatistics();
                var statsTable = new Table()
                    .BorderColor(Color.Green)
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]Metric[/]")
                    .AddColumn("[bold]Value[/]");

                statsTable.AddRow("Total Ticks", finalStats.CurrentTick.ToString());
                statsTable.AddRow("Active Tokens", finalStats.ActiveTokenCount.ToString());
                statsTable.AddRow("Total Generated", finalStats.TotalGenerated.ToString());
                statsTable.AddRow("Total Destroyed", finalStats.TotalDestroyed.ToString());
                statsTable.AddRow("Total Chains", finalStats.TotalChains.ToString());
                statsTable.AddRow("Stable Chains", finalStats.StableChains.ToString());
                statsTable.AddRow("Longest Chain", finalStats.LongestChainLength.ToString());

                AnsiConsole.Write(new Panel(statsTable)
                    .Header("[bold green]Final Statistics[/]")
                    .Expand());
            }
        }

        /// <summary>
        /// Gets a friendly name for a preset configuration
        /// </summary>
        static string GetPresetName(SimulationConfig config)
        {
            if (config.GridWidth == 10) return "Minimal";
            if (config.GridWidth == 50) return "Standard";
            if (config.DamageExponent == 2.5f) return "Expression Evolution";
            if (config.DamageExponent == 4.0f) return "Harsh Selection";
            if (config.VentEmissionRate == 2) return "Rapid Evolution";
            return "Custom";
        }

        /// <summary>
        /// Runs full integrated simulation with web interface
        /// </summary>
        static void RunFullSimulationWithWebInterface()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold yellow]Full Integrated Simulation (Web Interface)[/]\n");

            var config = AnsiConsole.Prompt(
                new SelectionPrompt<SimulationConfig>()
                    .Title("[green]Select preset configuration:[/]")
                    .UseConverter(c => $"{c.GridWidth}x{c.GridHeight}x{c.GridDepth} - {GetPresetName(c)}")
                    .AddChoices(new[] {
                        SimulationPresets.Minimal,
                        SimulationPresets.Standard,
                        SimulationPresets.ExpressionEvolution,
                        SimulationPresets.HarshSelection,
                        SimulationPresets.RapidEvolution
                    }));

            var port = AnsiConsole.Ask<int>("[cyan]Web server port:[/]", 8080);

            AnsiConsole.Clear();
            AnsiConsole.Status()
                .Start("Initializing simulation and web server...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.Status("Starting systems...");
                    Thread.Sleep(500);
                });

            using (var simulation = new IntegratedSimulationEngine(config))
            using (var webServer = new SimulationWebServer(simulation, port))
            {
                simulation.Start();
                webServer.Start();

                AnsiConsole.Clear();
                var panel = new Panel(
                    new Markup($"[green]✓ Simulation running[/]\n" +
                             $"[green]✓ Web server running on port {port}[/]\n\n" +
                             $"[cyan]Open your browser and navigate to:[/]\n" +
                             $"[bold white]http://localhost:{port}/[/]\n\n" +
                             $"[yellow]Features:[/]\n" +
                             $"  • Real-time 3D visualization with Three.js\n" +
                             $"  • Live metrics dashboard\n" +
                             $"  • Evolution tracker\n" +
                             $"  • CSV/JSON data export\n" +
                             $"  • Interactive controls\n\n" +
                             $"[dim]Press any key to stop the simulation...[/]"))
                {
                    Header = new PanelHeader("[bold cyan]🌐 Web Interface Active[/]"),
                    BorderColor = Color.Cyan,
                    Padding = new Padding(2, 1, 2, 1)
                };

                AnsiConsole.Write(panel);

                // Run simulation loop in background
                bool running = true;
                var simulationThread = new Thread(() =>
                {
                    while (running)
                    {
                        simulation.Update();
                        Thread.Sleep(10);
                    }
                })
                {
                    IsBackground = true
                };
                simulationThread.Start();

                // Wait for user to press a key
                Console.ReadKey(true);
                running = false;

                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[yellow]Stopping simulation and web server...[/]");

                simulation.Stop();
                webServer.Stop();

                Thread.Sleep(500);

                AnsiConsole.MarkupLine("[green]✓ Simulation stopped[/]");
                AnsiConsole.MarkupLine("[green]✓ Web server stopped[/]\n");

                var finalStats = simulation.GetStatistics();
                var statsTable = new Table()
                    .BorderColor(Color.Green)
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]Metric[/]")
                    .AddColumn("[bold]Value[/]");

                statsTable.AddRow("Total Ticks", finalStats.CurrentTick.ToString());
                statsTable.AddRow("Active Tokens", finalStats.ActiveTokenCount.ToString());
                statsTable.AddRow("Total Generated", finalStats.TotalGenerated.ToString());
                statsTable.AddRow("Total Chains", finalStats.TotalChains.ToString());
                statsTable.AddRow("Stable Chains", finalStats.StableChains.ToString());

                AnsiConsole.Write(new Panel(statsTable)
                    .Header("[bold green]Final Statistics[/]")
                    .Expand());
            }
        }
    }
}

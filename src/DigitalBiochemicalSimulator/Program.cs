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

            var configChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select configuration:[/]")
                    .AddChoices(new[] {
                        "Use Preset Configuration",
                        "Custom Configuration (Advanced)"
                    }));

            SimulationConfig config;

            if (configChoice == "Custom Configuration (Advanced)")
            {
                config = CustomizeConfiguration();
            }
            else
            {
                config = AnsiConsole.Prompt(
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
            }

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
        /// Allows user to customize simulation configuration
        /// </summary>
        static SimulationConfig CustomizeConfiguration()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold cyan]Custom Configuration[/]\n");

            // Start with a base preset
            var basePreset = AnsiConsole.Prompt(
                new SelectionPrompt<SimulationConfig>()
                    .Title("[green]Start with preset as base:[/]")
                    .UseConverter(c => GetPresetName(c))
                    .AddChoices(new[] {
                        SimulationPresets.Minimal,
                        SimulationPresets.Standard,
                        SimulationPresets.ExpressionEvolution,
                        SimulationPresets.HarshSelection,
                        SimulationPresets.RapidEvolution
                    }));

            var config = basePreset.Clone();

            AnsiConsole.MarkupLine($"\n[yellow]Customizing configuration based on: {GetPresetName(basePreset)}[/]\n");

            // Configuration categories to customize
            var categories = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[green]Select categories to customize:[/]")
                    .NotRequired()
                    .PageSize(15)
                    .InstructionsText("[grey](Press [blue]space[/] to toggle, [green]enter[/] to accept)[/]")
                    .AddChoices(new[] {
                        "Bond Strengths (Covalent, Ionic, Van der Waals)",
                        "Bonding Behavior (Radius, Minimum Strength)",
                        "Grid Dimensions & Cell Capacity",
                        "Energy Parameters & Metabolism",
                        "Physics (Gravity, Viscosity, Motion)",
                        "Damage & Mutation Parameters",
                        "Thermal Vents & Token Distribution",
                        "Validation & Chain Stability",
                        "Performance Limits"
                    }));

            // Customize Bond Strengths
            if (categories.Contains("Bond Strengths (Covalent, Ionic, Van der Waals)"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Bond Strength Multipliers ═══[/]");
                AnsiConsole.MarkupLine("[dim]Multipliers adjust bond strength (1.0 = default from grammar)[/]\n");

                config.CovalentBondStrength = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Covalent Bond Strength:[/]")
                        .DefaultValue(config.CovalentBondStrength)
                        .ValidationErrorMessage("[red]Must be between 0.1 and 5.0[/]")
                        .Validate(v => v >= 0.1f && v <= 5.0f));

                config.IonicBondStrength = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Ionic Bond Strength:[/]")
                        .DefaultValue(config.IonicBondStrength)
                        .ValidationErrorMessage("[red]Must be between 0.1 and 5.0[/]")
                        .Validate(v => v >= 0.1f && v <= 5.0f));

                config.VanDerWaalsBondStrength = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Van der Waals Bond Strength:[/]")
                        .DefaultValue(config.VanDerWaalsBondStrength)
                        .ValidationErrorMessage("[red]Must be between 0.1 and 5.0[/]")
                        .Validate(v => v >= 0.1f && v <= 5.0f));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Bond strengths configured:");
                AnsiConsole.MarkupLine($"  Covalent: [yellow]{config.CovalentBondStrength:F2}x[/]");
                AnsiConsole.MarkupLine($"  Ionic: [yellow]{config.IonicBondStrength:F2}x[/]");
                AnsiConsole.MarkupLine($"  Van der Waals: [yellow]{config.VanDerWaalsBondStrength:F2}x[/]");
            }

            // Customize Bonding Behavior
            if (categories.Contains("Bonding Behavior (Radius, Minimum Strength)"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Bonding Behavior ═══[/]");
                AnsiConsole.MarkupLine("[dim]Controls how tokens bond with each other[/]\n");

                config.BondingRadius = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Bonding Radius (0=same cell only):[/]")
                        .DefaultValue(config.BondingRadius)
                        .ValidationErrorMessage("[red]Must be between 0 and 5[/]")
                        .Validate(v => v >= 0 && v <= 5));

                config.MinBondStrength = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Minimum Bond Strength (threshold):[/]")
                        .DefaultValue(config.MinBondStrength)
                        .ValidationErrorMessage("[red]Must be between 0.0 and 1.0[/]")
                        .Validate(v => v >= 0.0f && v <= 1.0f));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Bonding behavior configured:");
                AnsiConsole.MarkupLine($"  Radius: [yellow]{config.BondingRadius}[/] cells");
                AnsiConsole.MarkupLine($"  Min Strength: [yellow]{config.MinBondStrength:F2}[/]");
            }

            // Customize Grid Dimensions & Cell Capacity
            if (categories.Contains("Grid Dimensions & Cell Capacity"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Grid Dimensions & Cell Capacity ═══[/]\n");

                config.GridWidth = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Grid Width:[/]")
                        .DefaultValue(config.GridWidth)
                        .ValidationErrorMessage("[red]Must be between 5 and 200[/]")
                        .Validate(v => v >= 5 && v <= 200));

                config.GridHeight = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Grid Height:[/]")
                        .DefaultValue(config.GridHeight)
                        .ValidationErrorMessage("[red]Must be between 5 and 200[/]")
                        .Validate(v => v >= 5 && v <= 200));

                config.GridDepth = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Grid Depth:[/]")
                        .DefaultValue(config.GridDepth)
                        .ValidationErrorMessage("[red]Must be between 5 and 200[/]")
                        .Validate(v => v >= 5 && v <= 200));

                config.CellCapacity = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Cell Capacity (max tokens per cell):[/]")
                        .DefaultValue(config.CellCapacity)
                        .ValidationErrorMessage("[red]Must be between 10 and 10000[/]")
                        .Validate(v => v >= 10 && v <= 10000));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Grid: [yellow]{config.GridWidth}×{config.GridHeight}×{config.GridDepth}[/]");
                AnsiConsole.MarkupLine($"  Cell Capacity: [yellow]{config.CellCapacity}[/] tokens");
            }

            // Customize Energy Parameters & Metabolism
            if (categories.Contains("Energy Parameters & Metabolism"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Energy Parameters ═══[/]\n");

                config.InitialTokenEnergy = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Initial Token Energy:[/]")
                        .DefaultValue(config.InitialTokenEnergy)
                        .ValidationErrorMessage("[red]Must be between 10 and 500[/]")
                        .Validate(v => v >= 10 && v <= 500));

                config.EnergyPerTick = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Energy Loss Per Tick:[/]")
                        .DefaultValue(config.EnergyPerTick)
                        .ValidationErrorMessage("[red]Must be between 0 and 10[/]")
                        .Validate(v => v >= 0 && v <= 10));

                config.EnergyPerBond = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Energy Gain Per Bond:[/]")
                        .DefaultValue(config.EnergyPerBond)
                        .ValidationErrorMessage("[red]Must be between 0 and 20[/]")
                        .Validate(v => v >= 0 && v <= 20));

                config.EnvironmentViscosity = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Environment Viscosity (energy cost to rise):[/]")
                        .DefaultValue(config.EnvironmentViscosity)
                        .ValidationErrorMessage("[red]Must be between 0 and 10[/]")
                        .Validate(v => v >= 0 && v <= 10));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Energy & metabolism configured");
            }

            // Customize Physics
            if (categories.Contains("Physics (Gravity, Viscosity, Motion)"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Physics Parameters ═══[/]");
                AnsiConsole.MarkupLine("[dim]Controls movement and gravity in the simulation[/]\n");

                config.GravityEnabled = AnsiConsole.Confirm(
                    "[cyan]Enable Gravity?[/]",
                    config.GravityEnabled);

                config.RiseRate = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Rise Rate (cells per tick):[/]")
                        .DefaultValue(config.RiseRate)
                        .ValidationErrorMessage("[red]Must be between 1 and 10[/]")
                        .Validate(v => v >= 1 && v <= 10));

                config.FallRate = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Fall Rate (cells per tick):[/]")
                        .DefaultValue(config.FallRate)
                        .ValidationErrorMessage("[red]Must be between 1 and 10[/]")
                        .Validate(v => v >= 1 && v <= 10));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Physics configured:");
                AnsiConsole.MarkupLine($"  Gravity: [yellow]{(config.GravityEnabled ? "Enabled" : "Disabled")}[/]");
                AnsiConsole.MarkupLine($"  Rise Rate: [yellow]{config.RiseRate}[/] cells/tick");
                AnsiConsole.MarkupLine($"  Fall Rate: [yellow]{config.FallRate}[/] cells/tick");
            }

            // Customize Damage & Mutation Parameters
            if (categories.Contains("Damage & Mutation Parameters"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Damage Parameters ═══[/]\n");

                config.BaseDamageRate = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Base Damage Rate:[/]")
                        .DefaultValue(config.BaseDamageRate)
                        .ValidationErrorMessage("[red]Must be between 0.0 and 0.5[/]")
                        .Validate(v => v >= 0.0f && v <= 0.5f));

                config.DamageExponent = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Damage Exponent:[/]")
                        .DefaultValue(config.DamageExponent)
                        .ValidationErrorMessage("[red]Must be between 1.0 and 10.0[/]")
                        .Validate(v => v >= 1.0f && v <= 10.0f));

                config.CriticalDamageThreshold = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Critical Damage Threshold:[/]")
                        .DefaultValue(config.CriticalDamageThreshold)
                        .ValidationErrorMessage("[red]Must be between 0.5 and 1.0[/]")
                        .Validate(v => v >= 0.5f && v <= 1.0f));

                config.DamageIncrement = AnsiConsole.Prompt(
                    new TextPrompt<float>("[cyan]Damage Increment (per collision):[/]")
                        .DefaultValue(config.DamageIncrement)
                        .ValidationErrorMessage("[red]Must be between 0.0 and 1.0[/]")
                        .Validate(v => v >= 0.0f && v <= 1.0f));

                config.MutationRange = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Mutation Range (cells from top):[/]")
                        .DefaultValue(config.MutationRange)
                        .ValidationErrorMessage("[red]Must be between 0 and 50[/]")
                        .Validate(v => v >= 0 && v <= 50));

                config.MutationRate = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Mutation Rate (attempts per tick):[/]")
                        .DefaultValue(config.MutationRate)
                        .ValidationErrorMessage("[red]Must be between 0 and 100[/]")
                        .Validate(v => v >= 0 && v <= 100));

                var mutationFalloffChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Mutation Falloff Pattern:[/]")
                        .AddChoices(new[] { "Gradual", "Sharp", "None" }));

                config.MutationFalloff = mutationFalloffChoice switch
                {
                    "Gradual" => MutationFalloff.Gradual,
                    "Sharp" => MutationFalloff.Sharp,
                    "None" => MutationFalloff.None,
                    _ => MutationFalloff.Gradual
                };

                AnsiConsole.MarkupLine($"\n[green]✓[/] Damage & mutation configured");
            }

            // Customize Thermal Vents & Token Distribution
            if (categories.Contains("Thermal Vents & Token Distribution"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Thermal Vents ═══[/]\n");

                config.NumberOfVents = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Number of Vents:[/]")
                        .DefaultValue(config.NumberOfVents)
                        .ValidationErrorMessage("[red]Must be between 1 and 20[/]")
                        .Validate(v => v >= 1 && v <= 20));

                config.VentEmissionRate = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Vent Emission Rate (ticks per token):[/]")
                        .DefaultValue(config.VentEmissionRate)
                        .ValidationErrorMessage("[red]Must be between 1 and 100[/]")
                        .Validate(v => v >= 1 && v <= 100));

                var ventDistChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Vent Distribution Pattern:[/]")
                        .AddChoices(new[] { "Central", "Distributed", "Random" }));

                config.VentDistribution = ventDistChoice switch
                {
                    "Central" => VentDistribution.Central,
                    "Distributed" => VentDistribution.Distributed,
                    "Random" => VentDistribution.Random,
                    _ => VentDistribution.Central
                };

                var tokenDistChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Token Distribution Profile:[/]")
                        .AddChoices(new[] { "Balanced", "Expression Heavy", "Control Heavy" }));

                config.TokenDistribution = tokenDistChoice switch
                {
                    "Balanced" => TokenDistributionProfile.Balanced,
                    "Expression Heavy" => TokenDistributionProfile.ExpressionHeavy,
                    "Control Heavy" => TokenDistributionProfile.ControlHeavy,
                    _ => TokenDistributionProfile.Balanced
                };

                AnsiConsole.MarkupLine($"\n[green]✓[/] Thermal vents & token distribution configured:");
                AnsiConsole.MarkupLine($"  Vents: [yellow]{config.NumberOfVents}[/] ({ventDistChoice})");
                AnsiConsole.MarkupLine($"  Distribution: [yellow]{tokenDistChoice}[/]");
            }

            // Customize Validation & Chain Stability
            if (categories.Contains("Validation & Chain Stability"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Validation & Chain Stability ═══[/]");
                AnsiConsole.MarkupLine("[dim]Controls how chains are validated and stabilized[/]\n");

                config.MinValidationLength = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Min Validation Length (tokens):[/]")
                        .DefaultValue(config.MinValidationLength)
                        .ValidationErrorMessage("[red]Must be between 2 and 20[/]")
                        .Validate(v => v >= 2 && v <= 20));

                config.StabilityThreshold = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Stability Threshold (ticks without bonding):[/]")
                        .DefaultValue(config.StabilityThreshold)
                        .ValidationErrorMessage("[red]Must be between 1 and 100[/]")
                        .Validate(v => v >= 1 && v <= 100));

                config.ValidationFrequency = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Validation Frequency (check every N ticks):[/]")
                        .DefaultValue(config.ValidationFrequency)
                        .ValidationErrorMessage("[red]Must be between 1 and 50[/]")
                        .Validate(v => v >= 1 && v <= 50));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Validation & stability configured");
            }

            // Customize Performance Limits
            if (categories.Contains("Performance Limits"))
            {
                AnsiConsole.MarkupLine("\n[bold cyan]═══ Performance Limits ═══[/]\n");

                config.MaxActiveTokens = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Max Active Tokens:[/]")
                        .DefaultValue(config.MaxActiveTokens)
                        .ValidationErrorMessage("[red]Must be between 100 and 10000[/]")
                        .Validate(v => v >= 100 && v <= 10000));

                config.MaxChains = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Max Chains:[/]")
                        .DefaultValue(config.MaxChains)
                        .ValidationErrorMessage("[red]Must be between 10 and 1000[/]")
                        .Validate(v => v >= 10 && v <= 1000));

                config.TicksPerSecond = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Ticks Per Second:[/]")
                        .DefaultValue(config.TicksPerSecond)
                        .ValidationErrorMessage("[red]Must be between 1 and 100[/]")
                        .Validate(v => v >= 1 && v <= 100));

                AnsiConsole.MarkupLine($"\n[green]✓[/] Performance limits configured");
            }

            // Validate configuration
            if (!config.Validate(out var errorMessage))
            {
                AnsiConsole.MarkupLine($"\n[red]⚠ Configuration validation failed: {errorMessage}[/]");
                AnsiConsole.MarkupLine("[yellow]Reverting to base preset...[/]");
                return basePreset;
            }

            AnsiConsole.MarkupLine("\n[green]✓ Custom configuration complete and validated![/]");
            Thread.Sleep(1000);

            return config;
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

            var configChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select configuration:[/]")
                    .AddChoices(new[] {
                        "Use Preset Configuration",
                        "Custom Configuration (Advanced)"
                    }));

            SimulationConfig config;

            if (configChoice == "Custom Configuration (Advanced)")
            {
                config = CustomizeConfiguration();
            }
            else
            {
                config = AnsiConsole.Prompt(
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
            }

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

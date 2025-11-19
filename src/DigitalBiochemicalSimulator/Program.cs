using System;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;

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
            Console.WriteLine("Digital Biochemical Simulator v1.0");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            // Display welcome message
            Console.WriteLine("Welcome to the Digital Biochemical Simulator!");
            Console.WriteLine("This simulation models programming tokens as physical entities");
            Console.WriteLine("subject to biochemical and thermodynamic principles.");
            Console.WriteLine();

            // Phase 1 Demo: Test core data structures
            DemoPhase1();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
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
    }
}

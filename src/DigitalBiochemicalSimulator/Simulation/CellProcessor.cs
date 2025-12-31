using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Processes bonding, repulsion, and interactions within cells.
    /// Integrates bonding, repulsion, and damage systems.
    /// </summary>
    public class CellProcessor
    {
        private readonly BondingManager _bondingManager;
        private readonly RepulsionHandler _repulsionHandler;
        private readonly SimulationConfig _config;

        public CellProcessor(BondingManager bondingManager, RepulsionHandler repulsionHandler, SimulationConfig config)
        {
            _bondingManager = bondingManager;
            _repulsionHandler = repulsionHandler;
            _config = config;
        }

        /// <summary>
        /// Attempts to form bonds between tokens in a cell
        /// </summary>
        public int AttemptBonding(Cell cell, long currentTick)
        {
            if (cell == null || cell.Tokens.Count < 2)
                return 0;

            int bondsFormed = 0;

            // Try all pairs of tokens in the cell
            var tokens = cell.Tokens.Where(t => t.IsActive).ToList();

            for (int i = 0; i < tokens.Count; i++)
            {
                for (int j = i + 1; j < tokens.Count; j++)
                {
                    var token1 = tokens[i];
                    var token2 = tokens[j];

                    // Skip if already bonded
                    if (token1.BondedTokens.Contains(token2))
                        continue;

                    // Attempt to bond
                    if (_bondingManager.AttemptBond(token1, token2, currentTick))
                    {
                        bondsFormed++;
                    }
                }
            }

            return bondsFormed;
        }

        /// <summary>
        /// Checks for and resolves repulsion between tokens in a cell
        /// </summary>
        public void CheckRepulsion(Cell cell)
        {
            if (cell == null || cell.Tokens.Count < 2)
                return;

            _repulsionHandler?.CheckAndResolveRepulsion(cell);
        }

        /// <summary>
        /// Processes all interactions in a cell (bonding and repulsion)
        /// </summary>
        public CellProcessingResult ProcessCell(Cell cell, long currentTick)
        {
            if (cell == null)
                return new CellProcessingResult();

            var result = new CellProcessingResult
            {
                CellPosition = cell.Position,
                TokenCount = cell.Tokens.Count(t => t.IsActive)
            };

            // Step 1: Check and resolve repulsion first
            CheckRepulsion(cell);

            // Step 2: Attempt bonding between compatible tokens
            result.BondsFormed = AttemptBonding(cell, currentTick);

            return result;
        }

        /// <summary>
        /// Processes multiple cells efficiently
        /// </summary>
        public List<CellProcessingResult> ProcessCells(List<Cell> cells, long currentTick)
        {
            var results = new List<CellProcessingResult>();

            foreach (var cell in cells)
            {
                var result = ProcessCell(cell, currentTick);
                if (result.BondsFormed > 0 || result.TokenCount > 1)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Processes only active cells (optimization)
        /// </summary>
        public List<CellProcessingResult> ProcessActiveCells(Grid grid, long currentTick)
        {
            if (grid == null || grid.ActiveCells == null)
                return new List<CellProcessingResult>();

            var activeCells = grid.ActiveCells.Select(pos => grid.GetCell(pos)).Where(c => c != null).ToList();
            return ProcessCells(activeCells, currentTick);
        }
    }

    /// <summary>
    /// Result of processing a single cell
    /// </summary>
    public class CellProcessingResult
    {
        public Vector3Int CellPosition { get; set; }
        public int TokenCount { get; set; }
        public int BondsFormed { get; set; }

        public override string ToString()
        {
            return $"Cell {CellPosition}: {TokenCount} tokens, {BondsFormed} bonds formed";
        }
    }
}

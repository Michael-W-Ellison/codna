using System;
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
        private readonly Grid? _grid;

        /// <param name="grid">Grid used to find bonding partners in other cells when the
        /// configured bonding range is above 0; without it bonding stays within a cell</param>
        public CellProcessor(BondingManager bondingManager, RepulsionHandler repulsionHandler, SimulationConfig config,
                             Grid? grid = null)
        {
            _bondingManager = bondingManager;
            _repulsionHandler = repulsionHandler;
            _config = config;
            _grid = grid;
        }

        /// <summary>
        /// Attempts to form bonds between tokens in a cell
        /// </summary>
        public int AttemptBonding(Cell cell, long currentTick)
        {
            if (cell == null || cell.Tokens.Count == 0)
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

            bondsFormed += AttemptBondingInRange(cell, tokens, currentTick);

            return bondsFormed;
        }

        /// <summary>
        /// Attempts bonds between tokens in a cell and tokens in other cells within the
        /// configured bonding range (Euclidean distance between cell positions)
        /// </summary>
        private int AttemptBondingInRange(Cell cell, List<Token> tokens, long currentTick)
        {
            float range = _config?.EffectiveBondingRange ?? 0.0f;
            if (_grid == null || range <= 0.0f)
                return 0;

            // Manhattan radius that covers the Euclidean range
            int searchRadius = (int)Math.Ceiling(range * Math.Sqrt(3));
            var candidates = _grid.GetTokensInRadius(cell.Position, searchRadius)
                .Where(t => t.IsActive && t.Position != cell.Position && cell.Position.Distance(t.Position) <= range)
                .ToList();

            int bondsFormed = 0;

            foreach (var token in tokens)
            {
                foreach (var partner in candidates)
                {
                    // Each cross-cell pair is attempted once, from the token with the lower ID
                    if (partner.Id <= token.Id || token.BondedTokens.Contains(partner))
                        continue;

                    if (_bondingManager.AttemptBond(token, partner, currentTick))
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

            var activeCells = grid.ActiveCells.Select(pos => grid.GetCell(pos)).OfType<Cell>().ToList();
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

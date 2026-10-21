using System;
using System.Collections.Generic;
using System.Linq;
using DigitalBiochemicalSimulator.Simulation;
using BasicPresets = DigitalBiochemicalSimulator.Simulation.SimulationPresets;
using ScenarioPresets = DigitalBiochemicalSimulator.Presets.SimulationPresets;

namespace DigitalBiochemicalSimulator.Presets
{
    /// <summary>
    /// A named preset configuration
    /// </summary>
    public class PresetInfo
    {
        public string Name { get; }
        public string Description { get; }
        private readonly Func<SimulationConfig> _create;

        public PresetInfo(string name, string description, Func<SimulationConfig> create)
        {
            Name = name;
            Description = description;
            _create = create;
        }

        /// <summary>
        /// Creates a new configuration from this preset
        /// </summary>
        public SimulationConfig Create() => _create();
    }

    /// <summary>
    /// Every preset the application offers: the basic presets (Simulation.SimulationPresets)
    /// followed by the scenario presets (Presets.SimulationPresets)
    /// </summary>
    public static class PresetCatalog
    {
        public static IReadOnlyList<PresetInfo> All { get; } = BuildCatalog();

        private static List<PresetInfo> BuildCatalog()
        {
            var presets = new List<PresetInfo>
            {
                new PresetInfo("Minimal", "Small grid for quick runs", () => BasicPresets.Minimal),
                new PresetInfo("Standard", "Balanced default simulation", () => BasicPresets.Standard),
                new PresetInfo("Complex", "Large grid with control-heavy token mix", () => BasicPresets.Complex),
                new PresetInfo("Expression Evolution", "Expression-heavy tokens, gentler damage, stronger bonds", () => BasicPresets.ExpressionEvolution),
                new PresetInfo("Harsh Selection", "Steep damage gradient and scarce tokens", () => BasicPresets.HarshSelection)
            };

            foreach (var name in ScenarioPresets.GetAllPresets().Keys)
            {
                presets.Add(new PresetInfo(
                    name,
                    ScenarioPresets.GetPresetDescription(name),
                    () => ScenarioPresets.GetAllPresets()[name]));
            }

            return presets;
        }

        /// <summary>
        /// Finds a preset by name (case-insensitive), or null
        /// </summary>
        public static PresetInfo? Find(string name)
        {
            return All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}

using Xunit;
using DigitalBiochemicalSimulator.Presets;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Tests.Presets
{
    public class SimulationPresetsTests
    {
        [Fact]
        public void RapidEvolution_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.RapidEvolution();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
            Assert.Equal(30, config.GridWidth);
            Assert.Equal(30, config.GridHeight);
            Assert.Equal(30, config.GridDepth);
        }

        [Fact]
        public void StableFormation_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.StableFormation();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void HighPressure_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.HighPressure();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void SparseExploration_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.SparseExploration();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void MicroEvolution_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.MicroEvolution();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void ChaoticDynamics_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.ChaoticDynamics();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void CompetitiveSelection_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.CompetitiveSelection();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void CooperativeBuilding_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.CooperativeBuilding();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void MinimalistSimulation_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.MinimalistSimulation();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
        }

        [Fact]
        public void MaximumComplexity_ReturnsValidConfig()
        {
            // Act
            var config = SimulationPresets.MaximumComplexity();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Validate(out _));
            Assert.Equal(100, config.GridWidth);
            Assert.Equal(100, config.GridHeight);
            Assert.Equal(100, config.GridDepth);
        }

        [Fact]
        public void GetAllPresets_ReturnsAllPresets()
        {
            // Act
            var allPresets = SimulationPresets.GetAllPresets();

            // Assert
            Assert.NotNull(allPresets);
            Assert.Equal(10, allPresets.Count);
            Assert.True(allPresets.ContainsKey("RapidEvolution"));
            Assert.True(allPresets.ContainsKey("StableFormation"));
            Assert.True(allPresets.ContainsKey("HighPressure"));
            Assert.True(allPresets.ContainsKey("SparseExploration"));
            Assert.True(allPresets.ContainsKey("MicroEvolution"));
            Assert.True(allPresets.ContainsKey("ChaoticDynamics"));
            Assert.True(allPresets.ContainsKey("CompetitiveSelection"));
            Assert.True(allPresets.ContainsKey("CooperativeBuilding"));
            Assert.True(allPresets.ContainsKey("MinimalistSimulation"));
            Assert.True(allPresets.ContainsKey("MaximumComplexity"));
        }

        [Fact]
        public void GetAllPresets_AllConfigsAreValid()
        {
            // Act
            var allPresets = SimulationPresets.GetAllPresets();

            // Assert
            foreach (var preset in allPresets.Values)
            {
                Assert.True(preset.Validate(out string errorMessage),
                    $"Preset configuration is invalid: {errorMessage}");
            }
        }

        [Fact]
        public void GetPresetDescription_ReturnsDescription()
        {
            // Act
            var description = SimulationPresets.GetPresetDescription("RapidEvolution");

            // Assert
            Assert.NotNull(description);
            Assert.NotEmpty(description);
        }

        [Fact]
        public void GetPresetDescription_InvalidName_ReturnsUnknown()
        {
            // Act
            var description = SimulationPresets.GetPresetDescription("InvalidPresetName");

            // Assert
            Assert.Contains("Unknown", description);
        }

        [Fact]
        public void GetRecommendedDuration_ReturnsPositiveValue()
        {
            // Act
            var duration = SimulationPresets.GetRecommendedDuration("RapidEvolution");

            // Assert
            Assert.True(duration > 0);
        }

        [Fact]
        public void GetRecommendedDuration_InvalidName_ReturnsDefault()
        {
            // Act
            var duration = SimulationPresets.GetRecommendedDuration("InvalidPresetName");

            // Assert
            Assert.Equal(1000, duration); // Default value
        }

        [Fact]
        public void GetExpectedPerformance_ReturnsDescription()
        {
            // Act
            var performance = SimulationPresets.GetExpectedPerformance("RapidEvolution");

            // Assert
            Assert.NotNull(performance);
            Assert.NotEmpty(performance);
        }

        [Fact]
        public void GetExpectedPerformance_InvalidName_ReturnsUnknown()
        {
            // Act
            var performance = SimulationPresets.GetExpectedPerformance("InvalidPresetName");

            // Assert
            Assert.Contains("Unknown", performance);
        }

        [Fact]
        public void Presets_HaveDistinctParameters()
        {
            // Arrange
            var rapid = SimulationPresets.RapidEvolution();
            var stable = SimulationPresets.StableFormation();
            var minimal = SimulationPresets.MinimalistSimulation();
            var maximum = SimulationPresets.MaximumComplexity();

            // Assert - Different grid sizes
            Assert.NotEqual(rapid.GridWidth, stable.GridWidth);
            Assert.NotEqual(minimal.GridWidth, maximum.GridWidth);

            // Assert - Different max tokens
            Assert.NotEqual(rapid.MaxTokens, maximum.MaxTokens);
            Assert.True(minimal.MaxTokens < maximum.MaxTokens);
        }

        [Fact]
        public void MinimalistSimulation_HasSmallestParameters()
        {
            // Arrange
            var minimal = SimulationPresets.MinimalistSimulation();
            var allPresets = SimulationPresets.GetAllPresets();

            // Act & Assert - Should be among smallest
            foreach (var preset in allPresets.Values)
            {
                if (preset != minimal)
                {
                    Assert.True(preset.MaxTokens >= minimal.MaxTokens ||
                                preset.GridWidth * preset.GridHeight * preset.GridDepth >
                                minimal.GridWidth * minimal.GridHeight * minimal.GridDepth);
                }
            }
        }

        [Fact]
        public void MaximumComplexity_HasLargestParameters()
        {
            // Arrange
            var maximum = SimulationPresets.MaximumComplexity();
            var allPresets = SimulationPresets.GetAllPresets();

            // Act & Assert - Should be among largest
            int largerCount = 0;
            foreach (var preset in allPresets.Values)
            {
                if (preset.MaxTokens > maximum.MaxTokens)
                    largerCount++;
            }

            Assert.True(largerCount <= 1); // At most one preset has more tokens
        }

        [Fact]
        public void HighPressure_HasHighAltitudeGrid()
        {
            // Act
            var config = SimulationPresets.HighPressure();

            // Assert - Should have tall grid for high altitude pressure
            Assert.True(config.GridHeight >= 100);
        }

        [Fact]
        public void SparseExploration_HasLargeVolume()
        {
            // Act
            var config = SimulationPresets.SparseExploration();

            // Assert - Should have large volume
            long volume = (long)config.GridWidth * config.GridHeight * config.GridDepth;
            Assert.True(volume > 100000); // Large volume for sparse exploration
        }
    }
}

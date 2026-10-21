using Xunit;

namespace DigitalBiochemicalSimulator.Tests.Performance
{
    /// <summary>
    /// Timing and memory measurements are process-wide, so these tests run
    /// without other tests executing in parallel
    /// </summary>
    [CollectionDefinition("Performance", DisableParallelization = true)]
    public class PerformanceCollection
    {
    }
}

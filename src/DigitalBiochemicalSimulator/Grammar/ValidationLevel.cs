namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// When to validate grammar rules.
    /// Based on section 3.5.1 of the design specification.
    /// </summary>
    public enum ValidationLevel
    {
        /// <summary>
        /// Validate immediately upon bonding
        /// </summary>
        IMMEDIATE,

        /// <summary>
        /// Validate after chain stabilizes
        /// </summary>
        DELAYED,

        /// <summary>
        /// Defer validation until explicitly requested
        /// </summary>
        DEFERRED
    }
}

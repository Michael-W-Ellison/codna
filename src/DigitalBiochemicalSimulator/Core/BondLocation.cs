namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Defines where a bond can be attached on a token.
    /// Based on section 3.1.3 of the design specification.
    /// </summary>
    public enum BondLocation
    {
        /// <summary>
        /// Bond at the start of the token (left side in linear chains)
        /// </summary>
        START,

        /// <summary>
        /// Bond at the end of the token (right side in linear chains)
        /// </summary>
        END,

        /// <summary>
        /// Bond on the left side (for branching structures)
        /// </summary>
        LEFT,

        /// <summary>
        /// Bond on the right side (for branching structures)
        /// </summary>
        RIGHT,

        /// <summary>
        /// Internal bond point (for complex structures)
        /// </summary>
        INTERNAL
    }
}
